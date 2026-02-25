using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// A file that reads in a met file from binary format (extra compressed)
    /// </summary>
    [Serializable]
    public static class MetFileUtilities
    {
        /// <summary>
        /// Symbol dictionary for converting from a 4-bit hex to data number character
        /// </summary>
        private static string[] SYMBOLS = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "-", "/", "nan", ""];

        private static Dictionary<char, char> SYMBOLS_DICT = new Dictionary<char, char>() { {'0', '0' }, {'1', '1' }, {'2', '2' }, {'3', '3' }, {'4', '4' }, {'5', '5' }, {'6', '6' }, {'7', '7' }, 
                                                                                          {'8', '8' }, {'9', '9' }, {'.', 'a'}, {'-', 'b'}, {'/', 'c'}, {'n', 'd'}, {' ', 'e'}, {'\t', 'f'} };

        /// <summary>
        /// Supported Met File formats
        /// </summary>
        public enum MetFileFormat { 
            /// <summary>
            /// Extension: .met
            /// Human readable text based met file. Consists of constants, comments, column headers, units and a row for each day of data.
            /// Whitespace may be modified if loading non-standard text metfiles and resaving.
            /// </summary>
            Text = 0,
            /// <summary>
            /// Extension: .bin
            /// A binary representation of a metfile. Contains constants, column headers, units and data for each day.
            /// Comments and whitespace are not supported by this format and are ignored when writing
            /// </summary>
            Binary = 1,
            /// <summary>
            /// Extension: .bin
            /// A compressed binary representation of a metfile. Contains numerial constants, column headers and daily data.
            /// Comments, whitespace, units and text constants are not stored. 
            /// Values are rounded to 1 decimal place.
            /// This format is designed for reducing filesize for network transfer and not for saving or archiving metfiles.
            /// </summary>
            Compressed = 2
        }

        /// <summary>
        /// Minimum spacing for a column of data. Values that are less than this in a column will be padded with whitespace
        /// </summary>
        private static int MIN_COLUMN_WIDTH = 8;

        /// <summary>
        /// Load a
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="data"></param>
        /// <param name="format"></param>
        private static void Save(string filepath, MetData data, MetFileFormat format = MetFileFormat.Text)
        {
            byte[] bytes = null;
            if (format == MetFileFormat.Text)
            {
                string output = WriteMet(data);
                bytes = Encoding.UTF8.GetBytes(output);
            }
            else if (format == MetFileFormat.Binary)
            {
                BinaryData output = WriteBin(data);
                bytes = Convert.FromHexString(output.Hex);
            }
            else if (format == MetFileFormat.Compressed)
            {
                
            }
            File.WriteAllBytes(filepath, bytes);
            return;
        }

        /// <summary>
        /// Opens a binary file and converts it to a valid string representation in a Stream object
        /// </summary>
        private static MetData Load(string filepath)
        {
            byte[] bytes = File.ReadAllBytes(filepath);
            if (filepath.EndsWith(".met"))
            {
                string text = Encoding.UTF8.GetString(bytes);
                return ReadMet(text);
            }
            else if (filepath.EndsWith(".bin"))
            {
                string text = Convert.ToHexString(bytes);
                BinaryData data = new BinaryData(text, 0);
                return ReadBin(data);
            }
            else
            {
                throw new Exception($"File {filepath} has extension {filepath.Substring(filepath.LastIndexOf('.'))} which is not recognised. Must be a .met or .bin file.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="data"></param>
        private static void SaveBin(string filepath, MetData data)
        {
            BinaryData output = WriteBin(data);
            byte[] bytes = Convert.FromHexString(output.Hex);
            File.WriteAllBytes(filepath, bytes);
            return;
        }

        /// <summary>
        /// Opens a binary file and converts it to a valid string representation in a Stream object
        /// </summary>
        private static MetData LoadBin(string filepath)
        {
            byte[] bytes = File.ReadAllBytes(filepath);
            string text = Convert.ToHexString(bytes);
            BinaryData data = new BinaryData(text, 0);
            return ReadBin(data);
        }

        /// <summary>
        /// Opens a binary file and converts it to a valid string representation in a Stream object
        /// </summary>
        private static Stream LoadBinAsStream(string filepath)
        {
            MetData data = LoadBin(filepath);
            string output = WriteMet(data);
            return new MemoryStream(Encoding.UTF8.GetBytes(output));
        }

        /// <summary>
        /// Converts a text metfile string into a MetData object
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static MetData ReadMet(string input)
        {
            MetData metData = new MetData();

            //read all comments
            List<int> commentPositions = new List<int>();
            int length = input.Length;
            for (int i = 0; i < length; i++)
                if (input[i] == '!')
                    commentPositions.Add(i);

            //Start reading file
            string[] lines = input.Split('\n');
            bool headerRemoved = false;
            int step = 0;
            string columnNameLine = "";
            string columnUnitsLine = "";
            List<string> dataLines = new List<string>();
            foreach(string line in lines)
            {
                //clean line
                string trimmed = line.Replace('\t', ' ').Trim();
                if (!headerRemoved && trimmed == "[weather.met.weather]")
                {
                    trimmed = "";
                    headerRemoved = true;
                }

                if (trimmed.Length > 0)
                {
                    if (step == 0) //reading constants and comments
                    {
                        string comment = "";
                        if (trimmed.Contains('!'))
                        {
                            int position = trimmed.IndexOf('!');
                            comment = trimmed.Substring(position);
                            trimmed = trimmed.Substring(0, position);
                        }
                        if (trimmed.Contains('=')) //we have a constant
                        {
                            string[] parts = trimmed.Split('=');
                            metData.Contants.Add(new MetConstant(parts[0].Trim(), parts[1].Trim(), comment));
                        }
                        else if (!string.IsNullOrEmpty(comment))
                        {
                            metData.Contants.Add(new MetConstant(comment));
                        }
                        else // we might have the header row, so move to next step
                        {
                            step = 1; // set this to one so the next line is read for units
                            columnNameLine = trimmed;
                        }
                    }
                    else if (step == 1) //found header last line, reading units
                    {
                        step = 2; // set this to two further lines are read for data
                        columnUnitsLine = trimmed;
                    }
                    else if (step == 2) //reading data
                    {
                        dataLines.Add(trimmed);
                    }
                }
            }
            
            //work out our column names and units
            string[] columnParts = columnNameLine.Split(" ");
            List<string> columnNames = new List<string>();
            foreach(string part in columnParts)
                if (part.Length > 0)
                    columnNames.Add(part);

            columnParts = columnUnitsLine.Split(" ");
            List<string> columnUnits = new List<string>();
            foreach(string part in columnParts)
                if (part.Length > 0)
                    columnUnits.Add(part);

            if (columnNames.Count != columnUnits.Count)
                throw new Exception($"Cannot read met file. {columnNames.Count} column names found ({columnNames}), {columnUnits.Count} units founds. {columnUnits}");
            
            int columnCount = columnNames.Count;
            for(int i = 0; i < columnCount; i++)
            {
                string units = columnUnits[i];
                if (units.StartsWith('('))
                    units = units.Remove(0, 1);
                if (units.EndsWith(')'))
                    units = units.Remove(units.Length-1, 1);
                MetColumn column = new MetColumn(columnNames[i], units);
                if (i == 0)
                    column.IsFirstColumn = true;
                metData.Columns.Add(column);
            }

            //read our data rows
            MetColumn[] columns  = metData.Columns.ToArray();
            foreach(string line in dataLines)
            {
                MetRow row = new MetRow();

                //find the values for each column, there may be variable amounts of whitespace
                string[] parts = line.Split(" ");
                foreach(string part in parts)
                    if(part.Length > 0)
                        row.Inputs.Add(part);

                //row values should match column counts
                if (row.Inputs.Count != columnCount)
                    throw new Exception($"Cannot read met file. Row {line} has {row.Inputs.Count} entries, but there are {columnCount} columns");

                //As we look through the row, we need to determine the date of the row.
                //this may be written either as a year and day columns, or as a date column
                int year = 0;
                int day = 0;
                for(int i = 0; i < columnCount; i++)
                {
                    string stringValue = row.Inputs[i];
                    //update our column width if this value is bigger
                    if (stringValue.Length > columns[i].Width)
                        columns[i].Width = stringValue.Length;

                    string columnName = columns[i].Name.ToLower();
                    if (columnName == "date")
                    {
                        DateTime date;
                        if (DateTime.TryParseExact(stringValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                            row.Date = date;
                        else
                            throw new Exception($"Cannot read met file. Row {line} has date of {stringValue} which must be in yyyy-MM-dd format");
                    }
                    else if (columnName == "year")
                    {
                        if (int.TryParse(stringValue, out year))
                        {
                            if (day > 0)
                                row.Date = new DateTime(year, 1, 1).AddDays(day-1);
                        }
                        else
                        {
                            throw new Exception($"Cannot read met file. Row {line} has year of {stringValue} which is not an integer.");
                        }
                    }
                    else if (columnName == "day")
                    {
                        if (int.TryParse(stringValue, out day))
                        {
                            if (year > 0)
                                row.Date = new DateTime(year, 1, 1).AddDays(day-1);
                        }
                        else
                        {
                            throw new Exception($"Cannot read met file. Row {line} has day of {stringValue} which is not an integer.");
                        }
                    }
                    //Then for all values, we convert to double to store
                    double value;
                    if (double.TryParse(stringValue, out value))
                        row.Values.Add(value);
                    else
                        row.Values.Add(double.NaN); //in cases where we have columns with string, we just set the value to NaN
                }
                metData.Data.Add(row);
            }

            return metData;
        }

        /// <summary>
        /// Converts a MetData into a text metfile string
        /// </summary>
        /// <param name="metData"></param>
        /// <returns></returns>
        private static string WriteMet(MetData metData)
        {
            StringBuilder output = new StringBuilder(2000000);

            //add header
            output.Append("[weather.met.weather]\n");

            //add space between header and constant
            output.Append("\n");

            //add constants
            foreach(MetConstant constant in metData.Contants)
            {
                if (string.IsNullOrEmpty(constant.Name))
                    output.Append($"{constant.Comment}\n");
                else if (string.IsNullOrEmpty(constant.Comment))
                    output.Append($"{constant.Name} = {constant.Value}\n");
                else
                    output.Append($"{constant.Name} = {constant.Value}  {constant.Comment}\n");
            }

            //add space between constants and columns/data
            output.Append("\n");

            //add header row
            foreach(MetColumn column in metData.Columns)
            {
                output.Append(column.Name.PadLeft(column.GetFormattingWidth()));
                output.Append(" ");
            }
            output.Remove(output.Length-1, 1);
            output.Append("\n");

            //add units row
            foreach(MetColumn column in metData.Columns)
            {
                string unit = $"({column.Unit})";
                output.Append(unit.PadLeft(column.GetFormattingWidth()));
                output.Append(" ");
            }
            output.Remove(output.Length-1, 1);
            output.Append("\n");

            //add data rows
            MetColumn[] columns  = metData.Columns.ToArray();
            foreach(MetRow row in metData.Data)
            {
                int index = 0;
                foreach(string data in row.Inputs)
                {
                    output.Append(data.PadLeft(columns[index].GetFormattingWidth()));
                    output.Append(" ");
                    index += 1;
                }
                output.Remove(output.Length-1, 1);
                output.Append("\n");
            }

            return output.ToString();
        }

        /// <summary>
        /// Converts a bytes object to a metfile string
        ///</summary>
        /// <param name="data">The bytes object from the file</param>
        /// <returns>
        /// Metfile as a valid met string
        /// </returns>
        private static MetData ReadBin(BinaryData data)
        {
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
            columns.Add(new StringPair("date", ""));

            int columnsLength = HexToInt(data);
            for (int i = 0; i < columnsLength; i++)
                columns.Add(new StringPair(HexToString(data), ""));

            for (int i = 1; i < columnsLength + 1; i++)
                columns[i].Value = HexToString(data);

            //data
            DateTime date;
            if (!DateTime.TryParseExact(start_date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new Exception($"Cannot read met file. Start date is {start_date} which must be in yyyy-MM-dd format");

            List<List<string>> values = new List<List<string>>();
            int rowsLength = HexToInt(data, 8);

            for (int i = 0; i < rowsLength; i++)
            {
                List<string> row = new List<string>();
                row.Add(date.ToString("yyyy-MM-dd"));

                date = date.AddDays(1);

                for (int j = 0; j < columnsLength; j++)
                    row.Add(DecodeNumber(data));

                values.Add(row);
            }

            string text = GetMetfileString(constants, columns, values);
            return ReadMet(text);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metData"></param>
        /// <returns></returns>
        private static BinaryData WriteBin(MetData metData)
        {
            //make copies of these inputs so we can edit them as needed
            List<MetConstant> constants = metData.Contants.ToList();

            //add start date to constants
            constants.Add(new MetConstant("start_date", metData.Data[0].Date.ToString("yyyy-MM-dd")));

            //remove date from data
            MetData datelessMetData = RemoveColumns(metData, new List<string>() {"date", "year", "day"});

            StringBuilder output = new StringBuilder(2000000);
            //version
            output.Append(StringToHex("met-bin-1"));
            
            //constants
            int constantCount = 0;
            string constantText = "";
            foreach(MetConstant constant in constants)
            {
                if (!string.IsNullOrEmpty(constant.Name) && !string.IsNullOrEmpty(constant.Value))
                {
                    constantCount += 1;
                    constantText += StringToHex(constant.Name) + StringToHex(constant.Value);
                }
            }
            output.Append(IntToHex(constantCount));
            output.Append(constantText);

            //headers
            //titles
            MetColumn[] columns = datelessMetData.Columns.ToArray();

            output.Append(IntToHex(columns.Length));
            foreach(MetColumn column in columns)
                output.Append(StringToHex(column.Name));
            foreach(MetColumn column in columns)
                output.Append(StringToHex(column.Unit));

            //data
            output.Append(IntToHex(datelessMetData.Data.Count, 8));
            foreach(MetRow row in datelessMetData.Data)
                foreach(double value in row.Values)
                    output.Append(EncodeNumber(value.ToString()));

            //if output is an odd length
            if (output.Length % 2 == 1) 
                output.Append("0");

            BinaryData data = new BinaryData(output.ToString(), 0);
            return data;
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

        private static string IntToHex(int value, int size = 2)
        {
            string output = value.ToString("x");
            while(output.Length < size)
                output = "0" + output;
            return output;
        }

        private static string StringToHex(string data)
        {
            string length = IntToHex(data.Length);

            string text = "";
            foreach(char c in data)
                text += IntToHex(c);

            return length + text;
        }

        private static string EncodeNumber(string data)
        {
            string output = "";
            if (data.ToLower() == "nan")
            {
                output = IntToHex(1, 1);
                output += SYMBOLS_DICT['n'];
            }
            else
            {
                //numbers ending in .0 are just the number before the decimal
                string trimmed = data;
                if (trimmed.EndsWith(".0"))
                    trimmed = data.Replace(".0", "");
                if (trimmed.StartsWith("-0."))
                    trimmed = trimmed.Replace("-0.", "-.");
                output = IntToHex(trimmed.Length, 1);
                foreach (char c in trimmed)
                    output += SYMBOLS_DICT[c];
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
                line += $"({column.Value}) ";
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
        /// Removes columns that match the provided names (case insensitive) and the data in those columns from a MetData object.
        /// </summary>
        /// <param name="metData"></param>
        /// <param name="names"></param>
        static private MetData RemoveColumns(MetData metData, List<string> names)
        {
            if (names.Count == 0)
                return metData;

            List<string> namesLower = new List<string>();
            foreach(string name in names)
                namesLower.Add(name.ToLower());

            MetData output = new MetData();
            output.Contants = metData.Contants;

            List<int> removedColumnIndexs = new List<int>();
            for (int i = 0; i < metData.Columns.Count; i++)
            {
                MetColumn column = metData.Columns[i];
                if (names.Contains(column.Name.ToLower()))
                    removedColumnIndexs.Add(i);
                else
                    output.Columns.Add(column);
            }
            
            foreach(MetRow row in metData.Data)
            {
                MetRow newRow = new MetRow();
                newRow.Date = row.Date;
                for (int i = 0; i < row.Values.Count; i++)
                {
                    if (!removedColumnIndexs.Contains(i))
                    {
                        newRow.Inputs.Add(row.Inputs[i]);
                        newRow.Values.Add(row.Values[i]);
                    }
                }
                output.Data.Add(newRow);
            }

            return output;
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

        /// <summary>
        /// A string pair to store constants and column data
        /// </summary>
        private class MetConstant
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
            /// Comments on line
            /// </summary>
            public string Comment { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetConstant()
            {
                Name = "";
                Value = "";
                Comment = "";
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name">The name of the pair</param>
            /// <param name="value">The value of the pair</param>
            public MetConstant(string name, string value)
            {
                Name = name;
                Value = value;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="comment"></param>
            public MetConstant(string comment)
            {
                Comment = comment;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name">The name of the pair</param>
            /// <param name="value">The value of the pair</param>
            /// <param name="comment"></param>
            public MetConstant(string name, string value, string comment)
            {
                Name = name;
                Value = value;
                Comment = comment;
            }
        }

        /// <summary>
        /// A string pair to store constants and column data
        /// </summary>
        private class MetColumn
        {
            /// <summary>
            /// 
            /// </summary>
            public bool IsFirstColumn { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public bool IsDate { get; set; }

            /// <summary>
            /// Name of the pair
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Value of the pair
            /// </summary>
            public string Unit { get; set; }

            /// <summary>
            /// Comments on line
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetColumn()
            {
                Name = "";
                Unit = "";
                Width = 0;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name">The name of the pair</param>
            /// <param name="unit">The value of the pair</param>
            public MetColumn(string name, string unit)
            {
                Name = name;
                Unit = unit;
                Width = Math.Max(name.Length, unit.Length + 2);
                
                string nameLowered = name.ToLower();
                if (nameLowered == "day" || nameLowered == "year" || nameLowered == "date")
                    IsDate = true;
                else
                    IsDate = false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public int GetFormattingWidth()
            {
                if (IsFirstColumn)
                    return Width;
                else
                    return Math.Max(MIN_COLUMN_WIDTH, Width);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class MetRow
        {
            /// <summary>
            ///
            /// </summary>
            public List<string> Inputs { get; set; }

            /// <summary>
            ///
            /// </summary>
            public DateTime Date { get; set; }

            /// <summary>
            ///
            /// </summary>
            public List<double> Values { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetRow()
            {
                Inputs = new List<string>();
                Date = DateTime.MinValue;
                Values = new List<double>();
            }
        }

        /// <summary>
        /// A data structure for holding everything about a metfile
        /// </summary>
        private class MetData
        {
            /// <summary>
            /// 
            /// </summary>
            public List<MetConstant> Contants;

            /// <summary>
            /// 
            /// </summary>
            public List<MetColumn> Columns;

            /// <summary>
            /// 
            /// </summary>
            public List<MetRow> Data;

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetData()
            {
                Contants = new List<MetConstant>();
                Columns = new List<MetColumn>();
                Data = new List<MetRow>();
            }
        }

        /// <summary>
        /// A data structure for holding everything about a metfile
        /// </summary>
        public class MetFile
        {
            private MetData data;

            /// <summary>
            /// Empty Constructor
            /// </summary>
            public MetFile()
            {
                data = new MetData();
            }

            /// <summary>
            /// Loads a metfile from the given filepath
            /// </summary>
            public MetFile(string filepath)
            {
                Load(filepath);
            }

            /// <summary>
            /// Loads a metfile from the given filepath
            /// </summary>
            public void Load(string filepath)
            {
                data = MetFileUtilities.Load(filepath);
            }

            /// <summary>
            /// Save a metfile to the given filepath.
            /// Optional format flag, defaults to .met text file.
            /// </summary>
            public void Save(string filepath, MetFileFormat format = MetFileFormat.Text)
            {
                MetFileUtilities.Save(filepath, data, format);
            }

            /// <summary>
            /// Returns the value of the given constant. Will return null if constant was not found.
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public string GetConstant(string name)
            {
                foreach(MetConstant constant in data.Contants)
                    if (constant.Name == name)
                        return constant.Value;
                return null;
            }

            /// <summary>
            /// Returns the values for the given date
            /// </summary>
            public double[] GetDay(DateTime date)
            {
                foreach(MetRow row in data.Data)
                    if (date == row.Date)
                        return row.Values.ToArray();
                throw new Exception($"Date {date.ToString("yyyy-MM-dd")} not found in MetFile");
            }

            /// <summary>Comments within met file</summary>
            public string[] Comments
            {
                get
                {
                    List<string> comments = new List<string>();
                    foreach(MetConstant constant in data.Contants)
                        if (string.IsNullOrEmpty(constant.Comment))
                            comments.Append(constant.Comment);
                    return comments.ToArray();
                }
            }

            /// <summary>List of constants</summary>
            public string[] Contants
            {
                get
                {
                    List<string> constants = new List<string>();
                    foreach(MetConstant constant in data.Contants)
                        if (string.IsNullOrEmpty(constant.Name))
                            constants.Append(constant.Name);
                    return constants.ToArray();
                }
            }

            /// <summary>Column header names</summary>
            public string[] Columns
            {
                get
                {
                    List<string> columns = new List<string>();
                    foreach(MetColumn column in data.Columns)
                        columns.Append(column.Name);
                    return columns.ToArray();
                }
            }

            /// <summary>Units for each column</summary>
            public string[] Units
            {
                get
                {
                    List<string> units = new List<string>();
                    foreach(MetColumn column in data.Columns)
                        units.Append(column.Unit);
                    return units.ToArray();
                }
            }
        }
    }
}



