// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using UnityEditor;

namespace UnityEditorInternal
{
    public static class Il2CppNativeCodeBuilderUtils
    {
        public static string GetConfigurationName(Il2CppCompilerConfiguration compilerConfiguration)
        {
            // In IL2CPP, Master config is called "ReleasePlus"
            return compilerConfiguration != Il2CppCompilerConfiguration.Master ? compilerConfiguration.ToString() : "ReleasePlus";
        }

        public static IEnumerable<string> AddBuilderArguments(Il2CppNativeCodeBuilder builder, string outputRelativePath, IEnumerable<string> includeRelativePaths, IEnumerable<string> additionalLibs, Il2CppCompilerConfiguration compilerConfiguration)
        {
            var arguments = new List<string>();

            arguments.Add("--compile-cpp");
            if (builder.LinkLibIl2CppStatically)
                arguments.Add("--libil2cpp-static");
            arguments.Add(FormatArgument("platform", builder.CompilerPlatform));
            arguments.Add(FormatArgument("architecture", builder.CompilerArchitecture));
            arguments.Add(FormatArgument("configuration", GetConfigurationName(compilerConfiguration)));

            arguments.Add(FormatArgument("outputpath", builder.ConvertOutputFileToFullPath(outputRelativePath)));

            string cacheDirectory = null;
            if (!string.IsNullOrEmpty(builder.CacheDirectory) && !builder.OverriddenCacheDirectory)
            {
                cacheDirectory = IL2CPPBuilder.GetShortPathName(CacheDirectoryPathFor(builder.CacheDirectory));
                arguments.Add(FormatArgument("cachedirectory", cacheDirectory));
            }

            if (!string.IsNullOrEmpty(builder.CompilerFlags))
                arguments.Add(FormatArgument("compiler-flags", builder.CompilerFlags));

            if (!string.IsNullOrEmpty(builder.LinkerFlags))
            {
                if (cacheDirectory == null)
                    throw new ArgumentException("If you pass linkerflags, a cachedirectory also needs to be passed.");

                NPath templinkerflagsTxt = $"{cacheDirectory}/linkerflags/linkerflags.txt";
                templinkerflagsTxt.WriteAllText(builder.LinkerFlags);
                arguments.Add(FormatArgument("linker-flags-file", templinkerflagsTxt.ToString()));
            }

            if (!string.IsNullOrEmpty(builder.PluginPath))
                arguments.Add(FormatArgument("plugin", builder.PluginPath));

            foreach (var includePath in builder.ConvertIncludesToFullPaths(includeRelativePaths))
                arguments.Add(FormatArgument("additional-include-directories", includePath));
            foreach (var library in additionalLibs)
                arguments.Add(FormatArgument("additional-libraries", library));

            if (!string.IsNullOrEmpty(builder.BaselibLibraryDirectory))
                arguments.Add(FormatArgument("baselib-directory", builder.BaselibLibraryDirectory));

            arguments.Add("--avoid-dynamic-library-copy");

            arguments.AddRange(builder.AdditionalIl2CPPArguments);

            return arguments;
        }

        public static void ClearAndPrepareCacheDirectory(Il2CppNativeCodeBuilder builder)
        {
            var currentEditorVersion = InternalEditorUtility.GetFullUnityVersion();
            ClearCacheIfEditorVersionDiffers(builder, currentEditorVersion);
            PrepareCacheDirectory(builder, currentEditorVersion);
        }

        public static void ClearCacheIfEditorVersionDiffers(Il2CppNativeCodeBuilder builder, string currentEditorVersion)
        {
            var cacheDirectoryPath = CacheDirectoryPathFor(builder.CacheDirectory);
            if (Directory.Exists(cacheDirectoryPath))
            {
                if (!File.Exists(Path.Combine(builder.CacheDirectory, EditorVersionFilenameFor(currentEditorVersion))))
                    Directory.Delete(cacheDirectoryPath, true);
            }
        }

        public static void PrepareCacheDirectory(Il2CppNativeCodeBuilder builder, string currentEditorVersion)
        {
            var cacheDirectoryPath = CacheDirectoryPathFor(builder.CacheDirectory);
            Directory.CreateDirectory(cacheDirectoryPath);

            foreach (var previousEditorVersionFile in Directory.GetFiles(builder.CacheDirectory, EditorVersionFilenameFor("*")))
                File.Delete(previousEditorVersionFile);

            var versionFilePath = Path.Combine(builder.CacheDirectory, EditorVersionFilenameFor(currentEditorVersion));
            if (!File.Exists(versionFilePath))
                File.Create(versionFilePath).Dispose();
        }

        public static string ObjectFilePathInCacheDirectoryFor(string builderCacheDirectory)
        {
            return CacheDirectoryPathFor(builderCacheDirectory);
        }

        private static string CacheDirectoryPathFor(string builderCacheDirectory)
        {
            return builderCacheDirectory + "/il2cpp_cache";
        }

        private static string FormatArgument(string name, string value)
        {
            return string.Format("--{0}=\"{1}\"", name, EscapeEmbeddedQuotes(value));
        }

        private static string EditorVersionFilenameFor(string editorVersion)
        {
            return string.Format("il2cpp_cache {0}", editorVersion);
        }

        private static string EscapeEmbeddedQuotes(string value)
        {
            return value.Replace("\"", "\\\"");
        }
    }
}
