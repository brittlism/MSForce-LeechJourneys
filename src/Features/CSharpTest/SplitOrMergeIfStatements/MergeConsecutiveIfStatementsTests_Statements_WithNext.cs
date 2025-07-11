﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

public sealed partial class MergeConsecutiveIfStatementsTests
{
    [Theory]
    [InlineData("[||]if (a)")]
    [InlineData("i[||]f (a)")]
    [InlineData("if[||] (a)")]
    [InlineData("if [||](a)")]
    [InlineData("if (a)[||]")]
    [InlineData("[|if|] (a)")]
    [InlineData("[|if (a)|]")]
    public Task MergedIntoNextStatementOnIfSpans(string ifLine)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    
            """ + ifLine + """

                        return;
                    if (b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoNextStatementOnIfExtendedHeaderSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
            [|        if (a)
                        return;
            |]        if (b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoNextStatementOnIfFullSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                        return;|]
                    if (b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoNextStatementOnIfExtendedFullSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
            [|        if (a)
                        return;
            |]        if (b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Theory]
    [InlineData("if ([||]a)")]
    [InlineData("[|i|]f (a)")]
    [InlineData("[|if (|]a)")]
    [InlineData("if [|(|]a)")]
    [InlineData("if (a[|)|]")]
    [InlineData("if ([|a|])")]
    [InlineData("if [|(a)|]")]
    public Task NotMergedIntoNextStatementOnIfSpans(string ifLine)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    
            """ + ifLine + """

                        return;
                    if (b)
                        return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoNextStatementOnIfOverreachingSelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                    |]    return;
                    if (b)
                      return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoNextStatementOnIfBodySelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        [|return;|]
                    if (b)
                        return;
                }
            }
            """);
}
