using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// A file that reads in a met file from binary format (extra compressed)
    /// </summary>
    [Serializable]
    public static class MetBinaryReader
    {
        /// <summary>
        /// Symbol dictionary for converting from a 4-bit hex to data number character
        /// </summary>
        private static string[] SYMBOLS = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "-", "/", "nan"];

        /// <summary>
        /// Opens a binary file and converts it to a valid string representation in a Stream object
        /// </summary>
        public static Stream Load(string filepath)
        {
            byte[] fileBytes = File.ReadAllBytes(filepath);
            string output = Read(fileBytes);
            return new MemoryStream(Encoding.UTF8.GetBytes(output));
        }

        /// <summary>
        /// Converts a bytes object to a metfile string
        ///</summary>
        /// <param name="bytes">The bytes object from the file</param>
        /// <returns>
        /// Metfile as a valid met string
        /// </returns>
        public static string Read(byte[] bytes)
        {
            string hex = Convert.ToHexString(bytes);

            int position = 0;

            //read version
            (string, int) resultString = HexToString(hex, position);
            string version = resultString.Item1; //not used when converting to string. Will be useful if a future binary format is developed
            position = resultString.Item2;

            //read constants
            (int, int) resultInt = HexToInt(hex, position);
            int constantsLength = resultInt.Item1;
            position = resultInt.Item2;

            List<(string, string)> constants = new List<(string, string)>();
            string start_date = "";

            for (int i = 0; i < constantsLength; i++)
            {
                //name
                (string, string) constant;
                resultString = HexToString(hex, position);
                constant.Item1 = resultString.Item1;
                position = resultString.Item2;

                //value
                resultString = HexToString(hex, position);
                constant.Item2 = resultString.Item1;
                position = resultString.Item2;

                if (constant.Item1 == "start_date")
                    start_date = constant.Item2;
                else
                    constants.Add(constant);
            }

            //headers
            List<(string, string)> columns = new List<(string, string)>();
            columns.Add(("date", "()"));

            resultInt = HexToInt(hex, position);
            int columnsLength = resultInt.Item1;
            position = resultInt.Item2;

            for (int i = 0; i < columnsLength; i++)
            {
                resultString = HexToString(hex, position);
                columns.Add((resultString.Item1, ""));
                position = resultString.Item2;
            }

            for (int i = 1; i < columnsLength + 1; i++)
            {
                resultString = HexToString(hex, position);
                columns[i] = (columns[i].Item1, resultString.Item1);
                position = resultString.Item2;
            }

            //data
            DateTime date = DateUtilities.GetDate(start_date);

            List<List<string>> data = new List<List<string>>();
            resultInt = HexToInt(hex, position, 8);
            int rowsLength = resultInt.Item1;
            position = resultInt.Item2;

            for (int i = 0; i < rowsLength; i++)
            {
                List<string> row = new List<string>();
                row.Add($"{date.Year}-{date.Month}-{date.Day}");

                date = date.AddDays(1);

                for (int j = 0; j < columnsLength; j++)
                {
                    resultString = DecodeNumber(hex, position);
                    row.Add(resultString.Item1);
                    position = resultString.Item2;
                }
                data.Add(row);
            }

            return getMetfileString(constants, columns, data);
        }

        /// <summary>
        /// Convert a hex string to an int
        /// </summary>
        /// <param name="data">the hex string</param>
        /// <param name="pos">the starting position to read from</param>
        /// <param name="size">how many digits it should be stored as. 1 hex = 4 bits</param>
        /// <returns>
        /// value: an int value for the given hex
        /// position: the end position after reading
        /// </returns>
        private static (int, int) HexToInt(string data, int pos = 0, int size = 2)
        {
            string substring = data.Substring(pos, size);
            int value = Convert.ToInt32(substring, 16);
            return (value, pos + size);
        }

        /// <summary>
        /// Convert a hex string to a character string
        /// </summary>
        /// <param name="data">the hex string</param>
        /// <param name="pos">the starting position to read from</param>
        /// <returns>
        /// value: a string value for the given bits
        /// position: the end position after reading
        /// </returns>
        private static (string, int) HexToString(string data, int pos = 0)
        {
            var result = HexToInt(data, pos);
            int length = result.Item1;
            int position = result.Item2;
            int count = length * 2;

            string substring = data.Substring(position, count);
            byte[] output = Convert.FromHexString(substring);

            return (Encoding.UTF8.GetString(output), position + count);
        }

        /// <summary>
        /// Convert a hex string to a metfile number
        /// </summary>
        /// <param name="data">the hex string</param>
        /// <param name="pos">the starting position to read from</param>
        /// <returns>
        /// value: a number value for the given bits
        /// position: the end position after reading
        /// </returns>
        private static (string, int) DecodeNumber(string data, int pos = 0) {

            int position = pos;

            var result = HexToInt(data, position, 1);
            int length = result.Item1;
            position = result.Item2;

            string output = "";
            for (int i = 0; i < length; i++)
            {
                result = HexToInt(data, position, 1);
                int index = result.Item1;
                position = result.Item2;

                output += SYMBOLS[index];
            }

            return (output, position);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="constants"></param>
        /// <param name="columns"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string getMetfileString(List<(string, string)> constants, List<(string, string)> columns, List<List<string>> data)
        {
            StringBuilder builder = new StringBuilder(2000000);
            string line = "";

            builder.AppendLine("[weather.met.weather]");

            foreach ((string, string) constant in constants)
                builder.AppendLine($"{constant.Item1} = {constant.Item2}");

            line = "";
            foreach ((string, string) column in columns)
                line += $"{column.Item1} ";
            builder.AppendLine(line);

            line = "";
            foreach ((string, string) column in columns)
                line += $"{column.Item2} ";
            builder.AppendLine(line);

            foreach (List<string> row in data)
            {
                line = "";
                foreach (string value in row)
                {
                    line += $"{value} ";
                }
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}



