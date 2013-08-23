using System;
using System.Diagnostics;
using System.IO;

namespace Utility
{
    public class Process
    {
        public enum CompilationMode
        {
            /// <summary>
            /// Unknown
            /// </summary>
            Invalid,
            /// <summary>
            /// Native Win32 code
            /// </summary>
            Native,
            /// <summary>
            /// Common Language Runtime
            /// </summary>
            CLR,
            /// <summary>
            /// Mixed mode
            /// </summary>
            Mixed
        };

        //=========================================================================
        /// <summary>
        /// Determine if the file refered to is a native win32 or a CLR assembly.
        /// Mixed mode assemblies are CLR.
        /// Visual C++ Developer Center. http://msdn2.microsoft.com/en-us/library/c91d4yzb(VS.80).aspx
        /// </summary>
        /// <param name="filename">File name of the Assembly or native dll to probe.</param>
        /// <returns>Compilation mode.</returns>
        //=========================================================================
        static public CompilationMode isManaged(string filename)
        {
            try
            {
                byte[] data = new byte[4096];
                FileInfo file = new FileInfo(filename);
                Stream fin = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                /*Int32 iRead =*/
                fin.Read(data, 0, 4096);
                fin.Close();

                // If we are running on Linux, the executable/so will start with the string 0x7f + 'ELF'
                // If the 5 byte is 1, it's a 32-bit image (2 indicates 64-bit)
                // If the 16th byte is 3, it's a shared object; 2 means it's an executable
                // If it's a Mono/.Net assembly, we should the the "Windows" header

                // If we're on Linux, see if it's a hash bang script. Should really
                // check executable flag via Mono.Unix.Native.Syscall.stat() too
                if (System.IO.Path.VolumeSeparatorChar == '/' &&
                    Convert.ToChar(data[0]) == '#' &&
                    Convert.ToChar(data[1]) == '!')
                    return CompilationMode.Native;
                // For now, if we're on Linux just see if it has an "ELF" header
                if (System.IO.Path.VolumeSeparatorChar == '/' && data[0] == 0x7f && data[1] == 'E' && data[2] == 'L' && data[3] == 'F')
                    return CompilationMode.Native;

                // Verify this is a executable/dll
                if (UInt16FromBytes(data, 0) != 0x5a4d)
                    return CompilationMode.Invalid;

                uint headerOffset = UInt32FromBytes(data, 0x3c);  // This will get the address for the WinNT header

                //at the file offset specified at offset 0x3c, is a 4-byte
                //signature that identifies the file as a PE format image file. This signature is “PE\0\0”
                if (UInt32FromBytes(data, headerOffset) != 0x00004550)
                    return CompilationMode.Invalid;

                //uint machineType = UInt16FromBytes(data, headerOffset + 4); //type of machine
                uint optionalHdrBase = headerOffset + 24;
                //uint exportTableAddr = UInt32FromBytes(data, optionalHdrBase + 96);     //.edata
                uint exportTableSize = UInt32FromBytes(data, optionalHdrBase + 96 + 4); //.edata size

                Int32 iLightningAddr = (int)headerOffset + 24 + 208;    //CLR runtime header addr & size
                Int32 iSum = 0;
                Int32 iTop = iLightningAddr + 8;

                for (int i = iLightningAddr; i < iTop; ++i)
                    iSum |= data[i];

                if (iSum == 0)
                    return CompilationMode.Native;
                else
                {
                    if (exportTableSize > 0)
                        return CompilationMode.Mixed;
                    else
                        return CompilationMode.CLR;
                }
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

        /// <summary>
        /// Run the specified executable with the specified arguments and working directory.
        /// Returns the Process object to caller.
        /// </summary>
        public static System.Diagnostics.Process RunProcess(string Executable, string Arguments, string WorkingDirectory)
        {
            if (!File.Exists(Executable))
                throw new System.Exception("Cannot execute file: " + Executable + ". File not found.");
            System.Diagnostics.Process PlugInProcess = new System.Diagnostics.Process();
            PlugInProcess.StartInfo.FileName = Executable;
            PlugInProcess.StartInfo.Arguments = Arguments;
            // Determine whether or not the file is an executable; execute from the shell if it's not
            PlugInProcess.StartInfo.UseShellExecute = isManaged(Executable) == CompilationMode.Invalid;
            PlugInProcess.StartInfo.CreateNoWindow = true;
            if (!PlugInProcess.StartInfo.UseShellExecute)
            {
                PlugInProcess.StartInfo.RedirectStandardOutput = true;
                PlugInProcess.StartInfo.RedirectStandardError = true;
            }
            PlugInProcess.StartInfo.WorkingDirectory = WorkingDirectory;
            PlugInProcess.Start();
            return PlugInProcess;
        }

        /// <summary>
        /// Checks whether the specified process has finished ok. Will wait for process to complete. 
        /// Will throw Exception with error message from process if Process exits with a non-zero
        /// exit code.
        /// </summary>
        public static string CheckProcessExitedProperly(System.Diagnostics.Process PlugInProcess)
        {
            if (!PlugInProcess.StartInfo.UseShellExecute)
            {
                string msg = "";
                if (PlugInProcess.StartInfo.RedirectStandardOutput)
                    msg = PlugInProcess.StandardOutput.ReadToEnd();
                PlugInProcess.WaitForExit();
                if (PlugInProcess.ExitCode != 0)
                {
                    if (PlugInProcess.StartInfo.RedirectStandardError)
                        msg += PlugInProcess.StandardError.ReadToEnd();
                    if (msg != "")
                        throw new System.Exception("Error from " + System.IO.Path.GetFileName(PlugInProcess.StartInfo.FileName) + ": "
                                                                 + msg);
                }
                return msg;
            }
            else
                return "";
        }


        static private UInt32 UInt32FromBytes(byte[] p, uint offset)
        {
            return (UInt32)(p[offset + 3] << 24 | p[offset + 2] << 16 | p[offset + 1] << 8 | p[offset]);
        }
        static private UInt16 UInt16FromBytes(byte[] p, uint offset)
        {
            return (UInt16)(p[offset + 1] << 8 | p[offset]);
        }

    }
}
