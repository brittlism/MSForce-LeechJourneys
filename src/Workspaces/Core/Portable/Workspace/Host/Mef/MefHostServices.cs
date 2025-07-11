﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host.Mef;

public class MefHostServices(CompositionContext compositionContext) : HostServices, IMefHostExportProvider
{
    internal delegate MefHostServices CreationHook(IEnumerable<Assembly> assemblies);

    /// <summary>
    /// This delegate allows test code to override the behavior of <see cref="Create(IEnumerable{Assembly})"/>.
    /// </summary>
    /// <seealso cref="TestAccessor.HookServiceCreation"/>
    private static CreationHook s_creationHook;

    public static MefHostServices Create(CompositionContext compositionContext)
    {
        if (compositionContext == null)
        {
            throw new ArgumentNullException(nameof(compositionContext));
        }

        return new MefHostServices(compositionContext);
    }

    public static MefHostServices Create(IEnumerable<System.Reflection.Assembly> assemblies)
    {
        if (assemblies == null)
        {
            throw new ArgumentNullException(nameof(assemblies));
        }

        if (s_creationHook != null)
        {
            return s_creationHook(assemblies);
        }

        var compositionConfiguration = new ContainerConfiguration().WithAssemblies(assemblies.Distinct());
        var container = compositionConfiguration.CreateContainer();
        return new MefHostServices(container);
    }

    protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
        => new MefWorkspaceServices(this, workspace);

    IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
        => compositionContext.GetExports<TExtension>().Select(e => new Lazy<TExtension>(() => e));

    IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
    {
        var importer = new WithMetadataImporter<TExtension, TMetadata>();
        compositionContext.SatisfyImports(importer);
        return importer.Exports;
    }

    private sealed class WithMetadataImporter<TExtension, TMetadata>
    {
        [ImportMany]
        public IEnumerable<Lazy<TExtension, TMetadata>> Exports { get; set; }
    }

    #region Defaults

    public static MefHostServices DefaultHost
    {
        get
        {
            if (field == null)
            {
                var host = Create(DefaultAssemblies);
                Interlocked.CompareExchange(ref field, host, null);
            }

            return field;
        }

        private set;
    }

    public static ImmutableArray<Assembly> DefaultAssemblies
    {
        get
        {
            if (field.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref field, LoadDefaultAssemblies());
            }

            return field;
        }

        private set;
    }

    // Used to build a MEF composition using the main workspaces assemblies and the known VisualBasic/CSharp workspace assemblies.
    // updated: includes feature assemblies since they now have public API's.
    internal static ImmutableArray<string> DefaultAssemblyNames =
        [
            "Microsoft.CodeAnalysis.Workspaces",
            "Microsoft.CodeAnalysis.CSharp.Workspaces",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
            "Microsoft.CodeAnalysis.Features",
            "Microsoft.CodeAnalysis.CSharp.Features",
            "Microsoft.CodeAnalysis.VisualBasic.Features"
        ];

    internal static bool IsDefaultAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        return DefaultAssemblyNames.Contains(name);
    }

    private static ImmutableArray<Assembly> LoadDefaultAssemblies()
        => MefHostServicesHelpers.LoadNearbyAssemblies(DefaultAssemblyNames);

    #endregion

    internal readonly struct TestAccessor
    {
        /// <summary>
        /// Injects replacement behavior for the <see cref="Create(IEnumerable{Assembly})"/> method.
        /// </summary>
        internal static void HookServiceCreation(CreationHook hook)
        {
            s_creationHook = hook;

            // The existing host, if any, is not retained past this call.
            DefaultHost = null;
        }
    }
}
