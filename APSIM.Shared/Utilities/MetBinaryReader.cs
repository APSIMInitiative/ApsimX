using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// 
        /// </summary>
        private static int MIN_COLUMN_WIDTH = 8;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="metData"></param>
        public static void Save(string filepath, MetData metData)
        {
            
        }

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
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static MetData ReadMet(string input)
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
        /// 
        /// </summary>
        /// <param name="metData"></param>
        /// <returns></returns>
        public static string WriteMet(MetData metData)
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

        private static BinaryData IntToHex()
        {
            
        }

        private static BinaryData StringToHex()
        {
            
        }

        private static BinaryData EncodeNumber()
        {
            
        }

        /*
        @staticmethod
    def int_to_hex(data:int, size:int = 2) -> str:
        '''
        Convert an int to hex string
        Arguments:
            data: the int value
            size: how many digits it should be stored as. 1 hex = 4 bits
        Returns:
            hex string representing the bits of the given value
        '''
        output = format(data, '0x')
        while(len(output) < size):
            output = "0" + output

        return output

    @staticmethod
    def str_to_hex(data:str) -> str:
        '''
        Convert a character string to hex string
        Arguments:
            data: the string value
        Returns:
            hex string representing the bits of the given value
        '''
        length = BinaryMetfile.int_to_hex(len(data))

        text = ""
        for char in data:
            text += BinaryMetfile.int_to_hex(ord(char))

        return length + text

        @staticmethod
    def encode_num(data) -> str:
        '''
        Convert a metfile number to hex string.
        Number is assumed to potentially be negatives, have decimal places
        or have other notation so is stored as list character symbols, not
        as a number.
        Arguments:
            data: the value
        Returns:
            hex string representing the bits of the given value
        '''

        data = str(data)
        output = ""

        if data == "nan":
            output = BinaryMetfile.int_to_hex(1)
            output += BinaryMetfile.__symbol_hex['nan']

        else:
            #numbers ending in .0 are just the number before the decimal
            data = data.replace(".0", "")
            data = data.replace("-0.", "-.")

            output = BinaryMetfile.int_to_hex(len(data), 1)
            for char in data:
                output += BinaryMetfile.__symbol_hex[char]

        return output

        @staticmethod
    def write_bytes(constants:dict, columns:list, units:list, data:list) -> bytes:
        '''
        Given the data for a metfile, returns the metfile in binary format in bytes
        Arguments:
            constants: A dictionary of constants and metadata. Stored as name and value, both as strings
            columns: A list of string column names
            units: A list of string unit names for each column
            data: A list of rows where each row has a value for each column. All values must be numbers.
        Returns:
            the metfile as bytes
        '''

        #make copies of these inputs so we can edit them as needed
        constants = constants.copy()
        columns = columns.copy()
        units = units.copy()

        #add start date to constants
        constants["start_date"] = data[0][0]

        new_data = []
        #remove date from data
        if "date" in columns:
            for i in range(0, len(columns)):
                if columns[i] == "date":
                    index = i
            columns.pop(index)
            units.pop(index)
            for row in data:
                new_row = row.copy()
                new_row.pop(0)
                new_data.append(new_row)

        #version
        output = BinaryMetfile.str_to_hex("met-bin-1")
        
        #constants
        output += BinaryMetfile.int_to_hex(len(constants.items()))
        for key, value in constants.items():
            output += BinaryMetfile.str_to_hex(key)
            output += BinaryMetfile.str_to_hex(str(value))

        #headers
        #titles
        output += BinaryMetfile.int_to_hex(len(columns))
        for value in columns:
            output += BinaryMetfile.str_to_hex(value)
        #units
        for value in units:
            output += BinaryMetfile.str_to_hex(value)

        #data
        output += BinaryMetfile.int_to_hex(len(new_data), 8)
        for row in new_data:
            for value in row:
                output += BinaryMetfile.encode_num(value)

        #if output is an odd length
        if len(output) % 2: 
            output += "0"

        return bytes.fromhex(output)
        */

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
        public class BinaryData
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
        public class StringPair
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
        public class MetConstant
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
        public class MetColumn
        {
            /// <summary>
            /// 
            /// </summary>
            public bool IsFirstColumn { get; set; }

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
        public class MetRow
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
        public class MetData
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
    }
}



