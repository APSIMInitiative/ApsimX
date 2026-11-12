using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;

namespace APSIM.Workflow
{
    /// <summary>
    /// A utility class for working with validation locations.
    /// </summary>
    public static class ValidationLocationUtility
    {
        private static readonly string RELATIVE_PATH_PREFIX = "./";

        /// <summary> List of R simulation files paths</summary>
        private static string[] R_SIMS_FILEPATHS = {
            "/Prototypes/CroptimizR/template.apsimx",
            "/Examples/Optimisation/CroptimizRExample.apsimx",
            "/Examples/Tutorials/Sensitivity_SobolMethod.apsimx",
            "/Examples/Tutorials/Sensitivity_MorrisMethod.apsimx",
            "/Examples/Tutorials/Sensitivity_FactorialANOVA.apsimx",
            "/Examples/Sensitivity/Sobol.apsimx",
            "/Examples/Sensitivity/Morris.apsimx"
        };

        /// <summary> List of excluded simulation file paths</summary>
        private static string[] EXCLUDED_SIMS_FILEPATHS = {
            "/Prototypes/CroptimizR/template.apsimx",
            "/Examples/Optimisation/CroptimizRExample.apsimx",
            "/Examples/CsvWeather.apsimx", //has no output
            "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/Edited_v2_BivariateNormal.apsimx", //has no output
            "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/Edited_v5_BivariateNormal.apsimx" //has no output
        };

        /// <summary>The locations where validation directories are expected to be found.</summary>
        private static readonly string[] VALIDATION_LOCATIONS =
        [
            "Prototypes/",
            "Tests/Simulation/",
            "Tests/UnderReview/",
            "Tests/Validation/",
            "Examples/"
        ];

        /// <summary>
        /// Get the paths of all apsimx files in the validation locations that contain an .apsimx file.
        /// </summary>
        /// <returns>A string array</returns>
        public static string[] GetValidationFilePaths()
        {
            List<string> validation_directories = [];
            foreach (string location in VALIDATION_LOCATIONS)
            {
                string directory = Path.GetFullPath(RELATIVE_PATH_PREFIX + location);
                //if directory didn't exist, try finding it using the apsim directory
                if (!Directory.Exists(directory))
                    directory = PathUtilities.GetAbsolutePath(RELATIVE_PATH_PREFIX + location, PathUtilities.GetApsimXDirectory());
                
                string[] files = Directory.GetFiles(directory, "*.apsimx", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    //clean path
                    string apsimxFile = file.Replace("\\", "/");

                    //check if it matches any of the excluded paths
                    bool containsExcludedPath = false;
                    foreach(string excludedPath in EXCLUDED_SIMS_FILEPATHS)
                        if (apsimxFile.Contains(excludedPath))
                            containsExcludedPath = true;
                    
                    if (!containsExcludedPath)
                    {
                        if (apsimxFile.Contains(location))
                        {
                            int locationFolderIndex = apsimxFile.IndexOf(location);
                            validation_directories.Add("/" + apsimxFile.Substring(locationFolderIndex)); // for linux compatibility
                        }
                        else
                        {
                            throw new Exception("The location: " + location + " was not found in the apsimx file path: " + apsimxFile);
                        }
                    }
                }
            }
            return validation_directories.ToArray();
        }
    }
}