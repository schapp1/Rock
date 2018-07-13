﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;

using Rock;

/// <summary>
/// MEF Directory Catalog that will handle outdated MEF Components
/// </summary>
public class SafeDirectoryCatalog : ComposablePartCatalog
{
    private readonly AggregateCatalog _catalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeDirectoryCatalog" /> class.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <param name="baseType">Type of the base.</param>
    public SafeDirectoryCatalog( string directory, Type baseType )
    {
        // blacklist of files that would never have Rock MEF components
        string[] ignoredFileStart = { "Lucene.", "Microsoft.", "msvcr100.", "System." };

        // get all *.dll in the current and subdirectories, except for *.resources.dll and the sql server type library files 
        var files = Directory.EnumerateFiles( directory, "*.dll", SearchOption.AllDirectories )
                        .Where( a => !a.EndsWith( ".resources.dll", StringComparison.OrdinalIgnoreCase )
                                    &&  !ignoredFileStart.Any( i => Path.GetFileName(a).StartsWith(i, StringComparison.OrdinalIgnoreCase) ));

        _catalog = new AggregateCatalog();
        string baseTypeAssemblyName = baseType.Assembly.GetName().Name;

        var loadedAssembliesDictionary = AppDomain.CurrentDomain.GetAssemblies().Where( a => !a.IsDynamic && !a.GlobalAssemblyCache && !string.IsNullOrWhiteSpace( a.Location ) )
            .DistinctBy(k => new Uri( k.CodeBase ).LocalPath )
            .ToDictionary( k => new Uri( k.CodeBase ).LocalPath, v => v, StringComparer.OrdinalIgnoreCase );

        foreach ( var file in files )
        {
            try
            {
                Assembly loadedAssembly = loadedAssembliesDictionary.GetValueOrNull( file );

                AssemblyCatalog assemblyCatalog = null;

                if ( loadedAssembly != null )
                {
                    if ( loadedAssembly == baseType.Assembly || loadedAssembly.GetReferencedAssemblies().Any( a => a.Name.Equals( baseTypeAssemblyName, StringComparison.OrdinalIgnoreCase ) ) )
                    {
                        assemblyCatalog = new AssemblyCatalog( loadedAssembly );
                    }
                }
                else
                {
                    assemblyCatalog = new AssemblyCatalog( file );
                }

                if ( assemblyCatalog != null )
                {
                    // Force MEF to load the plugin and figure out if there are any exports
                    // good assemblies will not throw the RTLE exception and can be added to the catalog
                    if ( assemblyCatalog.Parts.ToList().Count > 0 )
                    {
                        _catalog.Catalogs.Add( assemblyCatalog );
                    }
                }
            }
            catch ( ReflectionTypeLoadException e )
            {
                foreach ( var loaderException in e.LoaderExceptions )
                {
                    Rock.Model.ExceptionLogService.LogException( new Exception( "Unable to load MEF from " + file, loaderException ) );
                }

                string msg = e.Message;
            }
        }
    }

    /// <summary>
    /// Gets the part definitions that are contained in the catalog.
    /// </summary>
    /// <returns>The <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" /> contained in the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartCatalog" />.</returns>
    public override IQueryable<ComposablePartDefinition> Parts
    {
        get { return _catalog.Parts; }
    }
}