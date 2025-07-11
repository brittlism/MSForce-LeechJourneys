﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting;

[Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
public sealed class LoopHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(LoopHighlighter);

    [Fact]
    public Task TestExample1_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|while|]|} (true)
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|while|] (true)
                    {
                        if (x)
                        {
                            {|Cursor:[|break|]|};
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|while|] (true)
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            {|Cursor:[|continue|]|};
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|do|]|}
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                    [|while|] (true);
                }
            }
            """);

    [Fact]
    public Task TestExample2_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|do|]
                    {
                        if (x)
                        {
                            {|Cursor:[|break|]|};
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                    [|while|] (true);
                }
            }
            """);

    [Fact]
    public Task TestExample2_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|do|]
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            {|Cursor:[|continue|]|};
                        }
                    }
                    [|while|] (true);
                }
            }
            """);

    [Fact]
    public Task TestExample2_4()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|do|]
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                    {|Cursor:[|while|]|} (true);
                }
            }
            """);

    [Fact]
    public Task TestExample2_5()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    do
                    {
                        if (x)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    while {|Cursor:(true)|};
                }
            }
            """);

    [Fact]
    public Task TestExample2_6()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|do|]
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                    [|while|] (true);{|Cursor:|}
                }
            }
            """);

    [Fact]
    public Task TestExample3_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|for|]|} (int i = 0; i < 10; i++)
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample3_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|for|] (int i = 0; i < 10; i++)
                    {
                        if (x)
                        {
                            {|Cursor:[|break|];|}
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample3_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|for|] (int i = 0; i < 10; i++)
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            {|Cursor:[|continue|];|}
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample4_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|foreach|]|} (var a in x)
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample4_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|foreach|] (var a in x)
                    {
                        if (x)
                        {
                            {|Cursor:[|break|];|}
                        }
                        else
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample4_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|foreach|] (var a in x)
                    {
                        if (x)
                        {
                            [|break|];
                        }
                        else
                        {
                            {|Cursor:[|continue|];|}
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|foreach|]|} (var a in x)
                    {
                        if (a)
                        {
                            [|break|];
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|foreach|] (var a in x)
                    {
                        if (a)
                        {
                            {|Cursor:[|break|];|}
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        {|Cursor:[|do|]|}
                                        {
                                            [|break|];
                                        }
                                        [|while|] (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_4()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        [|do|]
                                        {
                                            {|Cursor:[|break|];|}
                                        }
                                        [|while|] (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_5()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        [|do|]
                                        {
                                            [|break|];
                                        }
                                        {|Cursor:[|while|]|} (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_6()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        [|do|]
                                        {
                                            [|break|];
                                        }
                                        [|while|] (false);{|Cursor:|}
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_7()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    {|Cursor:[|while|]|} (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        [|break|];
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_8()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    [|while|] (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        {|Cursor:[|break|];|}
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    // TestNestedExample1 9-13 are in SwitchStatementHighlighterTests.cs

    [Fact]
    public Task TestNestedExample1_14()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        {|Cursor:[|for|]|} (int i = 0; i < 10; i++)
                        {
                            [|break|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_15()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        [|for|] (int i = 0; i < 10; i++)
                        {
                            {|Cursor:[|break|];|}
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|foreach|]|} (var a in x)
                    {
                        if (a)
                        {
                            [|continue|];
                        }
                        else
                        {
                            while (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while (false);
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|foreach|] (var a in x)
                    {
                        if (a)
                        {
                            {|Cursor:[|continue|];|}
                        }
                        else
                        {
                            while (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while (false);
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                {|Cursor:[|do|]|}
                                {
                                    [|continue|];
                                }
                                [|while|] (false);
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_4()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                [|do|]
                                {
                                    {|Cursor:[|continue|];|}
                                }
                                [|while|] (false);
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_5()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                [|do|]
                                {
                                    [|continue|];
                                }
                                {|Cursor:[|while|]|} (false);
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_6()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while {|Cursor:(false)|};
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_7()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                [|do|]
                                {
                                    [|continue|];
                                }
                                [|while|] (false);{|Cursor:|}
                                continue;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_8()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            {|Cursor:[|while|]|} (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while (false);
                                [|continue|];
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_9()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            [|while|] (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while (false);
                                {|Cursor:[|continue|];|}
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            continue;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_10()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while (false);
                                continue;
                            }
                        }

                        {|Cursor:[|for|]|} (int i = 0; i < 10; i++)
                        {
                            [|continue|];
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample2_11()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                            while (true)
                            {
                                do
                                {
                                    continue;
                                }
                                while (false);
                                continue;
                            }
                        }

                        [|for|] (int i = 0; i < 10; i++)
                        {
                            {|Cursor:[|continue|];|}
                        }
                    }
                }
            }
            """);
}
