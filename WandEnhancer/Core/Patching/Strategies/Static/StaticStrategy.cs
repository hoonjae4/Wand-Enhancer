using System;
using System.Diagnostics;
using System.IO;
using WandEnhancer.Core.Patching.Shared;

namespace WandEnhancer.Core.Patching.Strategies.Static
{
    internal sealed class StaticStrategy : IPatchStrategy
    {
        private const string ResourcesDirectoryName = "resources";
        private const string AppAsarFileName = "app.asar";

        public bool RequiresLauncherAlways => false;

        public void ApplyEnablement(PatchContext context)
        {
            if (!DiskFusePatch.Apply(context.Install.ExecutablePath, context.Log))
            {
                throw new Exception($"[ENHANCER] Could not clear the ASAR integrity fuse in {Path.GetFileName(context.Install.ExecutablePath)}.");
            }

            // Absent on builds before 12.61: not every build embeds this resource to rewrite.
            string asarPath = Path.Combine(context.Install.RootDirectory, ResourcesDirectoryName, AppAsarFileName);
            if (!AsarIntegrityResourcePatch.Patch(context.Install.ExecutablePath, asarPath, context.Log))
            {
                context.Log?.Invoke("[ENHANCER] No ASAR integrity resource found in Wand.exe; skipping (older Wand build).", ELogType.Info);
            }

            AuxiliaryCompatibility.Apply(context, true);
        }

        public bool Launch(PatchContext context, string args)
        {
            try
            {
                // dispose handle; process keeps running
                Process.Start(new ProcessStartInfo(context.Install.ExecutablePath, args ?? string.Empty)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(context.Install.ExecutablePath)
                })?.Dispose();
                return true;
            }
            catch (Exception e)
            {
                context.Log?.Invoke($"Could not start Wand: {e.Message}", ELogType.Error);
                return false;
            }
        }

    }
}
