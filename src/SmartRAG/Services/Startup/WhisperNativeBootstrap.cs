#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SmartRAG.Services.Startup
{
    internal static class WhisperNativeBootstrap
    {
        private static bool _ran;
        private static string? _nativeDirPath;

        public static void EnsureMacOsWhisperNativeLibraries()
        {
            if (_ran)
                return;
            _ran = true;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            var baseDir = AppContext.BaseDirectory ?? ".";
            var isArm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            var candidates = new[]
            {
                Path.Combine(baseDir, "runtimes", isArm64 ? "osx-arm64" : "osx-x64", "native"),
                Path.Combine(baseDir, "runtimes", isArm64 ? "macos-arm64" : "macos-x64")
            };

            string? nativeDir = null;
            foreach (var dir in candidates)
            {
                if (File.Exists(Path.Combine(dir, "libwhisper.dylib")))
                {
                    nativeDir = dir;
                    break;
                }
            }

            if (string.IsNullOrEmpty(nativeDir))
                return;

            _nativeDirPath = nativeDir;

            SetEnvironmentVariables(baseDir, nativeDir);
            PreloadAndSetResolver(nativeDir);
        }

        private static void SetEnvironmentVariables(string baseDir, string nativeDir)
        {
            var existing = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH");
            var value = string.IsNullOrEmpty(existing) ? nativeDir : nativeDir + ":" + existing;
            Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", value);

            var metalFile = Path.Combine(baseDir, "ggml-metal.metal");
            if (File.Exists(metalFile))
                Environment.SetEnvironmentVariable("GGML_METAL_PATH_RESOURCES", baseDir);
        }

        private static void PreloadAndSetResolver(string nativeDirPath)
        {
            var nativeLibraryType = Type.GetType("System.Runtime.InteropServices.NativeLibrary, System.Runtime.InteropServices");
            if (nativeLibraryType == null)
                return;

            var loadMethod = nativeLibraryType.GetMethod("Load", new[] { typeof(string) });
            if (loadMethod == null)
                return;

            var preloadOrder = new[]
            {
                "libggml-base-whisper.dylib",
                "libggml-blas-whisper.dylib",
                "libggml-metal-whisper.dylib",
                "libggml-cpu-whisper.dylib",
                "libggml-whisper.dylib",
                "libwhisper.dylib"
            };

            foreach (var lib in preloadOrder)
            {
                var path = Path.Combine(nativeDirPath, lib);
                if (File.Exists(path))
                {
                    try { loadMethod.Invoke(null, new object[] { path }); } catch { }
                }
            }

            Assembly? whisperAssembly;
            try
            {
                whisperAssembly = Assembly.Load(new AssemblyName("Whisper.net"));
            }
            catch
            {
                return;
            }

            if (whisperAssembly == null)
                return;

            var delegateType = Type.GetType("System.Runtime.InteropServices.DllImportResolver, System.Runtime.InteropServices");
            if (delegateType == null)
                return;

            var resolveMethod = typeof(WhisperNativeBootstrap).GetMethod(nameof(ResolveDllImport), BindingFlags.Static | BindingFlags.NonPublic);
            if (resolveMethod == null)
                return;

            var resolverDelegate = Delegate.CreateDelegate(delegateType, resolveMethod);

            foreach (var mi in nativeLibraryType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.Name != "SetDllImportResolver" || mi.GetParameters().Length != 2)
                    continue;
                try
                {
                    mi.Invoke(null, new object[] { whisperAssembly, resolverDelegate });
                    break;
                }
                catch { }
            }
        }

        private static IntPtr ResolveDllImport(string name, Assembly assembly, DllImportSearchPath? searchPath)
        {
            var nativeDir = _nativeDirPath;
            if (string.IsNullOrEmpty(nativeDir))
                return IntPtr.Zero;

            var nativeLibraryType = Type.GetType("System.Runtime.InteropServices.NativeLibrary, System.Runtime.InteropServices");
            if (nativeLibraryType == null)
                return IntPtr.Zero;

            var loadMethod = nativeLibraryType.GetMethod("Load", new[] { typeof(string) });
            if (loadMethod == null)
                return IntPtr.Zero;

            string dylibName;
            if (name.Contains(".dylib"))
                dylibName = name;
            else
                dylibName = (name.StartsWith("lib", StringComparison.Ordinal) ? name : "lib" + name) + ".dylib";

            var fullPath = Path.Combine(nativeDir, dylibName);
            if (File.Exists(fullPath))
            {
                try
                {
                    var ptr = loadMethod.Invoke(null, new object[] { fullPath });
                    return ptr is IntPtr p ? p : IntPtr.Zero;
                }
                catch { }
            }

            var fallback = Path.Combine(nativeDir, "libwhisper.dylib");
            if (File.Exists(fallback))
            {
                try
                {
                    var ptr = loadMethod.Invoke(null, new object[] { fallback });
                    return ptr is IntPtr p ? p : IntPtr.Zero;
                }
                catch { }
            }

            return IntPtr.Zero;
        }
    }
}
