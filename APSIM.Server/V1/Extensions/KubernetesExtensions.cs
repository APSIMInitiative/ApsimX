using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace APSIM.Server.Extensions
{
    /// <summary>
    /// Hack to use kubectl for file copying.
    /// </summary>
    internal static class KubernetesExtensions
    {
        private const string kubectlInstallLink = "https://kubernetes.io/docs/tasks/tools/";

        public static void CopyFileToPod(this Kubernetes client, V1Pod pod, string container, string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsKubectlInstalled())
                throw new Exception($"kubectl is either not installed or is not on path\n{kubectlInstallLink}");
            if (!(File.Exists(sourceFilePath) || Directory.Exists(sourceFilePath)))
                throw new FileNotFoundException($"File {sourceFilePath} does not exist");

            Process proc = new Process();
            proc.StartInfo.FileName = "kubectl";
            proc.StartInfo.Arguments = $"cp {sourceFilePath} {pod.Namespace()}/{pod.Name()}:{destinationFilePath} -c {container}";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                throw new Exception($"Failed to copy {sourceFilePath} to pod {pod.Name()};\nstdout:\n{stdout}\nstderr:\n{stderr}");
            }
        }

        public static void CopyDirectoryToPod(this Kubernetes client, V1Pod pod, string container, string sourceDirectoryPath, string destinationDirectoyPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!Directory.Exists(sourceDirectoryPath))
                throw new DirectoryNotFoundException($"Directory {sourceDirectoryPath} does not exist");
            client.CopyFileToPod(pod, container, sourceDirectoryPath, destinationDirectoyPath, cancellationToken);
        }

        private static bool IsKubectlInstalled()
        {
            // Run "where" on windows, otherwise run "which" on 'nix systems.
            string program = IsWindows() ? "where" : "which";
            Process proc = Process.Start(program, "kubectl");
            proc.WaitForExit();
            // If exit code is 1, kubectl is not installed.
            return proc.ExitCode == 0;
        }

        private static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
    }
}
