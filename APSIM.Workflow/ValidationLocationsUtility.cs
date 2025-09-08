using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APSIM.Workflow
{
    /// <summary>
    /// A utility class for working with validation locations.
    /// </summary>
    public static class ValidationLocationUtility
    {
        private static readonly string RELATIVE_PATH_PREFIX = "./";

        /// <summary>The locations where validation directories are expected to be found.</summary>
        public static readonly string[] VALIDATION_LOCATIONS =
        [
            "Prototypes/",
            "Tests/Simulation/",
            "Tests/UnderReview/",
            "Tests/Validation/",
            "Examples/"
        ];

        /// <summary>
        /// Get the names of all directories in the validation locations that contain an .apsimx file.
        /// </summary>
        /// <returns>A string array</returns>
        public static string[] GetDirectoryPaths()
        {
            List<string> validation_directories = [];
            foreach (string location in VALIDATION_LOCATIONS)
            {
                var directory = Path.GetFullPath(RELATIVE_PATH_PREFIX + location);
                if (Directory.GetFiles(directory, "*.apsimx", SearchOption.AllDirectories).Any())
                {
                    var apsimxFiles = Directory.GetFiles(directory, "*.apsimx", SearchOption.AllDirectories);
                    foreach (var apsimxFile in apsimxFiles)
                    {
                        if (!PayloadUtilities.EXCLUDED_SIMS_FILEPATHS.Contains("/" + apsimxFile.NormalizePath()))
                        {
                            var apsimxNormalizedFilePath = apsimxFile.NormalizePath();
                            if (apsimxNormalizedFilePath.Contains(location))
                            {
                                var locationFolderIndex = apsimxNormalizedFilePath.IndexOf(location);
                                validation_directories.Add("/" + apsimxNormalizedFilePath.Substring(locationFolderIndex, locationFolderIndex + location.Length)); // for linux compatibility
                            }
                            else
                            {
                                throw new Exception("The location: " + location + " was not found in the apsimx file path: " + apsimxNormalizedFilePath);
                            }
                        }
                    }
                }
            }
            return validation_directories.ToArray();
        }
        

        /// <summary>
        /// Get the number of simulations/validation locations available.
        /// </summary>
        public static int GetSimulationCount()
        {
            return GetDirectoryPaths().Length;
        }
        
        /// <summary>Normalize a file path to use forward slashes instead of backslashes.</summary>
        public static string NormalizePath(this string path)
        {
            return path.Replace("\\", "/");
        }
    }
}