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
            BinaryData data = new BinaryData(Convert.ToHexString(bytes), 0);

            //read version
            //not used when converting to string. Will be useful if a future binary format is developed
            string version = HexToString(data);

            //read constants
            int constantsLength = HexToInt(data);

            List<StringPair> constants = new List<StringPair>();
            string start_date = "";

            for (int i = 0; i < constantsLength; i++)
            {
                string name = HexToString(data);
                string value = HexToString(data);

                //start date isn't an actual constant, but is stored as a constant to save space
                if (name == "start_date")
                    start_date = value;
                else
                    constants.Add(new StringPair(name, value));
            }

            //headers
            List<StringPair> columns = new List<StringPair>();
            columns.Add(new StringPair("date", "()"));

            int columnsLength = HexToInt(data);
            for (int i = 0; i < columnsLength; i++)
                columns.Add(new StringPair(HexToString(data), ""));

            for (int i = 1; i < columnsLength + 1; i++)
                columns[i].Value = HexToString(data);

            //data
            DateTime date = DateUtilities.GetDate(start_date);

            List<List<string>> values = new List<List<string>>();
            int rowsLength = HexToInt(data, 8);

            for (int i = 0; i < rowsLength; i++)
            {
                List<string> row = new List<string>();
                row.Add($"{date.Year}-{date.Month}-{date.Day}");

                date = date.AddDays(1);

                for (int j = 0; j < columnsLength; j++)
                    row.Add(DecodeNumber(data));

                values.Add(row);
            }

            return GetMetfileString(constants, columns, values);
        }

        /// <summary>
        /// Convert a hex string to an int
        /// </summary>
        /// <param name="data">the hex string</param>
        /// <param name="size">how many digits it should be stored as. 1 hex = 4 bits</param>
        /// <returns>
        /// value: an int value for the given hex
        /// position: the end position after reading
        /// </returns>
        private static int HexToInt(BinaryData data, int size = 2)
        {
            string substring = data.Hex.Substring(data.Position, size);
            int value = Convert.ToInt32(substring, 16);
            data.Position += size;
            return value;
        }

        /// <summary>
        /// Convert a hex string to a character string
        /// </summary>
        /// <param name="data">the hex string</param>
        /// <returns>
        /// value: a string value for the given bits
        /// position: the end position after reading
        /// </returns>
        private static string HexToString(BinaryData data)
        {
            int length = HexToInt(data);
            int count = length * 2;

            string substring = data.Hex.Substring(data.Position, count);
            byte[] output = Convert.FromHexString(substring);

            data.Position += count;

            return Encoding.UTF8.GetString(output);
        }

        /// <summary>
        /// Convert a hex string to a metfile number
        /// </summary>
        /// <param name="data">the hex string</param>
        /// <returns>
        /// value: a number value for the given bits
        /// position: the end position after reading
        /// </returns>
        private static string DecodeNumber(BinaryData data)
        {
            int length = HexToInt(data, 1);

            string output = "";
            for (int i = 0; i < length; i++)
            {
                int index = HexToInt(data, 1);
                output += SYMBOLS[index];
            }

            return output;
        }

        /// <summary>
        /// Takes the different parts of the met file after being converted to strings and builds a normal
        /// text metfile out of them with the correct formatting for it to be readable.
        /// </summary>
        /// <param name="constants">A list of StringPairs, one for each constant</param>
        /// <param name="columns">A list of StringPairs, where the name is the column name, and the value is the unit string</param>
        /// <param name="data">A list of list of strings of all the row x column data</param>
        /// <returns></returns>
        private static string GetMetfileString(List<StringPair> constants, List<StringPair> columns, List<List<string>> data)
        {
            StringBuilder builder = new StringBuilder(2000000);
            string line = "";

            builder.AppendLine("[weather.met.weather]");

            foreach (StringPair constant in constants)
                builder.AppendLine($"{constant.Name} = {constant.Value}");

            line = "";
            foreach (StringPair column in columns)
                line += $"{column.Name} ";
            builder.AppendLine(line);

            line = "";
            foreach (StringPair column in columns)
                line += $"{column.Value} ";
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

        /// <summary>
        /// BinaryData stores the hex string that is read when reading the file, and the position through the string
        /// that has been read. It is up to the reader functions to keep the position correct.
        /// </summary>
        private class BinaryData
        {
            /// <summary>
            /// A string of hex values representing the file
            /// </summary>
            public string Hex { get; set; }

            /// <summary>
            /// The current read position through the file.
            /// It is up to the reader functions to keep the position correct.
            /// </summary>
            public int Position { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public BinaryData()
            {
                Hex = "";
                Position = 0;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="hex">Hex string of the data</param>
            /// <param name="position">Position to start at</param>
            public BinaryData(string hex, int position)
            {
                Hex = hex;
                Position = position;
            }
        }

        /// <summary>
        /// A string pair to store constants and column data
        /// </summary>
        private class StringPair
        {
            /// <summary>
            /// Name of the pair
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Value of the pair
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public StringPair()
            {
                Name = "";
                Value = "";
            }

            /// <summary>
            /// Constructor for setting a pair.
            /// </summary>
            /// <param name="name">The name of the pair</param>
            /// <param name="value">The value of the pair</param>
            public StringPair(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}



