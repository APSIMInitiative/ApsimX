
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;

namespace Models.Core.ConfigFile
{

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

        /// <summary>
        /// Replaces any command placeholders.
        /// </summary>
        /// <param name="configFileCommand">A command from a config file. These are used with the --apply switch.</param>
        /// <param name="row"> A DataRow created from a BatchFile's csv file.</param>
        /// <param name="rowIndex">the index of <paramref name="row"/> in row.Table.</param>
        /// <returns></returns>
        public static string GetCommandReplacements(string configFileCommand, DataRow row, int rowIndex)
        {
            char targetChar = '$';
            List<char> commandCharList = configFileCommand.ToCharArray().ToList();
            List<int> dollarSignIndices = new();
            int index = commandCharList.IndexOf(targetChar);
            while (index != -1)
            {
                dollarSignIndices.Add(index);
                index = commandCharList.IndexOf(targetChar, index + 1);
            }

            if (dollarSignIndices.Count > 1)
                throw new Exception($"The command '{configFileCommand}' contains more than one placeholder. Only one placeholder per command is permitted.");
            if (dollarSignIndices.Count == 0)
                return configFileCommand;

            // Get indices of different ends to a placeholder.
            string columnName = null;
            int indexOfLineEndingChar = configFileCommand.IndexOf(Environment.NewLine);
            int lastPeriodIndex = configFileCommand.IndexOf('.', dollarSignIndices.First(), configFileCommand.Length - (dollarSignIndices.First() + 1));
            int lastBracketIndex = 0;
            while (lastBracketIndex != configFileCommand.Length-1 && lastBracketIndex != -1)
            {
                lastBracketIndex = configFileCommand.IndexOf(']', lastBracketIndex + 1);
            }

            // Get the column name for a placeholder.
            int placeholderStartIndex = dollarSignIndices.First() + 1;
            if (indexOfLineEndingChar != -1)
                columnName = configFileCommand.Substring(placeholderStartIndex, indexOfLineEndingChar);
            else if (lastBracketIndex != -1)
                columnName = configFileCommand[placeholderStartIndex..lastBracketIndex];
            else if (lastPeriodIndex != -1)
                columnName = configFileCommand[placeholderStartIndex..lastPeriodIndex];
            else columnName = configFileCommand[placeholderStartIndex..configFileCommand.Length];
            
            // Get the replacement value for the placeholder.
            string _row_value = null;
            if (!string.IsNullOrWhiteSpace(columnName))
                _row_value = row.Table.Rows[rowIndex][columnName].ToString();

            // Replace the placeholder with the replacement value.
            string actualCommand = configFileCommand.Replace("$" + columnName, _row_value);
            return actualCommand;
        }


    }
}