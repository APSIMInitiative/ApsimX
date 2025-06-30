using System.Collections.Generic;
using System.IO;

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
            foreach(string location in VALIDATION_LOCATIONS)
            {
                string[] directories = Directory.GetDirectories(RELATIVE_PATH_PREFIX + location, "*" , SearchOption.AllDirectories);
                foreach (string directory in directories)
                {
                    if (Directory.GetFiles(directory, "*.apsimx").Length > 0)
                    {
                        var fullpath = Path.GetFullPath(directory);
                        // remove unneeded prefixes, these are different in different environments
                        foreach (string validation_location in VALIDATION_LOCATIONS)
                        {
                            string full_path_normalized = fullpath.Replace("\\", "/");
                            string validation_location_normalized = validation_location.Replace("\\", "/");
                            if (full_path_normalized.Contains(validation_location_normalized))
                            {
                                var reduced_path = full_path_normalized.Substring(full_path_normalized.IndexOf(validation_location_normalized));
                                validation_directories.Add("/" + reduced_path); // for linux compatibility
                            }
                        }
                    }
                }
            }
            return validation_directories.ToArray();
        }
    }
}