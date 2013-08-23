using System;
using System.Data;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

using Utility;
using System.Globalization;
using System.Collections.Generic;


// An APSIMInputFile is either a ".met" file or a ".out" file.
// They are both text files that share the same format. 
// These classes are used to read/write these files and create an object instance of them.


namespace Utility
{
    // ---------------------------------------------
    // A simple type for encapsulating a constant
    // ---------------------------------------------
    class ApsimConstant
    {
        public ApsimConstant(string name, string val, string units, string comm)
        {
            Name = name;
            Value = val;
            Units = units;
            Comment = comm;
        }

        public string Name;
        public string Value;
        public string Units;
        public string Comment;
    }

    /// <summary>
    /// This class encapsulates an APSIM input file providing methods for
    /// reading data.
    /// </summary>
    class ApsimTextFile
    {
        private string _FileName;
        public StringCollection Headings;
        public StringCollection Units;
        private ArrayList _Constants = new ArrayList();
        private bool CSV = false;
        private StreamReaderRandomAccess In;
        private DateTime _FirstDate;
        private DateTime _LastDate;
        private int FirstLinePosition;
        private StringCollection Words = new StringCollection();
        private Type[] ColumnTypes;


        /// <summary>
        /// Open the file ready for reading.
        /// </summary>
        public void Open(string FileName)
        {
            if (FileName == "")
                return;

            if (!File.Exists(FileName))
                throw new Exception("Cannot find file: " + FileName);

            _FileName = FileName;
            CSV = System.IO.Path.GetExtension(FileName).ToLower() == ".csv";

            _Constants.Clear();

            In = new StreamReaderRandomAccess(_FileName);
            ReadApsimHeader(In);
            FirstLinePosition = In.Position;

            // Read in first line.
            StringCollection Words = new StringCollection();
            GetNextLine(In, ref Words);
            ColumnTypes = DetermineColumnTypes(In, Words);

            // Get first date.
            object[] Values = ConvertWordsToObjects(Words, ColumnTypes);
            _FirstDate = GetDateFromValues(Values);

            // Now we need to seek to the end of file and find the last full line in the file.
            In.Seek(0, SeekOrigin.End);
            if (In.Position >= 1000 && In.Position - 1000 > FirstLinePosition)
            {
                In.Seek(-1000, SeekOrigin.End);
                In.ReadLine(); // throw away partial line.
            }
            else
                In.Seek(FirstLinePosition, SeekOrigin.Begin);
            while (GetNextLine(In, ref Words))
            { }

            // Get the date from the last line.
            if (Words.Count == 0)
                throw new Exception("Cannot find last row of file: " + FileName);
            Values = ConvertWordsToObjects(Words, ColumnTypes);
            _LastDate = GetDateFromValues(Values);

            In.Seek(FirstLinePosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Close this file.
        /// </summary>
        public void Close()
        {
            In.Close();
        }

        public DateTime FirstDate { get { return _FirstDate; } }
        public DateTime LastDate { get { return _LastDate; } }

        public ArrayList Constants
        {
            get
            {
                return _Constants;
            }
        }
        public ApsimConstant Constant(string ConstantName)
        {
            // -------------------------------------
            // Return a given constant to caller
            // -------------------------------------

            foreach (ApsimConstant c in _Constants)
            {
                if (Utility.String.StringsAreEqual(c.Name, ConstantName))
                {
                    return c;
                }
            }
            return null;
        }
        public void SetConstant(string ConstantName, string ConstantValue)
        {
            // -------------------------------------
            // Set a given constant's value.
            // -------------------------------------

            foreach (ApsimConstant c in _Constants)
            {
                if (Utility.String.StringsAreEqual(c.Name, ConstantName))
                    c.Value = ConstantValue;
            }
        }

        /// <summary>
        /// Convert this file to a DataTable.
        /// </summary>
        public System.Data.DataTable ToTable()
        {
            System.Data.DataTable Data = new System.Data.DataTable();
            Data.TableName = "Data";

            StringCollection Words = new StringCollection();
            bool CheckHeadingsExist = true;
            while (GetNextLine(In, ref Words))
            {
                if (CheckHeadingsExist)
                {
                    for (int w = 0; w != ColumnTypes.Length; w++)
                        Data.Columns.Add(new DataColumn(Headings[w], ColumnTypes[w]));
                }
                DataRow NewMetRow = Data.NewRow();
                object[] Values = ConvertWordsToObjects(Words, ColumnTypes);

                for (int w = 0; w != Words.Count; w++)
                {
                    int TableColumnNumber = NewMetRow.Table.Columns.IndexOf(Headings[w]);
                    NewMetRow[TableColumnNumber] = Values[TableColumnNumber];
                }
                Data.Rows.Add(NewMetRow);
                CheckHeadingsExist = false;
            }
            return Data;
        }
        private void ReadApsimHeaderLines(StreamReaderRandomAccess In,
                                          ref StringCollection ConstantLines,
                                          ref StringCollection HeadingLines)
        {
            string PreviousLine = "";

            string Line = In.ReadLine();
            while (!In.EndOfStream)
            {
                int PosEquals = Line.IndexOf('=');
                if (PosEquals != -1)
                {
                    // constant found.
                    ConstantLines.Add(Line);
                }
                else
                {
                    char[] whitespace = { ' ', '\t' };
                    int PosFirstNonBlankChar = Utility.String.IndexNotOfAny(Line, whitespace);
                    if (PosFirstNonBlankChar != -1 && Line[PosFirstNonBlankChar] == '(')
                    {
                        HeadingLines.Add(PreviousLine);
                        HeadingLines.Add(Line);
                        break;
                    }
                }
                PreviousLine = Line;
                Line = In.ReadLine();
            }

        }

        /// <summary>
        /// Add our constants to every row in the specified table beginning with the specified StartRow.
        /// </summary>
        public void AddConstantsToData(System.Data.DataTable Table)
        {
            foreach (ApsimConstant Constant in Constants)
            {
                if (Table.Columns.IndexOf(Constant.Name) == -1)
                {
                    Type ColumnType = Utility.String.DetermineType(Constant.Value, "");
                    Table.Columns.Add(new DataColumn(Constant.Name, ColumnType));
                }
                for (int Row = 0; Row < Table.Rows.Count; Row++)
                {
                    double Value;
                    if (Double.TryParse(Constant.Value, NumberStyles.Float, new CultureInfo("en-US"), out Value))
                        Table.Rows[Row][Constant.Name] = Value;
                    else
                        Table.Rows[Row][Constant.Name] = Constant.Value;
                }
            }
        }
        
        /// <summary>
        /// Read in the APSIM header - headings/units and constants.
        /// </summary>
        private void ReadApsimHeader(StreamReaderRandomAccess In)
        {
            StringCollection ConstantLines = new StringCollection();
            StringCollection HeadingLines = new StringCollection();
            ReadApsimHeaderLines(In, ref ConstantLines, ref HeadingLines);

            bool TitleFound = false;
            foreach (string ConstantLine in ConstantLines)
            {
                string Line = ConstantLine;
                string Comment = Utility.String.SplitOffAfterDelimiter(ref Line, "!");
                Comment.Trim();
                int PosEquals = Line.IndexOf('=');
                if (PosEquals != -1)
                {
                    string Name = Line.Substring(0, PosEquals).Trim();
                    if (Name.ToLower() == "title")
                    {
                        TitleFound = true;
                        Name = "Title";
                    }
                    string Value = Line.Substring(PosEquals + 1).Trim();
                    string Unit = Utility.String.SplitOffBracketedValue(ref Value, '(', ')');
                    _Constants.Add(new ApsimConstant(Name, Value, Unit, Comment));
                }
            }
            if (HeadingLines.Count >= 2)
            {
                if (CSV)
                {
                    HeadingLines[0] = HeadingLines[0].TrimEnd(',');
                    HeadingLines[1] = HeadingLines[1].TrimEnd(',');
                    Headings = new StringCollection();
                    Units = new StringCollection();
                    Headings.AddRange(HeadingLines[0].Split(",".ToCharArray()));
                    Units.AddRange(HeadingLines[1].Split(",".ToCharArray()));
                    for (int i = 0; i < Headings.Count; i++)
                        Headings[i] = Headings[i].Trim();
                    for (int i = 0; i < Units.Count; i++)
                        Units[i] = Units[i].Trim();
                }
                else
                {
                    Headings = Utility.String.SplitStringHonouringQuotes(HeadingLines[0], " \t");
                    Units = Utility.String.SplitStringHonouringQuotes(HeadingLines[1], " \t");
                }
                TitleFound = TitleFound || Utility.String.IndexOfCaseInsensitive(Headings, "title") != -1;
                if (Headings.Count != Units.Count)
                    throw new Exception("The number of headings and units doesn't match in file: " + _FileName);
            }
            if (!TitleFound)
                _Constants.Add(new ApsimConstant("Title", System.IO.Path.GetFileNameWithoutExtension(_FileName), "", ""));
        }

        /// <summary>
        /// Determine and return the data types of the specfied words.
        /// </summary>
        private Type[] DetermineColumnTypes(StreamReaderRandomAccess In, StringCollection Words)
        {
            Type[] Types = new Type[Words.Count];
            for (int w = 0; w != Words.Count; w++)
            {
                if (Words[w] == "?" || Words[w] == "*" || Words[w] == "")
                    Types[w] = Utility.String.DetermineType(LookAheadForNonMissingValue(In, w), Units[w]);
                else
                    Types[w] = Utility.String.DetermineType(Words[w], Units[w]);
            }
            return Types;
        }

        /// <summary>
        /// Convert the specified words to the specified column types and return their values.
        /// </summary>
        private object[] ConvertWordsToObjects(StringCollection Words, Type[] ColumnTypes)
        {
            object[] Values = new object[Words.Count];
            for (int w = 0; w != Words.Count; w++)
            {
                try
                {
                    Words[w] = Words[w].Trim();
                    if (Words[w] == "?" || Words[w] == "*" || Words[w] == "")
                        Values[w] = DBNull.Value;

                    else if (ColumnTypes[w] == typeof(DateTime))
                    {
                        // Need to get a sanitised date e.g. d/M/yyyy 
                        string DateFormat = Units[w].ToLower();
                        DateFormat = Utility.String.SplitOffBracketedValue(ref DateFormat, '(', ')');
                        DateFormat = DateFormat.Replace("mmm", "MMM");
                        DateFormat = DateFormat.Replace("mm", "m");
                        DateFormat = DateFormat.Replace("dd", "d");
                        DateFormat = DateFormat.Replace("m", "M");
                        if (DateFormat == "")
                            DateFormat = "d/M/yyyy";
                        DateTime Value = DateTime.ParseExact(Words[w], DateFormat, null);
                        Values[w] = Value;
                    }
                    else if (ColumnTypes[w] == typeof(float))
                    {
                        double Value;
                        if (double.TryParse(Words[w], out Value))
                            Values[w] = Value;
                        else
                            Values[w] = DBNull.Value;
                    }
                    else
                        Values[w] = Words[w];
                }
                catch (Exception)
                {
                    Values[w] = DBNull.Value;
                }
            }
            return Values;
        }

        /// <summary>
        /// Return the next line in the file as a collection of words.
        /// </summary>
        private bool GetNextLine(StreamReaderRandomAccess In, ref StringCollection Words)
        {
            if (In.EndOfStream)
                return false;

            string Line = In.ReadLine();

            if (Line == null || Line.Length == 0)
                return false;

            if (Line.IndexOf("!") > 0) //used to ignore "!" in a row
                Line = Line.Substring(0, Line.IndexOf("!") - 1);

            if (CSV)
            {
                Words.Clear();
                Line = Line.TrimEnd(',');
                Words.AddRange(Line.Split(",".ToCharArray()));
            }
            else
                Words = Utility.String.SplitStringHonouringQuotes(Line, " \t");
            if (Words.Count != Headings.Count)
                throw new Exception("Invalid number of values on line: " + Line + "\r\nin file: " + _FileName);

            // Remove leading / trailing double quote chars.
            for (int i = 0; i < Words.Count; i++)
                Words[i] = Words[i].Trim("\"".ToCharArray());
            return true;
        }
        private string LookAheadForNonMissingValue(StreamReaderRandomAccess In, int w)
        {
            if (In.EndOfStream)
                return "?";

            int Pos = In.Position;

            StringCollection Words = new StringCollection();
            while (GetNextLine(In, ref Words) && Words[w] == "?") ;

            In.Position = Pos;

            if (Words.Count > w)
                return Words[w];
            else
                return "?";
        }



        /// <summary>
        /// Return the first date from the specified objects. Will return empty DateTime if not found.
        /// </summary>
        public DateTime GetDateFromValues(object[] Values)
        {
            int Year = 0;
            int Month = 0;
            int Day = 0;
            for (int Col = 0; Col != Values.Length; Col++)
            {
                string ColumnName = Headings[Col];
                if (ColumnName.Equals("date", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (ColumnTypes[Col] == typeof(DateTime))
                        return (DateTime)Values[Col];
                    else
                        return DateTime.Parse(Values[Col].ToString());
                }
                else if (ColumnName.Equals("year", StringComparison.CurrentCultureIgnoreCase))
                    Year = Convert.ToInt32(Values[Col]);
                else if (ColumnName.Equals("month", StringComparison.CurrentCultureIgnoreCase))
                    Month = Convert.ToInt32(Values[Col]);
                else if (ColumnName.Equals("day", StringComparison.CurrentCultureIgnoreCase))
                    Day = Convert.ToInt32(Values[Col]);
            }
            if (Year > 0)
            {
                if (Day > 0)
                    return new DateTime(Year, 1, 1).AddDays(Day - 1);
                else
                    Day = 1;
                if (Month == 0)
                    Month = 1;
                return new DateTime(Year, Month, Day);
            }
            return new DateTime();
        }

        /// <summary>
        /// Seek to the specified date. Will throw if date not found.
        /// </summary>
        /// <param name="Date"></param>
        public void SeekToDate(DateTime Date)
        {
            if (Date < _FirstDate)
                throw new Exception("Date " + Date.ToString() + " doesn't exist in file: " + _FileName);

            int NumRowsToSkip = (Date - _FirstDate).Days;

            In.Seek(FirstLinePosition, SeekOrigin.Begin);
            while (!In.EndOfStream && NumRowsToSkip > 0)
            {
                In.ReadLine();
                NumRowsToSkip--;
            }
            int SavedPosition = In.Position;
            StringCollection Words = new StringCollection();
            if (GetNextLine(In, ref Words))
            {
                // Make sure we found the date.
                object[] Values = ConvertWordsToObjects(Words, ColumnTypes);
                DateTime RowDate = GetDateFromValues(Values);
                if (RowDate != Date)
                    throw new Exception("Non consecutive dates found in file: " + _FileName);
            }
            else
                throw new Exception("End of file reached while trying to find date " + Date.ToShortDateString() +
                                    " in file " + _FileName);

            // All ok - restore position.
            In.Seek(SavedPosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Return the next line of data from the file as an array of objects.
        /// </summary>
        public object[] GetNextLineOfData()
        {
            Words.Clear();

            if (GetNextLine(In, ref Words))
                return ConvertWordsToObjects(Words, ColumnTypes);
            else
                throw new Exception("End of file reached while reading file: " + _FileName);
        }
    }
}
