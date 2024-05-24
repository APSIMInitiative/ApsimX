
using System.Data;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.Core.ConfigFile{

    /// <summary>
    /// A file used with a config file during an --apply run with Models.
    /// Primarily used to 
    /// </summary>
    public class BatchFile
    {
        /// <summary>
        /// A csv file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// A DataTable for holding csv data.
        /// </summary>
        public DataTable DataTable { get; set; }

        /// <summary>
        /// Create a BatchFile.
        /// </summary>
        /// <param name="fileName">the file name of a csv file.</param>
        public BatchFile(string fileName)
        {
            FileName = fileName;
            using var streamReader = new StreamReader(fileName);
            if (File.Exists(fileName) && Path.GetExtension(fileName).Equals(".csv"))
                DataTable = DataTableUtilities.FromCSV(fileName, streamReader.ReadToEnd());
        }
    }
}