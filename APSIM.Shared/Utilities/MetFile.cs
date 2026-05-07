using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// Metfile
    /// ---------------
    /// This is a c# class for writing and reading metfiles.
    ///
    /// ---------------
    /// Text (.met)
    /// ---------------
    /// This is the default met file standard, however due to the age of this 
    /// standard, and the resources that have been written to write met files, 
    /// not all files follow the standard exactly. Therefore while we've taken 
    /// care to make the reading and writing of met files as flexibile as we 
    /// can based on the collection stored within apsim, there may be 
    /// variations that this library cannot deal with.
    /// 
    /// Text met files follow the following structure:
    /// - Starts with a header of [weather.met.weather], but this is optional 
    ///   esspeically on older met files. [standard.met.weather] has also been 
    ///   observed in some files. When writing a text metfile, this will be 
    ///   added even if the original did not have it.
    /// 
    /// - This is followed by lines that define constants and other text 
    ///   comments about the met file.
    /// 
    /// - Comments always start with a exclamation mark (!) and are followed by 
    ///   any text. These can appear on any line and all content on the line 
    ///   after the excamation mark is treated as a comment and not processed.
    /// 
    /// - Constants are a key-value pair of text with an equals (=) in between. 
    ///   These often define constant values such as latitude and longitutde, 
    ///   tav and amp values or other metadata about the metfile.
    /// 
    /// - When a line is found without an = or ! in it, and it contains the 
    ///   text "rain", "maxt" and "mint", we determine that to be the line 
    ///   containing the header text for each of our columns. The column names 
    ///   are whitespace seperated (normally with spaces) and must equal the 
    ///   number of columns found within the daily data.
    /// 
    /// - Under the column names, the next row are the units for each column,
    ///   with one unit per column. These are often (but not always) defined 
    ///   with the unit wrapped in brackets (), and if written out by this 
    ///   library, brackets will be added to the units even if they did not 
    ///   previously use them.
    /// 
    /// - Neither the Column names or units lines are allowed to contain a 
    ///   comment.
    /// 
    /// - Every line after the units line is then treated as daily data, where 
    ///   each whitespace seperated value is a value for a column. These values 
    ///   can be either integer numbers, decimals or text values. A comment can 
    ///   be placed at the end of the line of daily data using the ! notation.
    /// 
    /// - All days must be consistent as the file is read, if there are missing 
    ///   days or duplicate rows, an error will be thrown. Metfiles use a 
    ///   standard calander and so leap years must contain the 29th of Feburary.
    /// 
    /// - Blank lines and whitespace padding are common in metfiles to help 
    ///   make them more readable, however this library will remove non-comment 
    ///   padding when writing met files back, as it uses its own standard for 
    ///   spacing out the text file.
    /// 
    /// ---------------
    /// Binary (.bin)
    /// Version: 2
    /// ---------------
    /// In order to reduce file size, the following modifications are made to 
    /// the data when writing:
    /// 
    /// - A start date is stored above the constants and each row of the data 
    ///   is assumed therefore to be the next day in sequeunce. When reading 
    ///   out, the date must be added back to the data rows.
    /// 
    /// - Numbers are stored as a list of symbol lookups instead of a smaller 
    ///   number format like float16. This is to avoid any rounding or 
    ///   mathematical errors when doing this writing, storing as symbols 
    ///   ensures the exact same number goes in and out.
    /// 
    /// - If a better version of this is developed in the future, a version 
    ///   string is included as the first bytes in the file so that versions 
    ///   can be handled in the future.
    /// 
    /// - Hexidecimals are used for handling memory here because the smallest 
    ///   unit we need is 4-bits. This allows the script to be simplier than 
    ///   with binary and run faster.
    /// 
    /// - Each day is recorded as the difference between that day and the last. 
    ///   So if the max temperature is 25.6 on the first day and 23.5 on the 
    ///   2nd day, a value of -2.1 is stored. The first day is the difference 
    ///   between the value and 0.
    /// 
    /// - If the difference between the last day and the next is 0, then a 0 
    ///   length value is stored, representing no change.
    /// 
    /// - If a column contains text data, the entire column will be stored as 
    ///   text values instead of numbers. However the text value is still 
    ///   compared between rows and is only stored again if it changes.
    /// 
    /// - If a daily row has a comment on the end, it will be stored in an 
    ///   added column at the end of the row, and restored to a comment when 
    ///   read back out.
    /// 
    /// - Values are limited to 15 characters, so any precision after 15 
    ///   characters will be cut off the numbers.
    /// 
    /// The binary file is built to the follow schema:
    /// Data Types:
    /// - NUMBER: 4-bit int (length) + array of 4-bit int for symbol lookup
    /// - WORD: 8-bit int (length) + array of utf-8 char
    /// 
    /// Structure:
    /// WORD (Version)
    /// 
    /// WORD (Start Date)
    ///
    /// 8-bit int (Number of constants)
    /// WORD x constants (Name)
    /// WORD x constants (Value)
    /// WORD x constants (Comment)
    /// 
    /// 8-bit int (Number of columns)
    /// WORD x columns (Name)
    /// WORD x columns (Units)
    /// 4-bit int x columns (Type)
    /// 4-bit int x columns (Decimal Places)
    /// 
    /// 32-bit int (Number of data rows)
    /// NUMBER x columns x rows (difference values, stored column first)
    /// 
    /// ---------------
    /// Binary (.bin)
    /// Version: 1
    /// ---------------
    /// Deprecated Legacy version of the binary format.
    /// Does not support comments, or text data, has poorer compression.
    /// 
    /// Structure:
    /// WORD (Version)
    ///
    /// 8-bit int (Number of constants)
    /// (WORD + WORD) x constants (Per constant: "name" + "value")
    /// 
    /// 8-bit int (Number of columns)
    /// WORD x columns (Per column: "name")
    /// WORD x columns (Per column: "unit")
    /// 
    /// 32-bit int (Number of data rows)
    /// NUMBER x columns x rows
    /// </summary>
    public class MetFile
    {
        /// <summary>
        /// Minimum spacing for a column of data. Values that are less than 
        /// this in a column will be padded with whitespace
        /// </summary>
        private static int MIN_COLUMN_WIDTH = 8;

        /// <summary>
        /// Constant used as a column name when writing a binary file with 
        /// comments alongside daily data values. This could potentially cause 
        /// a name conflict if a metfile has a column with exactly the same 
        /// name.
        /// </summary>
        private static string DATA_COMMENT_COLUMN = "DATA_COMMENT";
        
        /// <summary>
        /// Supported Met File formats
        /// </summary>
        public enum MetFileFormat { 
            /// <summary>
            /// Extension: .met
            /// Human readable text based met file. Consists of constants, 
            /// comments, column headers, units and a row for each day of data.
            /// Whitespace may be modified if loading non-standard text 
            /// metfiles and resaving.
            /// </summary>
            Text = 0,
            /// <summary>
            /// Extension: .bin
            /// A compressed binary representation of a metfile. Will not drop 
            /// information during the conversion, however will combine year, 
            /// day columns into a date and reorder columns if in non-standard 
            /// order.
            /// Supports constants, comments, headers, units and double or text 
            /// data.
            /// </summary>
            Binary = 1
        }

        private MetData data;

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public MetFile()
        {
            data = new MetData();
        }

        /// <summary>
        /// Load a metfile from the given pass and create MetFile object
        /// </summary>
        /// <param name="filepath">Filepath</param>
        public MetFile(string filepath)
        {
            Load(filepath);
        }

        /// <summary>
        /// Loads a metfile from the given filepath into this Metfile object
        /// Accepts:
        ///   - .met
        ///   - .bin
        ///   - .zip (that contains a .met or .bin)
        /// </summary>
        /// <param name="filepath">Filepath</param>
        public void Load(string filepath)
        {
            byte[] bytes = new byte[0];
            MetFileFormat format = MetFileFormat.Text;
            if (filepath.ToLower().EndsWith(".zip"))
            {
                WeatherZip zip = UnpackWeatherFromZip(filepath);
                bytes = zip.bytes;
                format = zip.format;
            }
            else if (filepath.ToLower().EndsWith(".met") || filepath.ToLower().EndsWith(".bin"))
            {
                if (filepath.ToLower().EndsWith(".met"))
                    format = MetFileFormat.Text;
                else if (filepath.ToLower().EndsWith(".bin"))
                    format = MetFileFormat.Binary;
                bytes = File.ReadAllBytes(filepath);
            }
            else if (!filepath.Contains('.'))
            {
                format = MetFileFormat.Binary;
                bytes = File.ReadAllBytes(filepath);
            }
            else
            {
                string extension = filepath.Substring(filepath.LastIndexOf('.'));
                throw new Exception($"File {filepath} has extension {extension} which is not recognised. Must be a .met or .bin file.");
            }
            data = Load(filepath, bytes, format);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="bytes"></param>
        public void Load(string filepath, byte[] bytes)
        {
            if (filepath.ToLower().EndsWith(".met"))
                data = Load(filepath, bytes, MetFileFormat.Text);
            else if (filepath.ToLower().EndsWith(".bin"))
                data = Load(filepath, bytes, MetFileFormat.Binary);
            else
            {
                string extension = filepath.Substring(filepath.LastIndexOf('.'));
                throw new Exception($"File {filepath} has extension {extension} which is not recognised. Must be a .met or .bin file.");
            }
        }

        /// <summary>
        /// Allows loading of a metfile from a series of arrays instead of from 
        /// file. Used for situations where metfile data is being created from 
        /// another type of data source.
        /// </summary>
        /// <param name="constants">
        /// Array of constant strings. Provided in the format of:
        /// Name = Value
        /// </param>
        /// <param name="columns">An array of column names</param>
        /// <param name="units">An array of unit names</param>
        /// <param name="values">
        /// An array of values. This is a 2x2 array stored 1 dimensionally, 
        /// where each row has a width equal to the number of columns. Date is 
        /// not stored on each row.
        /// </param>
        /// <param name="startDate">
        /// The starting date in string format yyy-MM-dd
        /// </param>
        public void Load(string[] constants, string[] columns, string[] units, double[] values, string startDate)
        {
            data = new MetData();
            foreach(string line in constants)
            {
                if (!line.Contains('='))
                    throw new Exception($"{line} is supposed to be a constant but does not contain an equals '=' sign");
                string[] parts = line.Split('=');
                MetConstant constant = new MetConstant(parts[0].Trim(), parts[1].Trim());
                data.Contants.Add(constant);
            }

            if (columns.Length != units.Length)
                throw new Exception($"Columns array and Units array have different sizes. {columns.Length} != {units.Length}");

            int numColumns = columns.Length;
            for(int i = 0; i < numColumns; i++)
            {
                MetColumn column = new MetColumn(columns[i], units[i]);
                data.Columns.Add(column);
            }

            if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                throw new Exception($"Cannot read met file. Start date is {startDate} which must be in yyyy-MM-dd format");

            for(int i = 0; i < values.Length; i += numColumns)
            {
                MetRow row = new MetRow();
                row.Date = date;
                date = date.AddDays(1);

                for(int j = 0; j < numColumns; j++)
                {
                    row.Inputs.Add(values[i].ToString());
                    row.Values.Add(values[i]);
                }
                data.Rows.Add(row);
            }
        }

        /// <summary>
        /// Save the content of the metfile to the given filepath.
        /// Optional format flag, defaults to .met text file.
        /// </summary>
        /// <param name="filepath">Filepath</param>
        /// <param name="format">MetFileFormat value</param>
        public void Save(string filepath, MetFileFormat format = MetFileFormat.Text)
        {
            Save(filepath, data, format);
        }

        /// <summary>
        /// Returns the value of the given constant. Will return null if 
        /// constant was not found.
        /// </summary>
        /// <param name="name"></param>
        public string GetConstant(string name)
        {
            foreach(MetConstant constant in data.Contants)
                if (constant.Name == name)
                    return constant.Value;
            return null;
        }

        /// <summary>
        /// Returns the double values for the given date
        /// </summary>
        public double[] GetDay(DateTime date)
        {
            int days = (date - data.StartDate).Days;
            if (date == data.Rows[days].Date)
                return data.Rows[days].Values.ToArray();
            else
            {
                foreach(MetRow row in data.Rows)
                if (date == row.Date)
                    return row.Values.ToArray();
            }
            throw new Exception($"Date {date.ToString("yyyy-MM-dd")} not found in MetFile");
        }

        /// <summary>
        /// Returns the met file as a text memory stream.
        /// </summary>
        /// <param name="format">Met Format to create the stream from</param>
        /// <returns>A MemoryStream of bytes for the MetFile</returns>
        public Stream GetStream(MetFileFormat format)
        {
            if (format == MetFileFormat.Text)
            {
                string output = WriteMet(data);
                return new MemoryStream(Encoding.UTF8.GetBytes(output));
            }
            else if (format == MetFileFormat.Binary)
            {
                HexData output = WriteBinaryV2(data);
                return new MemoryStream(Convert.FromHexString(output.Hex));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Given a list of column names, will remove all columns that aren't 
        /// in the list.
        /// </summary>
        /// <param name="columns">List of column names to keep</param>
        public void WhitelistColumns(string[] columns)
        {
            List<string> columnsToRemove = new List<string>();
            foreach(MetColumn column in data.Columns)
                if (!columns.Contains(column.Name))
                    columnsToRemove.Add(column.Name);
            data = RemoveColumns(data, columnsToRemove);
        }

        /// <summary>
        /// Allows for reducing the precision within the file by limiting how 
        /// many digits values in the given columns are allowed to have.
        /// If no column names are provided, the restriction is applied to all 
        /// columns.
        /// </summary>
        /// <param name="decimalPlaces">List of column names to keep</param>
        /// <param name="columns">List of column names to keep</param>
        public void LimitDecimalPercision(int decimalPlaces, string[] columns = null)
        {
            foreach(MetColumn column in data.Columns)
            {
                bool limit = false;
                if (columns == null)
                    limit = true;
                else if (columns.Contains(column.Name))
                    limit = true;
                if(limit)
                    column.DecimalPlaces = decimalPlaces;
            }
        }

        /// <summary>List of comments within met file</summary>
        public string[] Comments
        {
            get
            {
                List<string> comments = new List<string>();
                foreach(MetConstant constant in data.Contants)
                    if (!string.IsNullOrEmpty(constant.Comment))
                        comments.Add(constant.Comment);
                foreach(MetRow row in data.Rows)
                    if (!string.IsNullOrEmpty(row.Comment))
                        comments.Add(row.Date.ToString("yyyy-MM-dd") + " " + row.Comment);
                return comments.ToArray();
            }
        }

        /// <summary>List of constants within met file</summary>
        public string[] Contants
        {
            get
            {
                List<string> constants = new List<string>();
                foreach(MetConstant constant in data.Contants)
                    if (string.IsNullOrEmpty(constant.Name))
                        constants.Add(constant.Name);
                return constants.ToArray();
            }
        }

        /// <summary>List of column header names</summary>
        public string[] Columns
        {
            get
            {
                List<string> columns = new List<string>();
                foreach(MetColumn column in data.Columns)
                    columns.Add(column.Name);
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
                    units.Add(column.Unit);
                return units.ToArray();
            }
        }

        /// <summary>The number of days within the met file</summary>
        public int NumberOfDays
        {
            get
            {
                return data.Rows.Count;
            }
        }

        /// <summary>The starting date of the met file</summary>
        public DateTime StartDate
        {
            get
            {
                if (NumberOfDays > 0)
                    return data.Rows[0].Date;
                else
                    throw new Exception("Met file has no daily data, cannot get starting date");
            }
        }

        /// <summary>
        /// Compares this met file to another and returns true if they are effectively the same. 
        /// This is not a byte for byte comparison, but instead checks that the contents are equivalent.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Compare(MetFile other, MetFile current)
        {
            // Length checks
            if (other.Columns.Length != current.Columns.Length)
                return false;
            if (other.Comments.Length != current.Comments.Length)
                return false;
            if (other.Contants.Length != current.Contants.Length)
                return false;
            if (other.Units.Length != current.Units.Length)
                return false;

            // Value checks
            if (!Enumerable.SequenceEqual(other.Columns, current.Columns))
                return false;
            if (!Enumerable.SequenceEqual(other.Comments, current.Comments))
                return false;
            if (!Enumerable.SequenceEqual(other.Contants, current.Contants))
                return false;
            if (!Enumerable.SequenceEqual(other.Units, current.Units))
                return false;
            if (other.StartDate != current.StartDate)
                return false;
            if (other.NumberOfDays != current.NumberOfDays)
                return false;

            // If we've made it here, the files are effectively the same (they may differ on whitespace)
            return true;
        }

        /// <summary>
        /// Converts a MetFile object from using year and day columns to a single date column
        /// </summary>
        /// <param name="met"></param>
        /// <returns>If the conversion happened</returns>
        public static bool ConvertYearDayToDate(MetFile met)
        {
            bool hasYearColumn = false;
            bool hasDayColumn = false;
            bool hasDateColumn = false;
            int yearIndex = 0;
            int dayIndex = 0;
            for (int i = 0; i < met.Columns.Length; i++)
            {
                string name = met.Columns[i].ToLower();
                if (name == "year")
                {
                    hasYearColumn = true;
                    yearIndex = i;
                }
                if (name == "day")
                {
                    hasDayColumn = true;
                    dayIndex = i;
                }
                if (name == "date")
                    hasDateColumn = true;
            }

            if (!hasYearColumn || !hasDayColumn || hasDateColumn)
                return false;
            
            List<MetColumn> columns = new List<MetColumn>();
            columns.Add(new MetColumn("date", ""));
            for (int i = 0; i < met.data.Columns.Count(); i++)
            {
                if (i != yearIndex && i != dayIndex)
                {
                    columns.Add(met.data.Columns[i]);
                }
            }
            met.data.Columns = columns;

            foreach(MetRow row in met.data.Rows)
            {
                List<string> inputs = new List<string>();
                inputs.Add(row.Date.ToString("yyyy-MM-dd"));
                List<double> values = new List<double>();
                values.Add(double.NaN);
                for (int i = 0; i < row.Inputs.Count(); i++)
                {
                    if (i != yearIndex && i != dayIndex)
                    {
                        inputs.Add(row.Inputs[i]);
                        values.Add(row.Values[i]);
                    }
                }
                row.Inputs = inputs;
                row.Values = values;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // Private Static Helper Functions
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Save the given MetData into a file at the filepath given in the 
        /// format provided. If no format is provided, text format will be used.
        /// </summary>
        /// <param name="filepath">Where to save the file</param>
        /// <param name="data">MetData object to save</param>
        /// <param name="format">MetFileFormat to save in</param>
        private static void Save(string filepath, MetData data, MetFileFormat format)
        {
            if (format == MetFileFormat.Text)
            {
                string output = WriteMet(data);
                byte[] bytes = Encoding.UTF8.GetBytes(output);
                File.WriteAllBytes(filepath, bytes);
            }
            else if (format == MetFileFormat.Binary)
            {
                HexData output = WriteBinaryV2(data);
                byte[] bytes = Convert.FromHexString(output.Hex);
                File.WriteAllBytes(filepath, bytes);
            }
            return;
        }

        /// <summary>
        /// Opens a binary file and converts it to a valid string 
        /// representation in a Stream object
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="bytes"></param>
        /// <param name="format">MetFileFormat to save in</param>
        /// <returns>
        /// A MetData object containing the contents of the met file read in.
        /// </returns>
        private static MetData Load(string filepath, byte[] bytes, MetFileFormat format)
        {
            try
            {
                if (format == MetFileFormat.Text)
                {
                    string text = Encoding.UTF8.GetString(bytes);
                    return ReadMet(text);
                }
                else if (format == MetFileFormat.Binary)
                {
                    string text = Convert.ToHexString(bytes);
                    HexData data = new HexData(text, 0);
                    if (ReadBinaryVersion(data) == 1)
                        return ReadBinaryV1(data);
                    else
                        return ReadBinaryV2(data);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                throw new Exception($"Cannot load met file {filepath}. Error: {exception.Message}");
            }
            
        }

        /// <summary>
        /// Given a HexData object, reads which binary version it was saved 
        /// with. This is stored as a string at the start of the file and 
        /// after being read, the read position within the HexData object is 
        /// reset back to the start.
        ///</summary>
        /// <param name="data">HexData object to be read</param>
        /// <returns>
        /// Version number that was found.
        /// </returns>
        /// 
        private static int ReadBinaryVersion(HexData data)
        {
            //read version
            string version = HexToString(data);
            data.Position = 0;

            if (version == "met-bin-1")
                return 1;
            else if (version == "met-bin-2")
                return 2;
            else
                throw new Exception($"Binary file has version {version} which is not recognised.");
        }

        /// <summary>
        /// Given a string, reads the contents in as a metfile and creates a 
        /// MetData object.
        /// </summary>
        /// <param name="input">Text to be read</param>
        /// <returns>
        /// A MetData object with the contents of the Text
        /// </returns>
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
            List<string> headerLines = new List<string>();
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
                        string trimmedLower = trimmed.ToLower();

                        //check if the line has a comment or constant symbol
                        bool hasSymbol = false;
                        foreach(char symbol in new char[] {'!', '='}) 
                            if (trimmedLower.Contains(symbol))
                                hasSymbol = true;
                        
                        //if it doesn't have a symbol, check if the line has known column names
                        bool hasAllNames = true;
                        if (!hasSymbol)
                            foreach(string name in new string[] {"maxt", "mint", "rain"})
                                if (!trimmedLower.Contains(name))
                                    hasAllNames = false;

                        if (!hasSymbol && hasAllNames)
                        {
                            step = 1; // set this to one so the next line is read for units
                            columnNameLine = trimmed;
                        }
                        else
                        {
                            headerLines.Add(trimmed);
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

            //read our header rows
            foreach(string line in headerLines)
            {
                string trimmed = line.Trim();
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
                else //we just have text on a line, treat as a comment
                {
                    metData.Contants.Add(new MetConstant("! "+ trimmed));
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
            DateTime prevDay = DateTime.MinValue;
            bool firstRow = true;
            foreach(string line in dataLines)
            {
                MetRow row = new MetRow();
                string trimmed = line.Trim();

                //check if row contains a comment
                if (trimmed.Contains('!'))
                {
                    int position = trimmed.IndexOf('!');
                    string comment = trimmed.Substring(position);
                    trimmed = trimmed.Substring(0, position);
                    row.Comment = comment;
                }

                //find the values for each column, there may be variable amounts of whitespace
                string[] parts = trimmed.Split(" ");
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
                    columns[i].UpdateWidth(stringValue);
                    columns[i].UpdateType(stringValue);

                    string columnName = columns[i].Name.ToLower();
                    if (columnName == "date")
                    {
                        DateTime date;
                        stringValue = stringValue.Replace("/", "-");
                        stringValue = stringValue.Replace(",", "-");
                        stringValue = stringValue.Replace(".", "-");
                        bool success = DateTime.TryParseExact(stringValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        if(!success)
                            success = DateTime.TryParseExact(stringValue, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        if(!success)
                            success = DateTime.TryParseExact(stringValue, "d-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        if(!success)
                            success = DateTime.TryParseExact(stringValue, "dd-M-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        if(!success)
                            success = DateTime.TryParseExact(stringValue, "d-M-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        if (!success)
                            throw new Exception($"Cannot read met file. Row {line} has date of {stringValue} which must be in yyyy-MM-dd or dd-MM-yyyy format");
                        row.Date = date;
                    }
                    else if (columnName == "year")
                    {
                        if (int.TryParse(stringValue, out int yearInt))
                        {
                            year = yearInt;
                            if (day > 0)
                                row.Date = new DateTime(year, 1, 1).AddDays(day-1);
                        }
                        else if (double.TryParse(stringValue, out double yearDouble))
                        {
                            year = (int)Math.Round(yearDouble);
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
                        if (int.TryParse(stringValue, out int dayInt))
                        {
                            day = dayInt;
                            if (year > 0)
                                row.Date = new DateTime(year, 1, 1).AddDays(dayInt-1);
                        }
                        else if (double.TryParse(stringValue, out double dayDouble))
                        {
                            day = (int)Math.Round(dayDouble);
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
                        row.Values.Add(double.NaN); //where we have columns with string, we just set the value to NaN
                }

                //Check if the date is exact one day after the previous row, and throw if it is not.
                //met files are not allowed to have gaps in them, APSIM will throw if they do.
                if (firstRow)
                {
                    firstRow = false;
                    prevDay = row.Date;
                    metData.StartDate = row.Date;
                }
                else
                {
                    prevDay = prevDay.AddDays(1);
                    if (prevDay != row.Date)
                        throw new Exception($"Met file does not have consistent dates. Day {prevDay.ToString("yyyy-MM-dd")} was expected, but day {row.Date.ToString("yyyy-MM-dd")} was read.");
                }

                metData.Rows.Add(row);
            }

            return metData;
        }

        /// <summary>
        /// Writes a MetData object back to a text met file string
        /// </summary>
        /// <param name="metData">MetData to be converted to text</param>
        /// <returns>String representation of the metfile</returns>
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
            foreach(MetRow row in metData.Rows)
            {
                int index = 0;
                foreach(string data in row.Inputs)
                {
                    output.Append(data.PadLeft(columns[index].GetFormattingWidth()));
                    output.Append(" ");
                    index += 1;
                }
                output.Remove(output.Length-1, 1);

                if (!string.IsNullOrEmpty(row.Comment))
                    output.Append(" " + row.Comment);

                output.Append("\n");
            }

            return output.ToString();
        }

        /// <summary>
        /// Binary metfile writer, version 2.
        /// Stores everything a text metfile can, but in a much smaller size.
        /// Improvement on the first binary version which lacked features for 
        /// certain types of metfile.
        /// </summary>
        /// <param name="metData">MetData object to be written</param>
        /// <returns>
        /// A HexData object containing the contents of the met file in hex 
        /// symbols.
        /// </returns>
        private static HexData WriteBinaryV2(MetData metData)
        {
            StringBuilder output = new StringBuilder(2000000);
            //version
            output.Append(StringToHex("met-bin-2"));

            //add start date
            string start_date = metData.Rows[0].Date.ToString("yyyy-MM-dd");
            //remove date from data
            MetData datelessMetData = RemoveColumns(metData, new List<string>() {"date", "year", "day"});

            //check if we need to add a comment column to the data
            bool hasDataComments = false;
            foreach(MetRow row in datelessMetData.Rows)
                if (!string.IsNullOrEmpty(row.Comment))
                    hasDataComments = true;

            if (hasDataComments)
            {
                MetColumn commentColumn = new MetColumn(DATA_COMMENT_COLUMN, "");
                commentColumn.DataType = typeof(string);
                commentColumn.DecimalPlaces = 0;
                commentColumn.Width = 0;
                commentColumn.IsFirstColumn = false;
                datelessMetData.Columns.Add(commentColumn);
                foreach(MetRow row in datelessMetData.Rows)
                {
                    row.Values.Add(double.NaN);
                    row.Inputs.Add(row.Comment);
                }
            }

            output.Append(StringToHex(start_date));
            
            //-- Constants --
            //Number of Constants / Comments
            output.Append(UIntToHex(datelessMetData.Contants.Count, 2));
            //Names
            foreach(MetConstant constant in datelessMetData.Contants)
                output.Append(StringToHex(constant.Name));
            //Values
            foreach(MetConstant constant in datelessMetData.Contants)
                output.Append(StringToHex(constant.Value));
            //Comments
            foreach(MetConstant constant in datelessMetData.Contants)
                output.Append(StringToHex(constant.Comment));

            //-- Columns --
            //Number of columns
            int columnsLength = datelessMetData.Columns.Count;
            output.Append(UIntToHex(columnsLength, 2));
            //Names
            foreach(MetColumn column in datelessMetData.Columns)
                output.Append(StringToHex(column.Name));
            //Units
            foreach(MetColumn column in datelessMetData.Columns)
                output.Append(StringToHex(column.Unit));
            //Data types
            foreach(MetColumn column in datelessMetData.Columns)
                output.Append(TypeToHex(column.DataType));
            //Decimal Places
            foreach(MetColumn column in datelessMetData.Columns)
                output.Append(UIntToHex(column.DecimalPlaces));

            //-- Data --
            //Number of rows
            output.Append(UIntToHex(datelessMetData.Rows.Count, 8));
            for(int i = 0; i < columnsLength; i++)
            {
                double previousValue = 0;
                string previousString = "";
                int decimalPlaces = datelessMetData.Columns[i].DecimalPlaces;
                Type dataType =  datelessMetData.Columns[i].DataType;
                foreach(MetRow row in datelessMetData.Rows)
                {
                    if (dataType == typeof(string))
                    {
                        string input = row.Inputs[i];
                        if (input == previousString)
                        {
                            output.Append(UIntToHex(0));
                        }
                        else
                        {
                            output.Append(UIntToHex(1));
                            output.Append(StringToHex(input));
                            previousString = input;
                        }
                    }
                    else
                    {
                        double value = row.Values[i];
                        if (double.IsNaN(value))
                        {
                            output.Append(EncodeNumber("nan"));
                            previousValue = double.NaN;
                        }
                        else
                        {
                            if (double.IsNaN(previousValue))
                            {
                                double difference = Math.Round(-row.Values[i], decimalPlaces);
                                string differenceString = difference.ToString();
                                output.Append(EncodeNumber(differenceString));
                            }
                            else
                            {
                                double difference = Math.Round(previousValue - row.Values[i], decimalPlaces);
                                string differenceString = difference.ToString();
                                if (differenceString == "0")
                                    output.Append(UIntToHex(0));
                                else
                                    output.Append(EncodeNumber(differenceString));
                            }
                            previousValue = row.Values[i];
                        }
                    }
                }
            }

            //if output is an odd length
            if (output.Length % 2 == 1) 
                output.Append("0");

            HexData data = new HexData(output.ToString(), 0);
            return data;
        }

        /// <summary>
        /// Binary metfile reader, version 2.
        /// Reads a version binary metfile and creates a MetData object with 
        /// the contents.
        /// </summary>
        /// <param name="data">A HexData object with the contents of the file</param>
        /// <returns>
        /// A MetData object with the value read in.
        /// </returns>
        private static MetData ReadBinaryV2(HexData data)
        {
            MetData metData = new MetData();

            //read version
            string version = HexToString(data);

            //get start date
            string startDateString = HexToString(data);
            if (!DateTime.TryParseExact(startDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate))
                throw new Exception($"Cannot read met file. Start date is {startDate} which must be in yyyy-MM-dd format");
            metData.StartDate = startDate;

            //read constants
            int constantsLength = HexToUInt(data, 2);
            for (int i = 0; i < constantsLength; i++)
                metData.Contants.Add(new MetConstant());

            foreach(MetConstant constant in metData.Contants)
                constant.Name = HexToString(data);

            foreach(MetConstant constant in metData.Contants)
                constant.Value = HexToString(data);
            
            foreach(MetConstant constant in metData.Contants)
                constant.Comment = HexToString(data);

            //headers
            int columnsLength = HexToUInt(data, 2);
            for (int i = 0; i < columnsLength; i++)
                metData.Columns.Add(new MetColumn());

            foreach(MetColumn column in metData.Columns)
                column.Name = HexToString(data);

            foreach(MetColumn column in metData.Columns)
                column.Unit = HexToString(data);

            foreach(MetColumn column in metData.Columns)
                column.DataType = HexToType(data);

            foreach(MetColumn column in metData.Columns)
                column.DecimalPlaces = HexToUInt(data, 1);

            //Add date column back in at front
            columnsLength += 1;
            metData.Columns.Insert(0, new MetColumn("date", ""));

            //create rows and add date column to them
            int rowsLength = HexToUInt(data, 8);
            DateTime date = startDate;
            for (int i = 0; i < rowsLength; i++)
            {
                MetRow row = new MetRow();
                row.Date = date;
                string text = date.ToString("yyyy-MM-dd");
                row.Inputs.Add(text);
                row.Values.Add(double.NaN);
                date = date.AddDays(1);
                metData.Rows.Add(row);
                metData.Columns[0].UpdateWidth(text);
            }

            //data
            for (int i = 1; i < columnsLength; i++)
            {
                Type dataType =  metData.Columns[i].DataType;
                int decimalPlaces = metData.Columns[i].DecimalPlaces;
                double previousValue = 0;
                string previousString = previousValue.ToString("F"+decimalPlaces);
                if (dataType == typeof(string))
                    previousString = "";
                foreach(MetRow row in metData.Rows)
                {
                    if (dataType == typeof(string))
                    {
                        int difference = HexToUInt(data, 1);
                        row.Values.Add(double.NaN);
                        if (difference == 1) //string is not the same as previous
                            previousString = HexToString(data);
                        row.Inputs.Add(previousString);
                    }
                    else
                    {
                        string differenceString = DecodeNumber(data);
                        if (double.IsNaN(previousValue))
                            previousValue = 0;

                        if (differenceString.Length != 0)
                        {
                            previousValue = Math.Round(previousValue - double.Parse(differenceString), decimalPlaces);
                            previousString = previousValue.ToString("F"+decimalPlaces);
                        }
                        row.Values.Add(previousValue);
                        row.Inputs.Add(previousString);
                        metData.Columns[i].UpdateWidth(previousString);
                    }
                }
            }

            //check if we have data comments and put them back as comments instead of a column
            bool hasDataComments = false;
            foreach(MetColumn column in metData.Columns)
                if (column.Name == DATA_COMMENT_COLUMN)
                    hasDataComments = true;

            if (hasDataComments)
            {
                int index = metData.Columns.Count-1;
                foreach(MetRow row in metData.Rows)
                {
                    if (!string.IsNullOrEmpty(row.Inputs[index]))
                        row.Comment = row.Inputs[index];
                    row.Values.RemoveAt(index);
                    row.Inputs.RemoveAt(index);
                }
                metData.Columns.RemoveAt(index);
            }

            return metData;
        }

        /// <summary>
        /// Removes columns that match the provided names (case insensitive) 
        /// and the data in those columns from a MetData object.
        /// </summary>
        /// <param name="metData">MetData object to clone and modify</param>
        /// <param name="names">List of column names to remove</param>
        /// <returns>A new MetData object without the provided columns</returns>
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
            
            foreach(MetRow row in metData.Rows)
            {
                MetRow newRow = new MetRow();
                newRow.Comment = row.Comment;
                newRow.Date = row.Date;
                for (int i = 0; i < row.Values.Count; i++)
                {
                    if (!removedColumnIndexs.Contains(i))
                    {
                        newRow.Inputs.Add(row.Inputs[i]);
                        newRow.Values.Add(row.Values[i]);
                    }
                }
                output.Rows.Add(newRow);
            }

            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static WeatherZip UnpackWeatherFromZip(string filepath)
        {
            bool foundWeather = false;
            byte[] bytes = new byte[0];
            MetFileFormat format = MetFileFormat.Text;
            byte[] zipBytes = File.ReadAllBytes(filepath);
            using (MemoryStream zipStream = new MemoryStream(zipBytes))
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!foundWeather)
                        {
                            if (entry.FullName.EndsWith(".met"))
                            {
                                format = MetFileFormat.Text;
                                foundWeather = true;
                            }
                            else if (entry.FullName.EndsWith(".bin"))
                            {
                                format = MetFileFormat.Binary;
                                foundWeather = true;
                            }
                            else if (!entry.FullName.Contains('.'))
                            {
                                format = MetFileFormat.Binary;
                                foundWeather = true;
                            }
                            if (foundWeather)
                            {
                                //Read file content into memory
                                using (Stream entryStream = entry.Open())
                                {
                                    using(MemoryStream memoryStream = new MemoryStream())
                                    {
                                        entryStream.CopyTo(memoryStream);
                                        bytes = memoryStream.ToArray();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!foundWeather)
                throw new Exception($"Cannot find met file in {filepath}. Zip file must contain a .met or .bin file.");
            else
                return new WeatherZip(bytes, format);
        }

        ////////////////////////////////////////////////////////////////////////
        // Hex Conversion Functions
        ////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// Converts a system Type to a hex character
        /// Types allowed: DateTime, string, double
        /// </summary>
        /// <param name="type">System Type</param>
        /// <returns>
        /// A hex string of length 1 with the symbol for that type
        /// </returns>
        private static char TypeToHex(Type type)
        {
            if (type == typeof(DateTime))
                return '1';
            else if (type == typeof(string))
                return '2';
            else if (type == typeof(double))
                return '3';
            else
                return '0';
        }
        
        /// <summary>
        /// Converts a hex character to a system Type
        /// Types allowed: DateTime, string, double
        /// Will 
        /// update the hex memory position after reading.
        /// </summary>
        /// <param name="data">Hex memory object</param>
        /// <returns>
        /// A system type matching the provided hex value
        /// </returns>
        private static Type HexToType(HexData data)
        {
            char c = data.Hex[data.Position];
            data.Position += 1;
            switch(c)
            {
                case '1': return typeof(DateTime);
                case '2': return typeof(string);
                case '3': return typeof(double);
                default: return typeof(object);
            }
        }

        /// <summary>
        /// Converts a character to a hex character symbol
        /// These symbols are a restricted character set that only covers the 
        /// characters needed to represent a met number.
        /// </summary>
        /// <param name="symbol">Character to convert</param>
        /// <returns>Hex character</returns>
        private static char SymbolToHex(char symbol)
        {
            switch(symbol)
            {
                case '0': return '0';
                case '1': return '1';
                case '2': return '2';
                case '3': return '3';
                case '4': return '4';
                case '5': return '5';
                case '6': return '6';
                case '7': return '7';
                case '8': return '8';
                case '9': return '9';
                case '.': return 'A';
                case '-': return 'B';
                case '/': return 'C';
                case 'n': return 'D';
                case 'E': return 'E';
                case ',': return 'F';
                default: return '0';
            }
        }

        /// <summary>
        /// Converts a hex character to a character symbol
        /// These symbols are a restricted character set that only covers the 
        /// characters needed to represent a met number.
        /// Will update the hex memory position after reading.
        /// </summary>
        /// <param name="data">HexData object</param>
        /// <returns>Character of met number</returns>
        private static string HexToSymbol(HexData data)
        {
            char c = data.Hex[data.Position];
            data.Position += 1;
            switch(c)
            {
                case '0': return "0";
                case '1': return "1";
                case '2': return "2";
                case '3': return "3";
                case '4': return "4";
                case '5': return "5";
                case '6': return "6";
                case '7': return "7";
                case '8': return "8";
                case '9': return "9";
                case 'A': return ".";
                case 'B': return "-";
                case 'C': return "/";
                case 'D': return "nan";
                case 'E': return "E";
                case 'F': return ",";
                default: return "";
            }
        }

        /// <summary>
        /// Converts an unsigned int to a hex string representation.
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>
        /// A hex string of the unsigned integer number
        /// </returns>
        private static char UIntToHex(int value)
        {
            switch(value)
            {
                case 0: return '0';
                case 1: return '1';
                case 2: return '2';
                case 3: return '3';
                case 4: return '4';
                case 5: return '5';
                case 6: return '6';
                case 7: return '7';
                case 8: return '8';
                case 9: return '9';
                case 10: return 'A';
                case 11: return 'B';
                case 12: return 'C';
                case 13: return 'D';
                case 14: return 'E';
                case 15: return 'F';
                default: return '0';
            }
        }

        /// <summary>
        /// Converts a single character hex string to an unsigned int. Will 
        /// update the hex memory position after reading.
        /// Optimised version to speed up DecodeNumber which only uses length 1 
        /// integers.
        /// </summary>
        /// <param name="data">Hex memory object</param>
        /// <returns>
        /// Integer value that was read.
        /// </returns>
        private static int HexToUInt(HexData data)
        {
            char c = data.Hex[data.Position];
            data.Position += 1;
            switch(c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                default: return 0;
            }
        }

        /// <summary>
        /// Converts a varaible size int to a hex string representation.
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <param name="size">The number of hex symbols to use. 1 hex = 4 bits</param>
        /// <returns>
        /// A hex string of the unsigned integer number
        /// </returns>
        private static string UIntToHex(int value, int size)
        {
            string output = value.ToString("x");
            while(output.Length < size)
                output = "0" + output;
            return output;
        }

        /// <summary>
        /// Converts a varaible size hex string to an int. Will 
        /// update the hex memory position after reading.
        /// </summary>
        /// <param name="data">Hex memory object</param>
        /// <param name="size">How many hex symbols to read. 1 hex = 4 bits</param>
        /// <returns>
        /// Unsigned Integer value that was read.
        /// </returns>
        private static int HexToUInt(HexData data, int size)
        {
            string substring = data.Hex.Substring(data.Position, size);
            data.Position += size;
            return Convert.ToInt32(substring, 16);
        }

        /// <summary>
        /// Converts an ASCII string to a hex string. Each ASCII character 
        /// takes up two hex symbols.
        /// </summary>
        /// <param name="data">String to convert</param>
        /// <returns>A hex string representation of the ASCII string</returns>
        private static string StringToHex(string data)
        {
            string length = UIntToHex(data.Length, 2);
            StringBuilder text = new StringBuilder(length);
            foreach(char c in data)
                text.Append(UIntToHex(c, 2));
            return text.ToString();
        }

        /// <summary>
        /// Converts a varaible size hex string to an ASCII string. Will update 
        /// the hex memory position after reading.
        /// Strings are stored with a integer at the start marking how many 
        /// characters are in the string, followed by the text stored in hex 
        /// symbols.
        /// </summary>
        /// <param name="data">Hex memory object</param>
        /// <returns>
        /// The string value that was stored in the given hex memory.
        /// </returns>
        private static string HexToString(HexData data)
        {
            int length = HexToUInt(data, 2);
            int count = length * 2;

            string substring = data.Hex.Substring(data.Position, count);
            byte[] output = Convert.FromHexString(substring);

            data.Position += count;

            return Encoding.UTF8.GetString(output);
        }

        /// <summary>
        /// Converts a metfile number into hex symbols.
        /// 
        /// Uses a symbol lookup table to store characters of a number as a 
        /// string in a small size.
        /// 
        /// The first hex symbol defines the number of characters that make up 
        /// the number, then it is followed by a hex symbol for each character.
        /// </summary>
        /// <param name="data">Number in a string to convert</param>
        /// <returns>Hex string representation of the number</returns>
        private static string EncodeNumber(string data)
        {
            StringBuilder output = new StringBuilder(data.Length + 1);
            string dataLower = data.ToLower();
            if (dataLower == "nan" || dataLower == "n")
            {
                output.Append(UIntToHex(1));
                output.Append(SymbolToHex('n'));
            }
            else
            {
                //numbers ending in .0 are just the number before the decimal
                string trimmed = data;
                if (trimmed.EndsWith(".0"))
                    trimmed = data.Replace(".0", "");
                if (trimmed.StartsWith("-0."))
                    trimmed = trimmed.Replace("-0.", "-.");
                //limit text to 15 characters because length must be single hex digit
                if (trimmed.Length > 15)
                    trimmed = trimmed.Substring(0, 15); 

                output.Append(UIntToHex(trimmed.Length));
                foreach (char c in trimmed)
                    output.Append(SymbolToHex(c));
            }
            return output.ToString();
        }

        /// <summary>
        /// Converts a hex string to a metfile number string. Will update 
        /// the hex memory position after reading.
        /// 
        /// Uses a symbol lookup table to store characters of a number as a 
        /// string in a small size.
        /// 
        /// The first hex symbol defines the number of characters that make up 
        /// the number, then it is followed by a hex symbol for each character.
        /// </summary>
        /// <param name="data">Hex memory object</param>
        /// <returns>
        /// A string representing a number value
        /// </returns>
        private static string DecodeNumber(HexData data)
        {
            int length = HexToUInt(data);

            StringBuilder output = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                output.Append(HexToSymbol(data));

            return output.ToString();
        }

        ////////////////////////////////////////////////////////////////////////
        // Private Helper Classes
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// HexData stores Hex memory object that is read when reading the 
        /// file, and the position through the string that has been read. It is 
        /// up to the reader functions to keep the position correct.
        /// </summary>
        private class HexData
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
            public HexData()
            {
                Hex = "";
                Position = 0;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="hex">Hex string of the data</param>
            /// <param name="position">Position to start at</param>
            public HexData(string hex, int position)
            {
                Hex = hex;
                Position = position;
            }
        }

        /// <summary>
        /// MetConstant holds all the information about constants and header 
        /// comments within a Metfile. MetConstant can have both a constant 
        /// with key and value pair, along with a comment.
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
            /// Constructor for only a constant
            /// </summary>
            /// <param name="name">The name of the pair</param>
            /// <param name="value">The value of the pair</param>
            public MetConstant(string name, string value)
            {
                Name = name;
                Value = value;
                Comment = "";
            }

            /// <summary>
            /// Constructor for only a comment
            /// </summary>
            /// <param name="comment"></param>
            public MetConstant(string comment)
            {
                Name = "";
                Value = "";
                Comment = comment;
            }

            /// <summary>
            /// Constructor for a constant and comment line
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
        /// MetColumn holds all information about columns in a met file to 
        /// help track and rebuild the text version of a file.
        /// 
        /// It includes properties to help with formatting and adding 
        /// whitespace in sensible ways.
        /// </summary>
        private class MetColumn
        {
            /// <summary>
            /// Marker if this is the first column or not. This allows for left 
            /// whitespace padding to be removed if it is.
            /// </summary>
            public bool IsFirstColumn { get; set; }

            /// <summary>
            /// The datatype stored by this column, DateTime, text or number.
            /// </summary>
            public Type DataType { get; set; }

            /// <summary>
            /// Name of the column
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Units of the column (without brackets)
            /// </summary>
            public string Unit { get; set; }

            /// <summary>
            /// Maximum length of this column in characters
            /// Determined by the name, units and longest value.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// The number of decimal places used in this column
            /// Determined by the values in this column.
            /// </summary>
            public int DecimalPlaces { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetColumn()
            {
                Name = "";
                Unit = "";
                Width = 0;
                DecimalPlaces = 0;
                DataType = null;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">The name of the pair</param>
            /// <param name="unit">The value of the pair</param>
            public MetColumn(string name, string unit)
            {
                Name = name;
                Unit = unit;
                Width = Math.Max(name.Length, unit.Length + 2);
                DecimalPlaces = 0;
                
                string nameLowered = name.ToLower();
                if (nameLowered == "day" || nameLowered == "year" || nameLowered == "date")
                    DataType = typeof(DateTime);
            }

            /// <summary>
            /// Returns the character width of the column. Returns 
            /// MIN_COLUMN_WIDTH as a minimum width is the column is smaller.
            /// </summary>
            public int GetFormattingWidth()
            {
                if (IsFirstColumn)
                    return Width;
                else
                    return Math.Max(MIN_COLUMN_WIDTH, Width);
            }

            /// <summary>
            /// Pass a value and update the column width if the value is longer 
            /// than the width that is stored.
            /// </summary>
            public void UpdateWidth(string value)
            {
                if (value.Length > Width)
                    Width = value.Length;
            }

            /// <summary>
            /// Pass a value and update the column type if that value does not 
            /// fit into that type. 
            /// </summary>
            public void UpdateType(string value)
            {
                if (DataType == typeof(DateTime))
                    return;

                if (DataType == null)
                    DataType = typeof(double);

                if (DataType == typeof(double))
                {
                    if (double.TryParse(value, out double number))
                        DataType = typeof(double);
                    else
                        DataType = typeof(string);
                }

                if (DataType == typeof(double) && value.Contains('.'))
                {
                    string decimals = value.Substring(value.LastIndexOf('.') + 1);
                    if (decimals.Length > DecimalPlaces)
                        DecimalPlaces = decimals.Length;
                    if (DecimalPlaces >= 16)
                        DecimalPlaces = 15; // cap at 15 because Math.Round cant beyond that
                }
            }
        }

        /// <summary>
        /// MetRow stores all the information needed for a met file daily data 
        /// row. This includes both a double and string representation of the 
        /// value in a data cell, along with the date recorded for that row.
        /// 
        /// It also can store a comment that is appended to the end of the row 
        /// when written into a text met file.
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
            ///
            /// </summary>
            public string Comment { get; set; }

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetRow()
            {
                Inputs = new List<string>();
                Date = DateTime.MinValue;
                Values = new List<double>();
                Comment = "";
            }
        }

        /// <summary>
        /// An internal data structure for holding everything about a metfile
        /// This class should not be made public as it just holds data needed 
        /// for converting in and out of file versions.
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
            public List<MetRow> Rows;

            /// <summary>
            /// 
            /// </summary>
            public DateTime StartDate = DateTime.MinValue;

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public MetData()
            {
                Contants = new List<MetConstant>();
                Columns = new List<MetColumn>();
                Rows = new List<MetRow>();
            }
        }

        /// <summary>
        /// </summary>
        private class WeatherZip
        {
            /// <summary>
            /// 
            /// </summary>
            public byte[] bytes;

            /// <summary>
            /// 
            /// </summary>
            public MetFileFormat format;

            /// <summary>
            /// Basic Constructor
            /// </summary>
            public WeatherZip()
            {
                bytes = new byte[0];
                format = MetFileFormat.Text;
            }

            /// <summary>
            /// 
            /// </summary>
            public WeatherZip(byte[] bytes, MetFileFormat format)
            {
                this.bytes = bytes;
                this.format = format;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Legacy Read/Write Functions for deprecated versions
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Binary Writing version 1. 
        /// Deprecated by V2.
        /// Kept for archivial purposes in case a version 1 file needs to be 
        /// written again.
        /// </summary>
        /// <param name="metData"></param>
        /// <returns></returns>
        private static HexData WriteBinaryV1(MetData metData)
        {
            //make copies of these inputs so we can edit them as needed
            List<MetConstant> constants = metData.Contants.ToList();

            //add start date to constants
            constants.Add(new MetConstant("start_date", metData.Rows[0].Date.ToString("yyyy-MM-dd")));

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
            output.Append(UIntToHex(constantCount, 2));
            output.Append(constantText);

            //headers
            //titles
            MetColumn[] columns = datelessMetData.Columns.ToArray();

            output.Append(UIntToHex(columns.Length, 2));
            foreach(MetColumn column in columns)
                output.Append(StringToHex(column.Name));
            foreach(MetColumn column in columns)
                output.Append(StringToHex(column.Unit));

            //data
            output.Append(UIntToHex(datelessMetData.Rows.Count, 8));
            foreach(MetRow row in datelessMetData.Rows)
                foreach(double value in row.Values)
                    output.Append(EncodeNumber(value.ToString()));

            //if output is an odd length
            if (output.Length % 2 == 1) 
                output.Append("0");

            HexData data = new HexData(output.ToString(), 0);
            return data;
        }

        /// <summary>
        /// Binary reading version 1.
        /// Used to read binary files created with the first version of this 
        /// code.
        ///</summary>
        private static MetData ReadBinaryV1(HexData data)
        {
            MetData metData = new MetData();

            //read version
            string version = HexToString(data);

            //read constants
            int constantsLength = HexToUInt(data, 2);

            string startDate = "";

            for (int i = 0; i < constantsLength; i++)
            {
                string name = HexToString(data);
                string value = HexToString(data);

                //start date isn't an actual constant, but is stored as a constant to save space
                if (name == "start_date")
                    startDate = value;
                else
                    metData.Contants.Add(new MetConstant(name, value));
            }

            //headers
            metData.Columns.Add(new MetColumn("date", ""));

            int columnsLength = HexToUInt(data, 2);
            for (int i = 0; i < columnsLength; i++)
                metData.Columns.Add(new MetColumn(HexToString(data), ""));

            for (int i = 1; i < columnsLength + 1; i++)
            {
                string unit = HexToString(data);
                if (unit.StartsWith('('))
                    unit = unit.Substring(1);
                if (unit.EndsWith(')'))
                    unit = unit.Substring(0, unit.Length-1);
                metData.Columns[i].Unit = unit;
            }

            //data
            DateTime date;
            if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new Exception($"Cannot read met file. Start date is {startDate} which must be in yyyy-MM-dd format");

            metData.StartDate = date;

            //create rows and add date column to them
            int rowsLength = HexToUInt(data, 8);
            for (int i = 0; i < rowsLength; i++)
            {
                MetRow row = new MetRow();
                row.Date = date;
                string text = date.ToString("yyyy-MM-dd");
                row.Inputs.Add(text);
                row.Values.Add(double.NaN);
                date = date.AddDays(1);
                metData.Rows.Add(row);
                metData.Columns[0].UpdateWidth(text);
            }

            foreach(MetRow row in metData.Rows)
            {
                for (int i = 1; i < columnsLength + 1; i++)
                {
                    string number = DecodeNumber(data);
                    //the original writer had a bug where it would store a 0 infront of the nan value, causing columns
                    //unalign with the data
                    if (number.Length == 0)
                    {
                        DecodeNumber(data);
                        row.Values.Add(double.NaN);
                        row.Inputs.Add("nan");
                        metData.Columns[i].UpdateType("nan");
                        metData.Columns[i].UpdateWidth("nan");
                    }
                    else
                    {
                        double value = double.Parse(number);
                        row.Values.Add(value);
                        row.Inputs.Add(value.ToString());
                        metData.Columns[i].UpdateType(value.ToString());
                        metData.Columns[i].UpdateWidth(value.ToString());
                    }
                }
            }

            return metData;
        }

        /// <summary>
        /// Binary reading version 1.
        /// Faster version
        ///</summary>
        private static MetData ReadBinaryV1Fast(HexData data)
        {
            MetData metData = new MetData();

            //read version
            string version = HexToString(data);

            //read constants
            int constantsLength = HexToUInt(data, 2);

            string startDate = "";

            for (int i = 0; i < constantsLength; i++)
            {
                string name = HexToString(data);
                string value = HexToString(data);

                //start date isn't an actual constant, but is stored as a constant to save space
                if (name == "start_date")
                    startDate = value;
                else
                    metData.Contants.Add(new MetConstant(name, value));
            }

            //headers
            metData.Columns.Add(new MetColumn("date", ""));

            int columnsLength = HexToUInt(data, 2);
            for (int i = 0; i < columnsLength; i++)
                metData.Columns.Add(new MetColumn(HexToString(data), ""));

            for (int i = 1; i < columnsLength + 1; i++)
                metData.Columns[i].Unit = HexToString(data);

            //data
            DateTime date;
            if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new Exception($"Cannot read met file. Start date is {startDate} which must be in yyyy-MM-dd format");

            metData.StartDate = date;

            //create rows and add date column to them
            int rowsLength = HexToUInt(data, 8);
            for (int i = 0; i < rowsLength; i++)
            {
                MetRow row = new MetRow();
                row.Date = date;
                row.Values.Add(double.NaN);
                date = date.AddDays(1);
                metData.Rows.Add(row);
            }

            foreach(MetRow row in metData.Rows)
            {
                for (int i = 1; i < columnsLength + 1; i++)
                {
                    string number = DecodeNumber(data);
                    //the original writer had a bug where it would store a 0 infront of the nan value, causing columns
                    //unalign with the data
                    if (number.Length == 0)
                    {
                        DecodeNumber(data);
                        row.Values.Add(double.NaN);
                    }
                    else
                    {
                        double value = double.Parse(number);
                        row.Values.Add(value);
                    }
                }
            }

            return metData;
        }

    }
}