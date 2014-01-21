using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Importer
{
    /// <summary>
    /// This is the console program used for importing APSIM simulations into the new APSIM(X) system.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            APSIMImporter importer = new APSIMImporter();
            string apsimPath = "";

            if ((args.Length > 0) && (args[0].Length > 0))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    String filename = args[i];

                    if (filename[0] == '-')     // if this is a parameter
                    {
                        if (filename[1] == 'a')     // apsim path macro replacement
                            apsimPath = filename.Substring(2, filename.Length - 2);
                    }
                    else
                    {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(filename);

                        importer.ApsimPath = apsimPath;
                        //detect whether its a directory or file
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            importer.ProcessDir(filename);
                        else
                            importer.ProcessFile(filename);
                    }
                }
            }
            else
            {
                Console.WriteLine("Useage: Importer [options] file.apsim [file2.apsim ....]\n    Or: Importer [options] directoryname [dir2 ...]\n");
                Console.Write("Where options are\n\t-a to set the base apsim path. e.g. -aC:\\apsim \n");
            }
        }
    }
}
