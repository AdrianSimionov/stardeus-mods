using System;
using System.IO;
using Anatawa12.AppleSiliconHarmony;
using Game;
using Game.Utils;
using KL.Utils;
using UnityEngine;

namespace HarmonyAppleSiliconFix {
    /// <summary>
    /// Stardeus 0.15.14+ uses Unity 6000.6, whose Mono runtime puts JIT code in
    /// MAP_JIT pages. MonoMod's mprotect-based detour platform cannot make those
    /// pages writable (mprotect returned EACCES), so every Harmony patch fails.
    ///
    /// This runs before any mod is initialized (BeforeCoreDefs) and applies the
    /// AppleSiliconHarmony patch, which routes detour writes through a native
    /// pthread_jit_write_protect_np helper. That makes Harmony work natively on
    /// arm64 without Rosetta.
    /// </summary>
    public static class HarmonyAppleSiliconFixInit {
        public const string ModId = "HarmonyAppleSiliconFix";
        private const string NativeFileName = "libapple_silicon_harmony_native.dylib";

        [SelfInit(SelfInit.Stage.BeforeCoreDefs, -10000)]
        private static void Apply() {
            try {
                if (!Patcher.PatchNeeded) {
                    Debug.Log("Harmony Apple Silicon Fix: not needed in this process");
                    return;
                }

                var nativePath = FindNativeLib();
                if (nativePath == null) {
                    D.Err("Harmony Apple Silicon Fix: {0} not found", NativeFileName);
                    return;
                }

                // Mono resolves the bare DllImport name as lib{name}.dylib in the
                // current directory. Do it while the current directory points at
                // the mod's Libraries folder, then restore it. The native library
                // handle stays cached for later Harmony patches.
                var previousDir = Environment.CurrentDirectory;
                try {
                    Environment.CurrentDirectory =
                        Path.GetDirectoryName(nativePath) ?? previousDir;
                    Patcher.Patch();
                } finally {
                    Environment.CurrentDirectory = previousDir;
                }

                Debug.Log("Harmony Apple Silicon Fix: Harmony patched for native arm64");
            } catch (Exception e) {
                D.Err("Harmony Apple Silicon Fix: failed to patch Harmony: {0}", e);
            }
        }

        private static string FindNativeLib() {
            try {
                var asmPath = typeof(HarmonyAppleSiliconFixInit).Assembly.Location;
                if (!string.IsNullOrEmpty(asmPath)) {
                    var path = Path.Combine(
                        Path.GetDirectoryName(asmPath) ?? string.Empty, NativeFileName);
                    if (File.Exists(path)) { return path; }
                }
            } catch { }

            try {
                var info = The.ModLoader?.FindModById(ModId);
                var path = info?.ModFile("Libraries/" + NativeFileName);
                if (path != null && File.Exists(path)) { return path; }
            } catch { }

            return null;
        }
    }
}