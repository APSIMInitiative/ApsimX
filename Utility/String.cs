// -----------------------------------------------------------------------
// <copyright file="String.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Static functions for string manipulation
    /// </summary>
    public class String
    {
        /// <summary>
        /// This function converts a C string to a vb string by returning everything 
        /// up to the null character 
        /// </summary>
        /// <param name="Cstring"></param>
        /// <returns></returns>
        public static string CStringToVBString(string Cstring)
        {
            string result;
            try
            {
                char NullChar = new char();
                result = Cstring.Substring(0, Cstring.IndexOf(NullChar));
            }
            catch (System.Exception)
            {
                throw new Exception("Error converting string type from CS to VB: " + Cstring);
            }

            return result;
        }

        /// <summary>
        /// A version of IndexOf that is case insensitive. 
        /// </summary>
        public static int IndexOfCaseInsensitive(string[] Values, string St)
        {
            string StLower = St.ToLower();
            for (int i = 0; (i <= (Values.Length - 1)); i++)
            {
                if ((Values[i].ToLower() == StLower))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// A version of IndexOf that is case insensitive. 
        /// </summary>
        public static int IndexOfCaseInsensitive(StringCollection Values, string St)
        {
            string StLower = St.ToLower();
            for (int i = 0; (i <= (Values.Count - 1)); i++)
            {
                if ((Values[i].ToLower() == StLower))
                {
                    return i;
                }
            }
            return -1;

        }

        /// <summary>
        /// A version of IndexOf that is case insensitive. 
        /// </summary> 
        public static int IndexOfCaseInsensitive(List<string> Values, string St)
        {
            string StLower = St.ToLower();
            for (int i = 0; (i <= (Values.Count - 1)); i++)
            {
                if ((Values[i].ToLower() == StLower))
                {
                    return i;
                }
            }
            return -1;

        }

        /// <summary>
        /// A version of Array.Contains that is case insensitive. 
        /// </summary>
        public static bool Contains(IEnumerable Values, string St)
        {
            foreach (string Value in Values)
                if (Value.Equals(St, StringComparison.CurrentCultureIgnoreCase))
                    return true; 
            return false;
        }
        /// <summary>
        /// This method complements the string function IndexOfAny by
        /// providing a NOT version. Returns -1 if non of the specified
        /// characters are found in specified string.
        /// </summary>
        public static int IndexNotOfAny(string Text, char[] Delimiters)
        {
            return IndexNotOfAny(Text, Delimiters, 0);
        }

        /// <summary>
        /// This method complements the string function IndexOfAny by
        /// providing a NOT version. Returns -1 if non of the specified
        /// characters are found in specified string.
        /// </summary>
        public static int IndexNotOfAny(string Text, char[] Delimiters, int Pos)
        {
            string DelimitersString = new string(Delimiters);
            for (int i = Pos; i < Text.Length; i++)
            {
                if (DelimitersString.IndexOf(Text[i]) == -1)
                    return i;
            }
            return -1;
        }
 
        /// <summary>
        /// This method splits values on a comma but also honours double quotes
        /// ensuring something in double quotes is never split.
        ///     eg: if text = value1, "value 2, 2a", value3
        ///     then: words[0] = value1
        ///           words[1] = value2, 2a
        ///           words[2] = value3
        /// All values returned have been trimmed of spaces and double quotes.
        /// </summary>
        public static StringCollection SplitStringHonouringQuotes(string Text, string Delimiters)
        {
            StringCollection ReturnStrings = new StringCollection();
            if (Text.Trim() == "")
                return ReturnStrings;

            bool InsideQuotes = false;
            int Start = IndexNotOfAny(Text, " ".ToCharArray());
            for (int i = Start; i < Text.Length; i++)
            {
                if (Text[i] == '"')
                    InsideQuotes = !InsideQuotes; // toggle

                else if (!InsideQuotes)
                {
                    if (Delimiters.IndexOf(Text[i]) != -1)
                    {
                        // Found a word - store it.
                        if (Start != i)
                            ReturnStrings.Add(Text.Substring(Start, i - Start).Trim(" ".ToCharArray()));
                        Start = i+1;

                    }
                }
            }
            if (Start != Text.Length)
                ReturnStrings.Add(Text.Substring(Start, Text.Length - Start).Trim(" ".ToCharArray()));

            // remove leading and trailing quote if necessary.
            for (int i = 0; i < ReturnStrings.Count; i++)
            {
                if (ReturnStrings[i][0] == '"' && ReturnStrings[i][ReturnStrings[i].Length - 1] == '"')
                {
                    ReturnStrings[i] = ReturnStrings[i].Substring(1, ReturnStrings[i].Length - 2).Trim();
                    if (ReturnStrings[i] == "")
                    {
                        ReturnStrings.RemoveAt(i);
                        i--;
                    }
                }
            }
            return ReturnStrings;
        }

        /// <summary>
        /// Split the specified Text into bits. Bits are separated by delimiter characters but
        /// brackets must be honoured. Example Text given Delimiter='.':
        ///     Organs[AboveGround].Live.Wt   
        ///         Bits[0] = Organs[AboveGround]  
        ///         Bits[1]=Live   
        ///         Bits[2]=Wt
        ///     Leaf.Leaves[Leaf.CurrentRank].CoverAbove
        ///         Bits[0]=Leaf
        ///         Bits[1]=Leaves[Leaf.CurrentRank]
        ///         Bits[2]=CoverAbove
        /// </summary>
        public static string[] SplitStringHonouringBrackets(string Text, char Delimiter, char OpenBracket, char CloseBracket)
        {
            List<string> ReturnStrings = new List<string>();
            if (Text.Trim() == "")
                return ReturnStrings.ToArray();
            //if no delimiters in the string then return the original
            if (!Text.Contains("."))
            {
                ReturnStrings.Add(Text.Trim());
                return ReturnStrings.ToArray();
            }

            bool InsideBracket = false;
            int Start = IndexNotOfAny(Text, Delimiter.ToString().ToCharArray());
            for (int i = Start; i < Text.Length; i++)
            {
                if (Text[i] == OpenBracket)
                    InsideBracket = true; // toggle
                else if (Text[i] == CloseBracket)
                    InsideBracket = false;
                else if (!InsideBracket)
                {
                    if (Text[i] == Delimiter)
                    {
                        // Found a word - store it.
                        if (Start != i)
                            ReturnStrings.Add(Text.Substring(Start, i - Start).Trim());
                        Start = i + 1;
                    }
                }
            }
            if (Start != Text.Length)
                ReturnStrings.Add(Text.Substring(Start, Text.Length - Start).Trim());

            return ReturnStrings.ToArray();
        }

        /// <summary>
        /// Returns true if the 2 specified strings are equal
        /// </summary>
        /// <param name="St1"></param>
        /// <param name="St2"></param>
        /// <returns></returns>
        public static bool StringsAreEqual(string St1, string St2)
        {
            return St1.ToLower() == St2.ToLower();
        }

        /// <summary>
        /// Remove, and return everything after the specified
        /// delimiter from the specified string. 
        /// </summary>
        public static string SplitOffAfterDelimiter(ref string St, string Delimiter)
        {
            string ReturnString = "";
            int PosDelimiter = St.IndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                ReturnString = St.Substring(PosDelimiter + Delimiter.Length).Trim();
                St = St.Remove(PosDelimiter, St.Length - PosDelimiter);
            }
            return ReturnString;
        }

        /// <summary>
        /// Split off a bracketed value from the end of the specified string.
        /// The bracketed value is then returned, without the brackets,
        /// or blank if not found.
        /// </summary>
        public static string SplitOffBracketedValue(ref string St, char OpenBracket, char CloseBracket)
        {
            string ReturnString = "";

            int PosOpenBracket = St.LastIndexOf(OpenBracket);
            if (PosOpenBracket != -1)
            {
            int PosCloseBracket = St.LastIndexOf(CloseBracket);
                if (PosCloseBracket != -1 && PosOpenBracket < PosCloseBracket)
            {
                ReturnString = St.Substring(PosOpenBracket + 1, PosCloseBracket - PosOpenBracket - 1).Trim();
                St = St.Remove(PosOpenBracket, PosCloseBracket - PosOpenBracket + 1).Trim();
                }
            }
            return ReturnString;
        }

        /// <summary>
        /// Remove a substring (starting from OpenBracket) from a string.
        /// </summary>
        public static string RemoveAfter(string St, char OpenBracket)
        {
            int Pos = St.IndexOf(OpenBracket);
            if (Pos != -1)
                return St.Substring(0, Pos);
            else
                return St;
        }

        /// <summary>
        /// Return true if specified string is numeric
        /// </summary>
        public static bool IsNumeric(string St)
        {
            float Value;
            return Single.TryParse(St, out Value);
        }

        /// <summary>
        /// Return true if specified string is a date time.
        /// </summary>
        public static bool IsDateTime(string St)
        {
            DateTime Value;
            return DateTime.TryParse(St, out Value);
        }

        /// <summary>
        /// Indent the specified string a certain number of spaces.
        /// </summary>
        public static string IndentText(string St, int numChars)
        {
            if (St == null)
                return St;
            string space = new string(' ', numChars);
            return space + St.Replace("\n", "\n" + space);
        }

        /// <summary>
        /// Indent the specified string a certain number of spaces.
        /// </summary>
        public static string UnIndentText(string St, int numChars)
        {
            if (St.Length < numChars)
                return St;
            string returnString = St.Remove(0, numChars);

            string space = "\r\n" + new string(' ', numChars);
            return returnString.Replace(space, "\r\n");
        }

        /// <summary>
        /// Return a string with double quotes around St
        /// </summary>
        /// <param name="St"></param>
        /// <returns></returns>
        public static string DQuote(string St)
        {
            if (St.IndexOfAny(new char[] { ' ', '&', '<', '>', '(', ')', 
                                           '[', ']', '{', '}', '=', ';', 
                                           '!', '\'', '+', ',', '^', 
                                           '`', '~', '|','@' }) >= 0)
              return "\"" + St + "\"";
            return St;
        }

        /// <summary>
        /// Return a type for the specified string
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Units"></param>
        /// <returns></returns>
        public static Type DetermineType(string Value, string Units)
        {
            Type ColumnType;
            if (Value == "?")
                ColumnType = Type.GetType("System.String");

            else if (Math.IsNumericalenUS(Value))
                ColumnType = Type.GetType("System.Single");

            else if (Units == "" && Utility.String.IsDateTime(Value))
                ColumnType = Type.GetType("System.DateTime");

            else if ((Units.Contains("d") && Units.Contains("/") && Units.Contains("y"))
                      || Utility.String.IsDateTime(Value))
                ColumnType = Type.GetType("System.DateTime");

            else
                ColumnType = Type.GetType("System.String");

            return ColumnType;
        }

        /// <summary>
        /// Create a string array containing the specified number of values.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="NumValues"></param>
        /// <returns></returns>
        public static string[] CreateStringArray(string Value, int NumValues)
        {
            string[] Arr = new string[NumValues];
            for (int i = 0; i < NumValues; i++)
                Arr[i] = Value;
            return Arr;
        }

        /// <summary>
        /// Find the matching closing bracket.
        /// </summary>
        /// <param name="Contents"></param>
        /// <param name="StartPos"></param>
        /// <param name="OpenBracket"></param>
        /// <param name="CloseBracket"></param>
        /// <returns></returns>
        public static int FindMatchingClosingBracket(string Contents, int StartPos, char OpenBracket, char CloseBracket)
        {
            char[] CharSet = new char[2] { OpenBracket, CloseBracket };
            int Pos = Contents.IndexOfAny(CharSet, StartPos);

            int Count = 0;
            while (Pos != -1)
            {
                if (Contents[Pos] == OpenBracket)
                    Count++;
                else
                    Count--;
                if (Count == 0)
                    return Pos;

                Pos = Contents.IndexOfAny(CharSet, Pos + 1);
            }

            return -1;
        }

        /// <summary>
        /// Convert a FORTRAN type APSIM name e.g. canopy_water_balance into a camel case name
        /// like CanopyWaterBalance
        /// </summary>
        public static string CamelCase(string Name)
        {
            int PosUnderScore = -1;
            do
            {
                string UpperChar = Name[PosUnderScore + 1].ToString();
                UpperChar = UpperChar.ToUpper();
                Name = Name.Remove(PosUnderScore + 1, 1);
                Name = Name.Insert(PosUnderScore + 1, UpperChar);

                if (PosUnderScore != -1)
                    Name = Name.Remove(PosUnderScore, 1);
                PosUnderScore = Name.IndexOf('_');
            }
            while (PosUnderScore != -1);
            return Name;
        }

        /// <summary>
        /// A helper function for getting the parent name from the specified
        /// fully qualified name passed in. Assumes delimiter of '.'.
        /// e.g. if Name = Paddock.ModelB
        ///      then returns Paddock
        /// </summary>
        public static string ParentName(string Name, char Delimiter = '.')
        {
            string ReturnName = "";
            if (Name.Length > 0)
            {
                int PosLastPeriod = Name.LastIndexOf(Delimiter);
                if (PosLastPeriod >= 0)
                    ReturnName = Name.Substring(0, PosLastPeriod);
            }
            return ReturnName;
        }

        /// <summary>
        /// A helper function for getting the child name from the specified
        /// fully qualified name passed in. Assumes delimiter of '.'.
        /// e.g. if Name = Paddock.ModelB
        ///      then returns ModelB
        /// </summary>
        public static string ChildName(string Name, char Delimiter = '.')
        {
            string ReturnName = "";
            if (Name.Length > 0)
            {
                int PosLastPeriod = Name.LastIndexOf(Delimiter);
                if (PosLastPeriod >= 0)
                    ReturnName = Name.Substring(PosLastPeriod+1);
            }
            return ReturnName;
        }

        /// <summary>
        /// A helper function for building a string from an array of values.
        /// Format specifies the level of precision written e.g. "f2"
        /// </summary>
        public static string BuildString(double[] Values, string Format)
        {
            string ReturnString = "";
            foreach (double Value in Values)
                ReturnString += "   " + Value.ToString(Format);
            return ReturnString;
        }

        /// <summary>
        /// A helper function for building a string from an array of strings.
        /// Separator is inserted between each string.
        /// </summary>
        public static string BuildString(string[] Values, string separator)
        {
            if (Values == null)
                return "";
            string ReturnString = "";
            for (int i = 0; i < Values.Length; i++)
            {
                if (i > 0)
                    ReturnString += separator;
                ReturnString += Values[i];
            }
            return ReturnString;
        }

        /// <summary>
        /// Build a string for a series of values
        /// </summary>
        /// <param name="values">The values to use to construct the string</param>
        /// <param name="delimiter">The delimiter to use between the strings</param>
        /// <param name="prefix">The prefix string to put in front of each string - can be null for no prefix</param>
        /// <param name="suffix">The suffix string to put in after each string - can be null for no suffix</param>
        /// <param name="format">The format string to use to format the value e.g. N2 - can be null for no format</param>
        /// <returns>The return string</returns>
        public static string Build(IEnumerable values, string delimiter, string prefix = null, string suffix = null, string format = null)
        {
            string returnString = string.Empty;
            foreach (object value in values)
            {
                // Add in delimiter
                if (returnString != string.Empty)
                {
                    returnString += delimiter;
                }

                // Add prefix
                if (prefix != null)
                {
                    returnString += prefix;
                }

                // Add value
                if (format == null)
                {
                    returnString += value.ToString();
                }
                else
                {
                    returnString += string.Format("{0:" + format + "}", value);
                }

                // Add suffix
                if (prefix != null)
                {
                    returnString += suffix;
                }                
            }

            return returnString;
        }

        /// <summary>
        /// Look through the specified string for an environment variable name surrounded by
        /// % characters. Replace them with the environment variable value.
        /// </summary>
        public static string ReplaceEnvironmentVariables(string CommandLine)
        {
            if (CommandLine == null)
                return CommandLine;

            int PosPercent = CommandLine.IndexOf('%');
            while (PosPercent != -1)
            {
                string Value = null;
                int EndVariablePercent = CommandLine.IndexOf('%', PosPercent + 1);
                if (EndVariablePercent != -1)
                {
                    string VariableName = CommandLine.Substring(PosPercent + 1, EndVariablePercent - PosPercent - 1);
                    Value = System.Environment.GetEnvironmentVariable(VariableName);
                    if (Value == null)
                        Value = System.Environment.GetEnvironmentVariable(VariableName, EnvironmentVariableTarget.User);
                }

                if (Value != null)
                {
                    CommandLine = CommandLine.Remove(PosPercent, EndVariablePercent - PosPercent + 1);
                    CommandLine = CommandLine.Insert(PosPercent, Value);
                    PosPercent = PosPercent + 1;
                }

                else
                    PosPercent = PosPercent + 1;

                if (PosPercent >= CommandLine.Length)
                    PosPercent = -1;
                else
                    PosPercent = CommandLine.IndexOf('%', PosPercent);
            }
            return CommandLine;
        }

        /// <summary>
        /// Store all macros found in the command line arguments. Macros are keyword = value
        /// </summary>
        public static Dictionary<string, string> ParseCommandLine(string[] args)
        {
            Dictionary<string, string> Options = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                StringCollection MacroBits = Utility.String.SplitStringHonouringQuotes(args[i], "=");
                if (MacroBits.Count == 2)
                    Options.Add(MacroBits[0].Replace("\"", ""), MacroBits[1].Replace("\"", ""));
            }
            return Options;
        }

        /// <summary>
        /// Convert the specified enum to a list of strings.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string[] EnumToStrings(object obj)
        {
            List<string> items = new List<string>();
            foreach (object e in obj.GetType().GetEnumValues())
            {
                items.Add(e.ToString());
            }

            return items.ToArray();
        }


        /// The following set of routines is taken from the old CPI 
        /// StdStrng.pas unit.
        /// Token-handling routines for use in parsing.  A token is either:
        ///   * a string made up of alphanumeric characters and/or the underscore
        ///   * any string enclosed in double quotes (the quotes are stripped) 
        ///   * <=, >= or /=  (used as relational operators)
        ///   * a punctuation mark (other than double quotes) 
        // Token handling is case-insensitive.

        private static char[] whitespace = { '\t', '\r', '\n', ' ' };
        private static char[] punctuation = { ',', ';', ':', '(', ')', '[', ']', '{', '}', '<', '>', '=', '+', '-', '*', '/', '\\', '&', '^', '~', '%' };

        /// <summary>
        /// TextToken strips the first token from a string.
        ///</summary>
        ///<param name="inSt">
        /// String from which a token is to be taken.  It is returned as the
        /// remaining part of the input value (including any leading whitespace).
        ///</param>
        ///<param name="token">
        /// Returned as the token which has been taken from InSt.  If InSt is
        /// null or entirely whitespace, then Token will be the null string.
        ///</param>
        ///<param name="bRetainCase">
        /// If true, case is unchanged, otherwise the "returned" token is converted
        /// to uppercase.
        ///</param>
        public static void TextToken(ref string inSt, out string token, bool bRetainCase = false)
        {
            int breakPos = 0;
            token = "";
            inSt = inSt.TrimStart(whitespace); //  Start by clearing any whitespace at the beginning of inSt
            if (inSt.Length > 0)               // A Null string will return a null token   
            {
                if (inSt[0] == '"')            // Quoted token
                {
                    inSt = inSt.Remove(0, 1);
                    int idx = inSt.IndexOf('"');
                    if (idx < 0)
                        breakPos = inSt.Length;
                    else
                    {
                        breakPos = idx;
                        inSt = inSt.Remove(idx, 1);
                    }
                }
                else if ((inSt.Length >= 2) && (inSt[1] == '=') &&     // ">=", "<=", and "/=" are treated as tokens in their own right
                    (inSt[0] == '>' || inSt[0] == '<' || inSt[0] == '/'))
                    breakPos = 2;
                else if (inSt.IndexOfAny(punctuation, 0, 1) == 0)      // Other punctuation marks are taken as tokens
                    breakPos = 1;
                else                                                   // With anything else, go looking for whitespace as a break point
                {
                    int idxWhite = inSt.IndexOfAny(whitespace);
                    int idxPunc = inSt.IndexOfAny(punctuation);
                    if (idxWhite > 0)
                    {
                        if (idxPunc >= 0)
                            breakPos = System.Math.Min(idxWhite, idxPunc);
                        else
                            breakPos = idxWhite;
                    }
                    else
                    {
                        if (idxPunc >= 0)
                            breakPos = idxPunc;
                        else
                            breakPos = inSt.Length;
                    }
                }
                token = inSt.Substring(0, breakPos);                    // Split the string
                inSt = inSt.Remove(0, breakPos);
                if (!bRetainCase)
                    token = token.ToUpper();                            // Enforce case-insensitivity
            }
        }

        /// <summary>
        /// Function which returns TRUE i.f.f. the first token in a string matches
        /// an input token.  The match is case-insensitive. 
        /// </summary>
        /// <param name="inSt">
        /// String in which to look for Match.  If Match is found, then inSt
        /// will contain the remainder of the string (including any leading
        /// whitespace) on return. If not, inSt is returned unchanged. 
        /// </param>
        /// <param name="match">
        /// Token to be sought.  If Match is not a token, its first token is used instead. 
        /// </param>
        /// <returns>
        /// TRUE i.f.f. the first token in a string match
        /// </returns>
        public static bool MatchToken(ref string inSt, string match)
        {
            string storeSt = inSt;
            string token;
            string matchToken;
            TextToken(ref inSt, out token);
            TextToken(ref match, out matchToken);
            bool result = token == matchToken;
            if (!result)
                inSt = storeSt;
            return result;
        }

        /// <summary>
        /// Take an integer from the front of a string.
        /// </summary>
        /// <param name="inSt">
        /// String from which to take an integer.  If it is found, then inSt
        /// will contain the remainder of the string (including any leading
        /// whitespace).  If not, InSt is returned unchanged.
        /// </param>
        /// <param name="n">
        /// Returns the integer value.  If no integer is found in the string, 
        /// N is undefined.
        /// </param>
        /// <returns>
        /// Returns TRUE i.f.f. an integer was found.
        /// </returns>
        public static bool TokenInt(ref string inSt, ref int n)
        {
            string parseSt = inSt;
            string token = "";
            TextToken(ref parseSt, out token);  // Extract text from the front of ParseSt
            if (token == "-")
            {
                TextToken(ref parseSt, out token);
                token = "-" + token;
            }
            bool result = int.TryParse(token, out n); // Parse the integer
            if (result)
                inSt = parseSt;
            return result;
        }

        /// <summary>
        /// Take a floating-point value from the front of a string.
        ///  Rules are analogous to Token_Int. Exponential notation is dealt with.
        /// </summary>
        /// <param name="inSt">
        /// String from which to take a value.  If it is found, then inSt
        /// will contain the remainder of the string (including any leading
        /// whitespace).  If not, InSt is returned unchanged.
        /// </param>
        /// <param name="x">
        /// Returns the value.  If no value is found in the string, 
        /// x is undefined.
        /// </param>
        /// <returns>
        /// Returns TRUE i.f.f. a value was found.
        /// </returns>
        public static bool TokenFloat(ref string inSt, ref Single x)
        {
            string parseSt = inSt;
            string token = "";
            TextToken(ref parseSt, out token);  // Extract text from the front of ParseSt
            if (token == "-")
            {
                TextToken(ref parseSt, out token);
                token = "-" + token;
            }

            if (token.Length > 0 && token.IndexOf('E') == token.Length - 1)  // Number is in exponential format
            {
                bool dummy = MatchToken(ref parseSt, "+");       
                int exponent = 0;
                if (TokenInt(ref parseSt, ref exponent))
                    token = token + exponent.ToString();  // Add the exponent to token
                else
                    token = "";          // This forces a FALSE return
            }

            bool result = Single.TryParse(token, out x); // Parse the value
            if (result)
                inSt = parseSt;
            return result;
        }

        /// <summary>
        /// Take a double value from the front of a string.
        ///  Rules are analogous to Token_Int. Exponential notation is dealt with.
        /// </summary>
        /// <param name="inSt">
        /// String from which to take a value.  If it is found, then inSt
        /// will contain the remainder of the string (including any leading
        /// whitespace).  If not, InSt is returned unchanged.
        /// </param>
        /// <param name="x">
        /// Returns the value.  If no value is found in the string, 
        /// x is undefined.
        /// </param>
        /// <returns>
        /// Returns TRUE i.f.f. a value was found.
        /// </returns>
        public static bool TokenDouble(ref string inSt, ref double x)
        {
            string parseSt = inSt;
            string token = "";
            TextToken(ref parseSt, out token);  // Extract text from the front of ParseSt
            if (token == "-")
            {
                TextToken(ref parseSt, out token);
                token = "-" + token;
            }

            if (token.Length > 0 && token.IndexOf('E') == token.Length - 1)  // Number is in exponential format
            {
                bool dummy = MatchToken(ref parseSt, "+");
                int exponent = 0;
                if (TokenInt(ref parseSt, ref exponent))
                    token = token + exponent.ToString();  // Add the exponent to token
                else
                    token = "";          // This forces a FALSE return
            }

            bool result = Double.TryParse(token, out x); // Parse the value
            if (result)
                inSt = parseSt;
            return result;
        }
    }
}
