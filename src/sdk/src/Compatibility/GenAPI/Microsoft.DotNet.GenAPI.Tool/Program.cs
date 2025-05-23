// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.ApiSymbolExtensions.Logging;

namespace Microsoft.DotNet.GenAPI.Tool
{
    /// <summary>
    /// CLI frontend for the Roslyn-based GenAPI.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            // Global options
            Option<string[]> assembliesOption = new("--assembly")
            {
                Description = "The path to one or more assemblies or directories with assemblies.",
                CustomParser = ParseAssemblyArgument,
                Arity = ArgumentArity.OneOrMore,
                Required = true,
                Recursive = true
            };

            Option<string[]?> assemblyReferencesOption = new("--assembly-reference")
            {
                Description = "Paths to assembly references or their underlying directories for a specific target framework in the package.",
                CustomParser = ParseAssemblyArgument,
                Arity = ArgumentArity.ZeroOrMore,
                Recursive = true
            };

            Option<string[]?> excludeApiFilesOption = new("--exclude-api-file")
            {
                Description = "The path to one or more api exclusion files with types in DocId format.",
                CustomParser = ParseAssemblyArgument,
                Arity = ArgumentArity.ZeroOrMore,
                Recursive = true
            };

            Option<string[]?> excludeAttributesFilesOption = new("--exclude-attributes-file")
            {
                Description = "The path to one or more attribute exclusion files with types in DocId format.",
                CustomParser = ParseAssemblyArgument,
                Arity = ArgumentArity.ZeroOrMore,
                Recursive = true
            };

            Option<string?> outputPathOption = new("--output-path")
            {
                Description = @"Output path. Default is the console. Can specify an existing directory as well
            and then a file will be created for each assembly with the matching name of the assembly.",
                Recursive = true
            };

            Option<string?> headerFileOption = new("--header-file")
            {
                Description = "Specify a file with an alternate header content to prepend to output.",
                Recursive = true
            };

            Option<string?> exceptionMessageOption = new("--exception-message")
            {
                Description = "If specified - method bodies should throw PlatformNotSupportedException, else `throw null`.",
                Recursive = true
            };

            Option<bool> respectInternalsOption = new("--respect-internals")
            {
                Description = "If true, includes both internal and public API.",
                Recursive = true
            };

            Option<bool> includeAssemblyAttributesOption = new("--include-assembly-attributes")
            {
                Description = "Includes assembly attributes which are values that provide information about an assembly. Default is false."
            };

            RootCommand rootCommand = new("Microsoft.DotNet.GenAPI v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion)
            {
                TreatUnmatchedTokensAsErrors = true
            };

            rootCommand.Options.Add(assembliesOption);
            rootCommand.Options.Add(assemblyReferencesOption);
            rootCommand.Options.Add(excludeApiFilesOption);
            rootCommand.Options.Add(excludeAttributesFilesOption);
            rootCommand.Options.Add(outputPathOption);
            rootCommand.Options.Add(headerFileOption);
            rootCommand.Options.Add(exceptionMessageOption);
            rootCommand.Options.Add(respectInternalsOption);
            rootCommand.Options.Add(includeAssemblyAttributesOption);

            rootCommand.SetAction((ParseResult parseResult) =>
            {
                bool respectInternals = parseResult.GetValue(respectInternalsOption);

                ILog log = new ConsoleLog(MessageImportance.Normal);

                string[]? assemblies = parseResult.GetValue(assembliesOption);
                Debug.Assert(assemblies != null, "Assemblies cannot be null.");

                GenAPIApp.Run(log,
                    assemblies,
                    parseResult.GetValue(assemblyReferencesOption),
                    parseResult.GetValue(outputPathOption),
                    parseResult.GetValue(headerFileOption),
                    parseResult.GetValue(exceptionMessageOption),
                    parseResult.GetValue(excludeApiFilesOption),
                    parseResult.GetValue(excludeAttributesFilesOption),
                    respectInternals,
                    parseResult.GetValue(includeAssemblyAttributesOption)
                );
            });

            return rootCommand.Parse(args).Invoke();
        }

        private static string[] ParseAssemblyArgument(ArgumentResult argumentResult)
        {
            List<string> args = [];
            foreach (var token in argumentResult.Tokens)
            {
                args.AddRange(token.Value.Split(','));
            }

            return [.. args];
        }
    }
}
