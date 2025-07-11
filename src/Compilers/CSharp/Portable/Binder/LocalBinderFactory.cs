﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The LocalBinderFactory is used to build up the map of all Binders within a method body, and the associated
    /// CSharpSyntaxNode. To do so it traverses all the statements, handling blocks and other
    /// statements that create scopes. For efficiency reasons, it does not traverse into all
    /// expressions. This means that blocks within lambdas and queries are not created.
    /// Blocks within lambdas are bound by their own LocalBinderFactory when they are
    /// analyzed.
    ///
    /// For reasons of lifetime management, this type is distinct from the BinderFactory
    /// which also creates a map from CSharpSyntaxNode to Binder. That type owns its binders
    /// and that type's lifetime is that of the compilation. Therefore we do not store
    /// binders local to method bodies in that type's cache.
    /// </summary>
    internal sealed class LocalBinderFactory : CSharpSyntaxWalker
    {
        private readonly SmallDictionary<SyntaxNode, Binder> _map;
        private Symbol _containingMemberOrLambda;
        private Binder _enclosing;
        private readonly SyntaxNode _root;

        private void Visit(CSharpSyntaxNode syntax, Binder enclosing)
        {
            if (_enclosing == enclosing)
            {
                this.Visit(syntax);
            }
            else
            {
                Binder oldEnclosing = _enclosing;
                _enclosing = enclosing;
                this.Visit(syntax);
                _enclosing = oldEnclosing;
            }
        }

        private void VisitRankSpecifiers(TypeSyntax type, Binder enclosing)
        {
            type.VisitRankSpecifiers((rankSpecifier, args) =>
            {
                foreach (var size in rankSpecifier.Sizes)
                {
                    if (size.Kind() != SyntaxKind.OmittedArraySizeExpression)
                    {
                        args.localBinderFactory.Visit(size, args.binder);
                    }
                }
            }, (localBinderFactory: this, binder: enclosing));
        }

        // Currently the types of these are restricted to only be whatever the syntax parameter is, plus any LocalFunctionStatementSyntax contained within it.
        // This may change if the language is extended to allow iterator lambdas, in which case the lambda would also be returned.
        // (lambdas currently throw a diagnostic in WithLambdaParametersBinder.GetIteratorElementType when a yield is used within them)
        public static SmallDictionary<SyntaxNode, Binder> BuildMap(
            Symbol containingMemberOrLambda,
            SyntaxNode syntax,
            Binder enclosing,
            Action<Binder, SyntaxNode> binderUpdatedHandler = null)
        {
            var builder = new LocalBinderFactory(containingMemberOrLambda, syntax, enclosing);

            StatementSyntax statement;
            var expressionSyntax = syntax as ExpressionSyntax;
            if (expressionSyntax != null)
            {
                enclosing = new ExpressionVariableBinder(syntax, enclosing);

                if ((object)binderUpdatedHandler != null)
                {
                    binderUpdatedHandler(enclosing, syntax);
                }

                builder.AddToMap(syntax, enclosing);
                builder.Visit(expressionSyntax, enclosing);
            }
            else if (syntax.Kind() != SyntaxKind.Block && (statement = syntax as StatementSyntax) != null)
            {
                CSharpSyntaxNode embeddedScopeDesignator;
                enclosing = builder.GetBinderForPossibleEmbeddedStatement(statement, enclosing, out embeddedScopeDesignator);

                if ((object)binderUpdatedHandler != null)
                {
                    binderUpdatedHandler(enclosing, embeddedScopeDesignator);
                }

                if (embeddedScopeDesignator != null)
                {
                    builder.AddToMap(embeddedScopeDesignator, enclosing);
                }

                builder.Visit(statement, enclosing);
            }
            else
            {
                if ((object)binderUpdatedHandler != null)
                {
                    binderUpdatedHandler(enclosing, null);
                }

                builder.Visit((CSharpSyntaxNode)syntax, enclosing);
            }

            return builder._map;
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            foreach (MemberDeclarationSyntax member in node.Members)
            {
                if (member.Kind() == SyntaxKind.GlobalStatement)
                {
                    Visit(member);
                }
            }
        }

        private LocalBinderFactory(Symbol containingMemberOrLambda, SyntaxNode root, Binder enclosing)
        {
            Debug.Assert((object)containingMemberOrLambda != null);
            Debug.Assert(containingMemberOrLambda.Kind != SymbolKind.Local && containingMemberOrLambda.Kind != SymbolKind.RangeVariable && containingMemberOrLambda.Kind != SymbolKind.Parameter);

            _map = new SmallDictionary<SyntaxNode, Binder>(ReferenceEqualityComparer.Instance);
            _containingMemberOrLambda = containingMemberOrLambda;
            _enclosing = enclosing;
            _root = root;
        }

        #region Starting points - these nodes contain statements

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            Visit(node.Body);
            Visit(node.ExpressionBody);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            Binder enclosing = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, enclosing);

            Visit(node.Initializer, enclosing);
            Visit(node.Body, enclosing);
            Visit(node.ExpressionBody, enclosing);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitTypeDeclaration(node);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            VisitTypeDeclaration(node);
        }

        private void VisitTypeDeclaration(TypeDeclarationSyntax node)
        {
            Debug.Assert(node.ParameterList is object);
            Debug.Assert(node.Kind() is SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration);

            Visit(node.PrimaryConstructorBaseTypeIfClass);
        }

        public override void VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node)
        {
            Binder enclosing = new ExpressionVariableBinder(node, _enclosing).WithAdditionalFlags(BinderFlags.ConstructorInitializer);
            AddToMap(node, enclosing);
            VisitConstructorInitializerArgumentList(node, node.ArgumentList, enclosing);
        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            Visit(node.Body);
            Visit(node.ExpressionBody);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            Visit(node.Body);
            Visit(node.ExpressionBody);
        }

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            Visit(node.Body);
            Visit(node.ExpressionBody);
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            Visit(node.Body);
            Visit(node.ExpressionBody);
        }

#nullable enable
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.MayBeNameofOperator())
            {
                var oldEnclosing = _enclosing;

                WithTypeParametersBinder? withTypeParametersBinder;
                Binder? withParametersBinder;
                if ((_enclosing.Flags & BinderFlags.InContextualAttributeBinder) != 0)
                {
                    var attributeTarget = getAttributeTarget(_enclosing);
                    withTypeParametersBinder = getExtraWithTypeParametersBinder(_enclosing, attributeTarget);
                    withParametersBinder = getExtraWithParametersBinder(_enclosing, attributeTarget);
                }
                else
                {
                    withTypeParametersBinder = null;
                    withParametersBinder = null;
                }

                var argumentExpression = node.ArgumentList.Arguments[0].Expression;
                var possibleNameofBinder = new NameofBinder(argumentExpression, _enclosing, withTypeParametersBinder, withParametersBinder);
                AddToMap(node, possibleNameofBinder);

                _enclosing = possibleNameofBinder;
                base.VisitInvocationExpression(node);
                _enclosing = oldEnclosing;
                return;
            }

            if (receiverIsInvocation(node, out InvocationExpressionSyntax? nested))
            {
                var invocations = ArrayBuilder<InvocationExpressionSyntax>.GetInstance();

                invocations.Push(node);

                node = nested;
                while (receiverIsInvocation(node, out nested))
                {
                    invocations.Push(node);
                    node = nested;
                }

                Visit(node.Expression);

                do
                {
                    Visit(node.ArgumentList);
                }
                while (invocations.TryPop(out node!));

                invocations.Free();
            }
            else
            {
                Visit(node.Expression);
                Visit(node.ArgumentList);
            }

            return;

            static bool receiverIsInvocation(InvocationExpressionSyntax node, [NotNullWhen(true)] out InvocationExpressionSyntax? nested)
            {
                if (node.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax receiver } && !receiver.MayBeNameofOperator())
                {
                    nested = receiver;
                    return true;
                }

                nested = null;
                return false;
            }

            static Symbol getAttributeTarget(Binder current)
            {
                Debug.Assert((current.Flags & BinderFlags.InContextualAttributeBinder) != 0);
                var contextualAttributeBinder = Binder.TryGetContextualAttributeBinder(current);

                Debug.Assert(contextualAttributeBinder is not null);
                return contextualAttributeBinder.AttributeTarget;
            }

            static WithTypeParametersBinder? getExtraWithTypeParametersBinder(Binder enclosing, Symbol target)
                => target.Kind == SymbolKind.Method ? new WithMethodTypeParametersBinder((MethodSymbol)target, enclosing) : null;

            // We're bringing parameters in scope inside `nameof` in attributes on methods, their type parameters and parameters.
            // This also applies to local functions, lambdas, indexers and delegates.
            static Binder? getExtraWithParametersBinder(Binder enclosing, Symbol target)
            {
                if (target is LambdaSymbol lambda)
                {
                    // lambda parameters have some special rules around parameters named `_`
                    return new WithLambdaParametersBinder(lambda, enclosing);
                }

                var parameters = target switch
                {
                    SourcePropertyAccessorSymbol { MethodKind: MethodKind.PropertySet } setter => getSetterParameters(setter),
                    MethodSymbol methodSymbol => methodSymbol.Parameters,
                    ParameterSymbol parameter when parameter.ContainingSymbol is not NamedTypeSymbol => getAllParameters(parameter),
                    TypeParameterSymbol typeParameter => getMethodParametersFromTypeParameter(typeParameter),
                    PropertySymbol property => property.Parameters,
                    NamedTypeSymbol namedType when namedType.IsDelegateType() => getDelegateParameters(namedType),
                    _ => default
                };

                return parameters.IsDefaultOrEmpty
                    ? null
                    : new WithParametersBinder(parameters, enclosing);
            }

            static ImmutableArray<ParameterSymbol> getAllParameters(ParameterSymbol parameter)
            {
                switch (parameter.ContainingSymbol)
                {
                    case MethodSymbol method:
                        return method.Parameters;
                    case PropertySymbol property:
                        return property.Parameters;
                    default:
                        Debug.Assert(false);
                        return default;
                }
            }

            static ImmutableArray<ParameterSymbol> getMethodParametersFromTypeParameter(TypeParameterSymbol typeParameter)
            {
                switch (typeParameter.ContainingSymbol)
                {
                    case MethodSymbol method:
                        return method.Parameters;
                    case NamedTypeSymbol namedType when namedType.IsDelegateType():
                        return getDelegateParameters(namedType);
                    default:
                        Debug.Assert(false);
                        return default;
                }
            }

            static ImmutableArray<ParameterSymbol> getDelegateParameters(NamedTypeSymbol delegateType)
            {
                Debug.Assert(delegateType.IsDelegateType());
                if (delegateType.DelegateInvokeMethod is { } invokeMethod)
                {
                    return invokeMethod.Parameters;
                }

                Debug.Assert(false);
                return default;
            }

            static ImmutableArray<ParameterSymbol> getSetterParameters(SourcePropertyAccessorSymbol setter)
            {
                var parameters = setter.Parameters;
                Debug.Assert(parameters[^1] is SynthesizedAccessorValueParameterSymbol);
                return parameters.RemoveAt(parameters.Length - 1);
            }
        }
#nullable disable

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            VisitLambdaExpression(node);
        }

        private void VisitLambdaExpression(LambdaExpressionSyntax node)
        {
            // Do not descend into a lambda unless it is a root node
            if (_root != node)
            {
                return;
            }

            CSharpSyntaxNode body = node.Body;
            if (body.Kind() == SyntaxKind.Block)
            {
                VisitBlock((BlockSyntax)body);
            }
            else
            {
                var binder = new ExpressionVariableBinder(body, _enclosing);
                AddToMap(body, binder);
                Visit(body, binder);
            }
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            VisitLambdaExpression(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            Symbol oldMethod = _containingMemberOrLambda;
            Binder binder = _enclosing;
            LocalFunctionSymbol match = FindLocalFunction(node, _enclosing);

            if ((object)match != null)
            {
                _containingMemberOrLambda = match;

                binder = match.IsGenericMethod
                    ? new WithMethodTypeParametersBinder(match, _enclosing)
                    : _enclosing;

                binder = binder.SetOrClearUnsafeRegionIfNecessary(node.Modifiers,
                    isIteratorBody: match.IsIterator);

                binder = new InMethodBinder(match, binder);
            }

            BlockSyntax blockBody = node.Body;
            if (blockBody != null)
            {
                Visit(blockBody, binder);
            }

            ArrowExpressionClauseSyntax arrowBody = node.ExpressionBody;
            if (arrowBody != null)
            {
                Visit(arrowBody, binder);
            }

            _containingMemberOrLambda = oldMethod;
        }

        private static LocalFunctionSymbol FindLocalFunction(LocalFunctionStatementSyntax node, Binder enclosing)
        {
            LocalFunctionSymbol match = null;
            // Don't use LookupLocalFunction because it recurses up the tree, as it
            // should be defined in the directly enclosing block (see note below)

            Binder possibleScopeBinder = enclosing;
            while (possibleScopeBinder != null && !possibleScopeBinder.IsLocalFunctionsScopeBinder)
            {
                possibleScopeBinder = possibleScopeBinder.Next;
            }

            if (possibleScopeBinder != null)
            {
                foreach (var candidate in possibleScopeBinder.LocalFunctions)
                {
                    if (candidate.GetFirstLocation() == node.Identifier.GetLocation())
                    {
                        match = candidate;
                    }
                }
            }

            return match;
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            var arrowBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, arrowBinder);
            Visit(node.Expression, arrowBinder);
        }

        public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            var valueBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, valueBinder);
            Visit(node.Value, valueBinder);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            var attrBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, attrBinder);

            if (node.ArgumentList?.Arguments.Count > 0)
            {
                foreach (AttributeArgumentSyntax argument in node.ArgumentList.Arguments)
                {
                    Visit(argument.Expression, attrBinder);
                }
            }
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            var binder = _enclosing.WithAdditionalFlags(BinderFlags.ConstructorInitializer);
            AddToMap(node, binder);
            VisitConstructorInitializerArgumentList(node, node.ArgumentList, binder);
        }

        private void VisitConstructorInitializerArgumentList(CSharpSyntaxNode node, ArgumentListSyntax argumentList, Binder binder)
        {
            if (argumentList != null)
            {
                if (_root == node)
                {
                    binder = new ExpressionVariableBinder(argumentList, binder);
                    AddToMap(argumentList, binder);
                }

                Visit(argumentList, binder);
            }
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            // Do not descend into a lambda unless it is a root node
            if (_root != node)
            {
                return;
            }

            VisitBlock(node.Block);
        }

        public override void VisitGlobalStatement(GlobalStatementSyntax node)
        {
            Visit(node.Statement);
        }

        #endregion

        // Top-level block has an enclosing that is not a BinderContext. All others must (so that variables can be declared).
        public override void VisitBlock(BlockSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var blockBinder = new BlockBinder(_enclosing, node);
            AddToMap(node, blockBinder);

            // Visit all the statements inside this block
            foreach (StatementSyntax statement in node.Statements)
            {
                Visit(statement, blockBinder);
            }
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var usingBinder = new UsingStatementBinder(_enclosing, node);
            AddToMap(node, usingBinder);

            ExpressionSyntax expressionSyntax = node.Expression;
            VariableDeclarationSyntax declarationSyntax = node.Declaration;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            if (expressionSyntax != null)
            {
                Visit(expressionSyntax, usingBinder);
            }
            else
            {
                VisitRankSpecifiers(declarationSyntax.Type, usingBinder);

                foreach (VariableDeclaratorSyntax declarator in declarationSyntax.Variables)
                {
                    Visit(declarator, usingBinder);
                }
            }

            VisitPossibleEmbeddedStatement(node.Statement, usingBinder);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var whileBinder = new WhileBinder(_enclosing, node);
            AddToMap(node, whileBinder);

            Visit(node.Condition, whileBinder);
            VisitPossibleEmbeddedStatement(node.Statement, whileBinder);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var whileBinder = new WhileBinder(_enclosing, node);
            AddToMap(node, whileBinder);

            Visit(node.Condition, whileBinder);
            VisitPossibleEmbeddedStatement(node.Statement, whileBinder);
        }

        public override void VisitDeleteStatement(DeleteStatementSyntax node)
        {
            Visit(node.NameSyntax);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            Binder binder = new ForLoopBinder(_enclosing, node);
            AddToMap(node, binder);

            VariableDeclarationSyntax declaration = node.Declaration;
            if (declaration != null)
            {
                VisitRankSpecifiers(declaration.Type, binder);

                foreach (VariableDeclaratorSyntax variable in declaration.Variables)
                {
                    Visit(variable, binder);
                }
            }
            else
            {
                foreach (ExpressionSyntax initializer in node.Initializers)
                {
                    Visit(initializer, binder);
                }
            }

            ExpressionSyntax condition = node.Condition;
            if (condition != null)
            {
                binder = new ExpressionVariableBinder(condition, binder);
                AddToMap(condition, binder);
                Visit(condition, binder);
            }

            SeparatedSyntaxList<ExpressionSyntax> incrementors = node.Incrementors;
            if (incrementors.Count > 0)
            {
                var incrementorsBinder = new ExpressionListVariableBinder(incrementors, binder);
                AddToMap(incrementors.First(), incrementorsBinder);
                foreach (ExpressionSyntax incrementor in incrementors)
                {
                    Visit(incrementor, incrementorsBinder);
                }
            }

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        private void VisitCommonForEachStatement(CommonForEachStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var patternBinder = new ExpressionVariableBinder(node.Expression, _enclosing);

            AddToMap(node.Expression, patternBinder);
            Visit(node.Expression, patternBinder);

            var binder = new ForEachLoopBinder(patternBinder, node);
            AddToMap(node, binder);

            if (node is ForEachVariableStatementSyntax forEachVariable && !forEachVariable.Variable.IsDeconstructionLeft())
            {
                // We will bind this expression for error recovery, anything could be there
                Visit(forEachVariable.Variable, binder);
            }

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            VisitCommonForEachStatement(node);
        }

        public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
        {
            VisitCommonForEachStatement(node);
        }

        public override void VisitCheckedExpression(CheckedExpressionSyntax node)
        {
            Binder binder = _enclosing.WithCheckedOrUncheckedRegion(@checked: node.Kind() == SyntaxKind.CheckedExpression);
            AddToMap(node, binder);
            Visit(node.Expression, binder);
        }

        public override void VisitCheckedStatement(CheckedStatementSyntax node)
        {
            Binder binder = _enclosing.WithCheckedOrUncheckedRegion(@checked: node.Kind() == SyntaxKind.CheckedStatement);
            AddToMap(node, binder);

            Visit(node.Block, binder);
        }

        public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            Binder binder = _enclosing.WithAdditionalFlags(BinderFlags.UnsafeRegion);
            AddToMap(node, binder);

            Visit(node.Block, binder); // This will create the block binder for the block.
        }

        public override void VisitFixedStatement(FixedStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var binder = new FixedStatementBinder(_enclosing, node);
            AddToMap(node, binder);

            if (node.Declaration != null)
            {
                VisitRankSpecifiers(node.Declaration.Type, binder);

                foreach (VariableDeclaratorSyntax declarator in node.Declaration.Variables)
                {
                    Visit(declarator, binder);
                }
            }

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            var lockBinder = new LockBinder(_enclosing, node);
            AddToMap(node, lockBinder);

            Visit(node.Expression, lockBinder);

            StatementSyntax statement = node.Statement;
            Binder statementBinder = lockBinder.WithAdditionalFlags(BinderFlags.InLockBody);
            if (statementBinder != lockBinder)
            {
                AddToMap(statement, statementBinder);
            }

            VisitPossibleEmbeddedStatement(statement, statementBinder);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            AddToMap(node.Expression, _enclosing);
            Visit(node.Expression, _enclosing);

            var switchBinder = SwitchBinder.Create(_enclosing, node);
            AddToMap(node, switchBinder);

            foreach (SwitchSectionSyntax section in node.Sections)
            {
                Visit(section, switchBinder);
            }
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            var patternBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, patternBinder);

            foreach (SwitchLabelSyntax label in node.Labels)
            {
                switch (label.Kind())
                {
                    case SyntaxKind.CasePatternSwitchLabel:
                        {
                            var switchLabel = (CasePatternSwitchLabelSyntax)label;
                            Visit(switchLabel.Pattern, patternBinder);
                            if (switchLabel.WhenClause != null)
                            {
                                Visit(switchLabel.WhenClause.Condition, patternBinder);
                            }
                            break;
                        }
                    case SyntaxKind.CaseSwitchLabel:
                        {
                            var switchLabel = (CaseSwitchLabelSyntax)label;
                            Visit(switchLabel.Value, patternBinder);
                            break;
                        }
                }
            }

            foreach (StatementSyntax statement in node.Statements)
            {
                Visit(statement, patternBinder);
            }
        }

        public override void VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            var switchExpressionBinder = new SwitchExpressionBinder(node, _enclosing);
            AddToMap(node, switchExpressionBinder);
            Visit(node.GoverningExpression, switchExpressionBinder);
            foreach (SwitchExpressionArmSyntax arm in node.Arms)
            {
                var armScopeBinder = new ExpressionVariableBinder(arm, switchExpressionBinder);
                var armBinder = new SwitchExpressionArmBinder(arm, armScopeBinder, switchExpressionBinder);
                AddToMap(arm, armBinder);
                Visit(arm.Pattern, armBinder);
                if (arm.WhenClause != null)
                {
                    Visit(arm.WhenClause, armBinder);
                }

                Visit(arm.Expression, armBinder);
            }
        }

        public override void VisitBinaryPattern(BinaryPatternSyntax node)
        {
            // Users (such as ourselves) can have many, many nested binary patterns. To avoid crashing, do left recursion manually.
            while (true)
            {
                Visit(node.Right);
                if (node.Left is not BinaryPatternSyntax binOp)
                {
                    Visit(node.Left);
                    break;
                }

                node = binOp;
            }
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            Binder enclosing = _enclosing;
            while (true)
            {
                Visit(node.Condition, enclosing);
                VisitPossibleEmbeddedStatement(node.Statement, enclosing);

                if (node.Else == null)
                {
                    break;
                }

                var elseStatementSyntax = node.Else.Statement;
                if (elseStatementSyntax is IfStatementSyntax ifStatementSyntax)
                {
                    node = ifStatementSyntax;
                    enclosing = GetBinderForPossibleEmbeddedStatement(node, enclosing);
                }
                else
                {
                    VisitPossibleEmbeddedStatement(elseStatementSyntax, enclosing);
                    break;
                }
            }
        }

        public override void VisitElseClause(ElseClauseSyntax node)
        {
            VisitPossibleEmbeddedStatement(node.Statement, _enclosing);
        }

        public override void VisitLabeledStatement(LabeledStatementSyntax node)
        {
            Visit(node.Statement, _enclosing);
        }

        public override void VisitTryStatement(TryStatementSyntax node)
        {
            if (node.Catches.Any())
            {
                // NOTE: We're going to cheat a bit - we know that the block is definitely going
                // to get a map entry, so we don't need to worry about the WithAdditionalFlags
                // binder being dropped.  That is, there's no point in adding the WithAdditionalFlags
                // binder to the map ourselves and having VisitBlock unconditionally overwrite it.
                Visit(node.Block, _enclosing.WithAdditionalFlags(BinderFlags.InTryBlockOfTryCatch));
            }
            else
            {
                Visit(node.Block, _enclosing);
            }

            foreach (CatchClauseSyntax c in node.Catches)
            {
                Visit(c, _enclosing);
            }

            if (node.Finally != null)
            {
                Visit(node.Finally, _enclosing);
            }
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var clauseBinder = new CatchClauseBinder(_enclosing, node);
            AddToMap(node, clauseBinder);

            if (node.Filter != null)
            {
                Binder filterBinder = clauseBinder.WithAdditionalFlags(BinderFlags.InCatchFilter);
                AddToMap(node.Filter, filterBinder);
                Visit(node.Filter, filterBinder);
            }

            Visit(node.Block, clauseBinder);
        }

        public override void VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            Visit(node.FilterExpression);
        }

        public override void VisitFinallyClause(FinallyClauseSyntax node)
        {
            // NOTE: We're going to cheat a bit - we know that the block is definitely going
            // to get a map entry, so we don't need to worry about the WithAdditionalFlags
            // binder being dropped.  That is, there's no point in adding the WithAdditionalFlags
            // binder to the map ourselves and having VisitBlock unconditionally overwrite it.

            // If this finally block is nested inside a catch block, we need to use a distinct
            // binder flag so that we can detect the nesting order for error CS074: A throw
            // statement with no arguments is not allowed in a finally clause that is nested inside
            // the nearest enclosing catch clause.

            var additionalFlags = BinderFlags.InFinallyBlock;
            if (_enclosing.Flags.Includes(BinderFlags.InCatchBlock))
            {
                additionalFlags |= BinderFlags.InNestedFinallyBlock;
            }

            Visit(node.Block, _enclosing.WithAdditionalFlags(additionalFlags));

            Binder finallyBinder;
            Debug.Assert(_map.TryGetValue(node.Block, out finallyBinder) && finallyBinder.Flags.Includes(BinderFlags.InFinallyBlock));
        }

        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression, _enclosing);
            }
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            Visit(node.Expression, _enclosing);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            VisitRankSpecifiers(node.Declaration.Type, _enclosing);

            foreach (VariableDeclaratorSyntax decl in node.Declaration.Variables)
            {
                Visit(decl);
            }
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            Visit(node.ArgumentList);

            if (node.Initializer is { } initializer)
            {
                var enclosing = _enclosing;
                if (node.Parent is VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax { IsConst: true } })
                {
                    enclosing = new LocalInProgressBinder(initializer, _enclosing);
                    AddToMap(initializer, enclosing);
                }

                Visit(initializer.Value, enclosing);
            }
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression, _enclosing);
            }
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression, _enclosing);
            }
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // The binary operators (except ??) are left-associative, and expressions of the form
            // a + b + c + d .... are relatively common in machine-generated code. The parser can handle
            // creating a deep-on-the-left syntax tree no problem, and then we promptly blow the stack.

            // For the purpose of creating binders, the order, in which we visit expressions, is not
            // significant.
            while (true)
            {
                Visit(node.Right);
                var binOp = node.Left as BinaryExpressionSyntax;
                if (binOp == null)
                {
                    Visit(node.Left);
                    break;
                }

                node = binOp;
            }
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            // We should only get here for statements that don't introduce new scopes.
            // Given pattern variables, they must have no subexpressions either.
            // It is fine to get here for non-statements.
            base.DefaultVisit(node);
        }

        private void AddToMap(SyntaxNode node, Binder binder)
        {
            // If this ever breaks, make sure that all callers of
            // CanHaveAssociatedLocalBinder are in sync.
            Debug.Assert(node.CanHaveAssociatedLocalBinder() ||
                (node == _root && node is ExpressionSyntax));

            // Cleverness: for some nodes (e.g. lock), we want to specify a binder flag that
            // applies to the embedded statement, but not to the entire node.  Since the
            // embedded statement may or may not have its own binder, we need a way to ensure
            // that the flag is set regardless.  We accomplish this by adding a binder for
            // the embedded statement immediately, and then overwriting it with one constructed
            // in the usual way, if there is such a binder.  That's why we're using update,
            // rather than add, semantics.
            Binder existing;
            // Note that a lock statement has two outer binders (a second one for pattern variable scope)
            Debug.Assert(!_map.TryGetValue(node, out existing) || existing == binder || existing == binder.Next || existing == binder.Next?.Next);

            _map[node] = binder;
        }

        /// <summary>
        /// Some statements by default do not introduce its own scope for locals.
        /// For example: Expression Statement, Return Statement, etc. However,
        /// when a statement like that is an embedded statement (like IfStatementSyntax.Statement),
        /// then it should introduce a scope for locals declared within it.
        /// Here we are detecting such statements and creating a binder that should own the scope.
        /// </summary>
        private Binder GetBinderForPossibleEmbeddedStatement(StatementSyntax statement, Binder enclosing, out CSharpSyntaxNode embeddedScopeDesignator)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                case SyntaxKind.LabeledStatement:
                case SyntaxKind.LocalFunctionStatement:
                // It is an error to have a declaration or a label in an embedded statement,
                // but we still want to bind it.

                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.LockStatement:
                case SyntaxKind.IfStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                    Debug.Assert((object)_containingMemberOrLambda == enclosing.ContainingMemberOrLambda);
                    embeddedScopeDesignator = statement;
                    return new EmbeddedStatementBinder(enclosing, statement);

                case SyntaxKind.SwitchStatement:
                    Debug.Assert((object)_containingMemberOrLambda == enclosing.ContainingMemberOrLambda);
                    var switchStatement = (SwitchStatementSyntax)statement;
                    embeddedScopeDesignator = switchStatement.Expression;
                    return new ExpressionVariableBinder(switchStatement.Expression, enclosing);

                default:
                    embeddedScopeDesignator = null;
                    return enclosing;
            }
        }

        private Binder GetBinderForPossibleEmbeddedStatement(StatementSyntax statement, Binder enclosing)
        {
            CSharpSyntaxNode embeddedScopeDesignator;
            // Some statements by default do not introduce its own scope for locals.
            // For example: Expression Statement, Return Statement, etc. However,
            // when a statement like that is an embedded statement (like IfStatementSyntax.Statement),
            // then it should introduce a scope for locals declared within it. Here we are detecting
            // such statements and creating a binder that should own the scope.
            enclosing = GetBinderForPossibleEmbeddedStatement(statement, enclosing, out embeddedScopeDesignator);

            if (embeddedScopeDesignator != null)
            {
                AddToMap(embeddedScopeDesignator, enclosing);
            }

            return enclosing;
        }

        private void VisitPossibleEmbeddedStatement(StatementSyntax statement, Binder enclosing)
        {
            if (statement != null)
            {
                enclosing = GetBinderForPossibleEmbeddedStatement(statement, enclosing);
                Visit(statement, enclosing);
            }
        }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            Visit(node.FromClause.Expression);
            Visit(node.Body);
        }

        public override void VisitQueryBody(QueryBodySyntax node)
        {
            foreach (QueryClauseSyntax clause in node.Clauses)
            {
                if (clause.Kind() == SyntaxKind.JoinClause)
                {
                    Visit(((JoinClauseSyntax)clause).InExpression);
                }
            }

            Visit(node.Continuation);
        }
    }
}
