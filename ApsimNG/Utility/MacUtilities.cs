using System;
using System.Diagnostics;
using APSIM.Shared.Utilities;

namespace Utility
{
    public static class MacUtilities
    {
        public static bool DarkThemeEnabled()
        {
            // Can't use ProcessWithRedirectedOutput because it tries to run sh in shell mode.
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "/bin/sh";
            p.StartInfo.Arguments = "-c 'defaults read -g AppleInterfaceStyle'";
            p.Start();
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd().Trim(Environment.NewLine.ToCharArray());
            return output == "Dark";
        }
    }
}
