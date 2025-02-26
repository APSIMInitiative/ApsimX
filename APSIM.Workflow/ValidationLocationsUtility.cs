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
                foreach(string directory in directories)
                {
                    if (Directory.GetFiles(directory, "*.apsimx").Length > 0)
                        validation_directories.Add(Path.GetFullPath(directory));
                }
            }
            return validation_directories.ToArray();
        }
    }
}