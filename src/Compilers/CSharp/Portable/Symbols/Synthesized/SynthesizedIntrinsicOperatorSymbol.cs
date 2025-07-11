﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedIntrinsicOperatorSymbol : MethodSymbol
    {
        private readonly TypeSymbol _containingType;
        private readonly string _name;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly TypeSymbol _returnType;

        public SynthesizedIntrinsicOperatorSymbol(TypeSymbol leftType, string name, TypeSymbol rightType, TypeSymbol returnType)
        {
            if (leftType.Equals(rightType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
            {
                _containingType = leftType;
            }
            else if (rightType.Equals(returnType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
            {
                _containingType = rightType;
            }
            else
            {
                Debug.Assert(leftType.Equals(returnType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                _containingType = leftType;
            }

            _name = name;
            _returnType = returnType;

            Debug.Assert((leftType.IsDynamic() || rightType.IsDynamic()) == returnType.IsDynamic());
            Debug.Assert(_containingType.IsDynamic() == returnType.IsDynamic());

            _parameters = ImmutableArray.Create<ParameterSymbol>(new SynthesizedOperatorParameterSymbol(this, leftType, 0, "left"),
                                                      new SynthesizedOperatorParameterSymbol(this, rightType, 1, "right"));
        }

        public SynthesizedIntrinsicOperatorSymbol(TypeSymbol container, string name, TypeSymbol returnType)
        {
            _containingType = container;
            _name = name;
            _returnType = returnType;
            _parameters = ImmutableArray.Create<ParameterSymbol>(new SynthesizedOperatorParameterSymbol(this, container, 0, "value"));
        }

        public override bool IsCheckedBuiltin => SyntaxFacts.IsCheckedOperator(this.Name);

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.BuiltinOperator;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        internal override CSharpCompilation DeclaringCompilation
        {
            get
            {
                return null;
            }
        }

        public override string GetDocumentationCommentId()
        {
            return null;
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return false;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return true;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return System.Reflection.MethodImplAttributes.Managed;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get
            {
                return false;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        public override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return null;
            }
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return false;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        public override bool IsVararg
        {
            get
            {
                return false;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return false;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return false;
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return RefKind.None;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                return TypeWithAnnotations.Create(_returnType);
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get
            {
                return ImmutableArray<TypeWithAnnotations>.Empty;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        // operators are never 'readonly' because there is no 'this' parameter
        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                return Cci.CallingConvention.Default;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get
            {
                return false;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType as NamedTypeSymbol;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.Public;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return null;
            }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool IsNullableAnalysisEnabled() => false;

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => false;

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var other = obj as SynthesizedIntrinsicOperatorSymbol;

            if ((object)other == null)
            {
                return false;
            }

            if (_parameters.Length == other._parameters.Length &&
                string.Equals(_name, other._name, StringComparison.Ordinal) &&
                TypeSymbol.Equals(_containingType, other._containingType, compareKind) &&
                TypeSymbol.Equals(_returnType, other._returnType, compareKind))
            {
                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (!TypeSymbol.Equals(_parameters[i].Type, other._parameters[i].Type, compareKind))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_name, Hash.Combine(_containingType, _parameters.Length));
        }

        private sealed class SynthesizedOperatorParameterSymbol : SynthesizedParameterSymbolBase
        {
            public SynthesizedOperatorParameterSymbol(
                SynthesizedIntrinsicOperatorSymbol container,
                TypeSymbol type,
                int ordinal,
                string name
            ) : base(container, TypeWithAnnotations.Create(type), ordinal, RefKind.None, ScopedKind.None, name)
            {
            }

            internal override bool IsMetadataIn => RefKind is RefKind.In or RefKind.RefReadOnlyParameter;

            internal override bool IsMetadataOut => RefKind == RefKind.Out;

            public override bool Equals(Symbol obj, TypeCompareKind compareKind)
            {
                if (obj == (object)this)
                {
                    return true;
                }

                var other = obj as SynthesizedOperatorParameterSymbol;

                if ((object)other == null)
                {
                    return false;
                }

                return Ordinal == other.Ordinal && ContainingSymbol.Equals(other.ContainingSymbol, compareKind);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(ContainingSymbol, Ordinal.GetHashCode());
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get { return ImmutableArray<CustomModifier>.Empty; }
            }

            internal override bool HasEnumeratorCancellationAttribute => false;

            internal override MarshalPseudoCustomAttributeData MarshallingInformation
            {
                get { return null; }
            }

            internal override bool HasUnscopedRefAttribute => false;
        }

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override int TryGetOverloadResolutionPriority()
        {
            return 0;
        }
    }
}
