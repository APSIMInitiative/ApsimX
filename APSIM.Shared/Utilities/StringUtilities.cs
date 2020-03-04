namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Static functions for string manipulation
    /// </summary>
    public class StringUtilities
    {
        /// <summary>
        /// This function converts a C string to a vb string by returning everything 
        /// up to the null character 
        /// </summary>
        /// <param name="cstring"></param>
        /// <returns></returns>
        public static string CStringToVBString(string cstring)
        {
            string result;
            try
            {
                char NullChar = new char();
                result = cstring.Substring(0, cstring.IndexOf(NullChar));
            }
            catch (System.Exception)
            {
                throw new Exception("Error converting string type from CS to VB: " + cstring);
            }

            return result;
        }

        /// <summary>
        /// A version of IndexOf that is case insensitive. 
        /// </summary>
        public static int IndexOfCaseInsensitive(string[] values, string st)
        {
            for (int i = 0; (i <= (values.Length - 1)); i++)
            {
                if (String.Equals(values[i], st, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// A version of IndexOf that is case insensitive. 
        /// </summary>
        public static int IndexOfCaseInsensitive(StringCollection values, string st)
        {
            for (int i = 0; (i <= (values.Count - 1)); i++)
            {
                if (String.Equals(values[i], st, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }
            return -1;

        }

        /// <summary>
        /// A version of IndexOf that is case insensitive. 
        /// </summary> 
        public static int IndexOfCaseInsensitive(List<string> values, string st)
        {
            for (int i = 0; (i <= (values.Count - 1)); i++)
            {
                if (String.Equals(values[i], st, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }
            return -1;

        }

        /// <summary>
        /// A version of Array.Contains that is case insensitive. 
        /// </summary>
        public static bool Contains(IEnumerable values, string st)
        {
            foreach (string Value in values)
                if (Value.Equals(st, StringComparison.CurrentCultureIgnoreCase))
                    return true; 
            return false;
        }
        /// <summary>
        /// This method complements the string function IndexOfAny by
        /// providing a NOT version. Returns -1 if non of the specified
        /// characters are found in specified string.
        /// </summary>
        public static int IndexNotOfAny(string text, char[] delimiters)
        {
            return IndexNotOfAny(text, delimiters, 0);
        }

        /// <summary>
        /// This method complements the string function IndexOfAny by
        /// providing a NOT version. Returns -1 if non of the specified
        /// characters are found in specified string.
        /// </summary>
        public static int IndexNotOfAny(string text, char[] delimiters, int pos)
        {
            string DelimitersString = new string(delimiters);
            for (int i = pos; i < text.Length; i++)
            {
                if (DelimitersString.IndexOf(text[i]) == -1)
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
        public static StringCollection SplitStringHonouringQuotes(string text, string delimiters)
        {
            StringCollection ReturnStrings = new StringCollection();
            if (text.Trim() == "")
                return ReturnStrings;

            bool InsideQuotes = false;
            int Start = IndexNotOfAny(text, " ".ToCharArray());
            for (int i = Start; i < text.Length; i++)
            {
                if (text[i] == '"')
                    InsideQuotes = !InsideQuotes; // toggle

                else if (!InsideQuotes)
                {
                    if (delimiters.IndexOf(text[i]) != -1)
                    {
                        // Found a word - store it.
                        if (Start != i)
                            ReturnStrings.Add(text.Substring(Start, i - Start).Trim(" ".ToCharArray()));
                        Start = i+1;

                    }
                }
            }
            if (Start != text.Length)
                ReturnStrings.Add(text.Substring(Start, text.Length - Start).Trim(" ".ToCharArray()));

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
        public static string[] SplitStringHonouringBrackets(string text, char delimiter, char openBracket, char closeBracket)
        {
            List<string> ReturnStrings = new List<string>();
            if (text.Trim() == "")
                return ReturnStrings.ToArray();
            //if no delimiters in the string then return the original
            if (!text.Contains("."))
            {
                ReturnStrings.Add(text.Trim());
                return ReturnStrings.ToArray();
            }

            bool InsideBracket = false;
            int Start = IndexNotOfAny(text, delimiter.ToString().ToCharArray());
            for (int i = Start; i < text.Length; i++)
            {
                if (text[i] == openBracket)
                    InsideBracket = true; // toggle
                else if (text[i] == closeBracket)
                    InsideBracket = false;
                else if (!InsideBracket)
                {
                    if (text[i] == delimiter)
                    {
                        // Found a word - store it.
                        if (Start != i)
                            ReturnStrings.Add(text.Substring(Start, i - Start).Trim());
                        Start = i + 1;
                    }
                }
            }
            if (Start != text.Length)
                ReturnStrings.Add(text.Substring(Start, text.Length - Start).Trim());

            return ReturnStrings.ToArray();
        }

        /// <summary>
        /// Returns true if the 2 specified strings are equal
        /// </summary>
        /// <param name="st1"></param>
        /// <param name="st2"></param>
        /// <returns></returns>
        public static bool StringsAreEqual(string st1, string st2)
        {
            return st1.Equals(st2, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Remove, and return everything after the specified
        /// delimiter from the specified string. 
        /// </summary>
        public static string SplitOffAfterDelimiter(ref string st, string delimiter)
        {
            string ReturnString = "";
            int PosDelimiter = st.IndexOf(delimiter);
            if (PosDelimiter != -1)
            {
                ReturnString = st.Substring(PosDelimiter + delimiter.Length).Trim();
                st = st.Remove(PosDelimiter, st.Length - PosDelimiter);
            }
            return ReturnString;
        }

        /// <summary>
        /// Split off a bracketed value from the end of the specified string.
        /// The bracketed value is then returned, without the brackets,
        /// or blank if not found.
        /// </summary>
        public static string SplitOffBracketedValue(ref string st, char openBracket, char closeBracket)
        {
            string ReturnString = "";

            int PosOpenBracket = st.LastIndexOf(openBracket);
            if (PosOpenBracket != -1)
            {
            int PosCloseBracket = st.LastIndexOf(closeBracket);
                if (PosCloseBracket != -1 && PosOpenBracket < PosCloseBracket)
            {
                ReturnString = st.Substring(PosOpenBracket + 1, PosCloseBracket - PosOpenBracket - 1).Trim();
                st = st.Remove(PosOpenBracket, PosCloseBracket - PosOpenBracket + 1).Trim();
                }
            }
            return ReturnString;
        }

        /// <summary>
        /// Remove a substring (starting from OpenBracket) from a string.
        /// </summary>
        public static string RemoveAfter(string st, char openBracket)
        {
            int Pos = st.IndexOf(openBracket);
            if (Pos != -1)
                return st.Substring(0, Pos);
            else
                return st;
        }

        /// <summary>
        /// Return a substring after the delimiter
        /// </summary>
        public static string GetAfter(string st, string delimiter)
        {
            int Pos = st.IndexOf(delimiter);
            if (Pos != -1)
                return st.Substring(Pos + delimiter.Length);
            else
                return st;
        }

        /// <summary>
        /// Return true if specified string is numeric
        /// </summary>
        public static bool IsNumeric(string st)
        {
            float Value;
            return Single.TryParse(st, out Value);
        }

        /// <summary>
        /// Return true if specified string is a date time.
        /// </summary>
        public static bool IsDateTime(string st)
        {
            DateTime Value;
            return DateTime.TryParse(st, out Value);
        }

        /// <summary>
        /// Indent the specified string a certain number of spaces.
        /// </summary>
        public static string IndentText(string st, int numChars)
        {
            if (st == null)
                return st;
            string space = new string(' ', numChars);
            return space + st.Replace("\n", "\n" + space);
        }

        /// <summary>
        /// Indent the specified string a certain number of spaces.
        /// </summary>
        public static string UnIndentText(string st, int numChars)
        {
            if (st.Length < numChars)
                return st;
            string returnString = st.Remove(0, numChars);

            string space = "\r\n" + new string(' ', numChars);
            return returnString.Replace(space, "\r\n");
        }

        /// <summary>
        /// Return a string with double quotes around St
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static string DQuote(string st)
        {
            if (st.IndexOfAny(new char[] { ' ', '&', '<', '>', '(', ')', 
                                           '[', ']', '{', '}', '=', ';', 
                                           '!', '\'', '+', ',', '^', 
                                           '`', '~', '|','@' }) >= 0)
              return "\"" + st + "\"";
            return st;
        }

        /// <summary>
        /// Return a type for the specified string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        public static Type DetermineType(string value, string units)
        {
            Type ColumnType;
            if (value == "?")
                ColumnType = Type.GetType("System.String");

            else if (MathUtilities.IsNumericalenUS(value))
                ColumnType = Type.GetType("System.Single");

            else if ((units == "" || units == "()") && StringUtilities.IsDateTime(value))
                ColumnType = Type.GetType("System.DateTime");

            else if ((units.Contains("d") && units.Contains("/") && units.Contains("y"))
                      || StringUtilities.IsDateTime(value))
                ColumnType = Type.GetType("System.DateTime");

            else
                ColumnType = Type.GetType("System.String");

            return ColumnType;
        }

        /// <summary>
        /// Create a string array containing the specified number of values.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="numValues"></param>
        /// <returns></returns>
        public static string[] CreateStringArray(string value, int numValues)
        {
            string[] Arr = new string[numValues];
            for (int i = 0; i < numValues; i++)
                Arr[i] = value;
            return Arr;
        }

        /// <summary>
        /// Find the matching closing bracket.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="startPos"></param>
        /// <param name="openBracket"></param>
        /// <param name="closeBracket"></param>
        /// <returns></returns>
        public static int FindMatchingClosingBracket(string contents, int startPos, char openBracket, char closeBracket)
        {
            char[] CharSet = new char[2] { openBracket, closeBracket };
            int Pos = contents.IndexOfAny(CharSet, startPos);

            int Count = 0;
            while (Pos != -1)
            {
                if (contents[Pos] == openBracket)
                    Count++;
                else
                    Count--;
                if (Count == 0)
                    return Pos;

                Pos = contents.IndexOfAny(CharSet, Pos + 1);
            }

            return -1;
        }

        /// <summary>
        /// Convert a FORTRAN type APSIM name e.g. canopy_water_balance into a camel case name
        /// like CanopyWaterBalance
        /// </summary>
        public static string CamelCase(string name)
        {
            int PosUnderScore = -1;
            do
            {
                string UpperChar = name[PosUnderScore + 1].ToString();
                UpperChar = UpperChar.ToUpper();
                name = name.Remove(PosUnderScore + 1, 1);
                name = name.Insert(PosUnderScore + 1, UpperChar);

                if (PosUnderScore != -1)
                    name = name.Remove(PosUnderScore, 1);
                PosUnderScore = name.IndexOf('_');
            }
            while (PosUnderScore != -1);
            return name;
        }

        /// <summary>
        /// A helper function for getting the parent name from the specified
        /// fully qualified name passed in. Assumes delimiter of '.'.
        /// e.g. if Name = Paddock.ModelB
        ///      then returns Paddock
        /// </summary>
        public static string ParentName(string name, char delimiter = '.')
        {
            string ReturnName = "";
            if (name.Length > 0)
            {
                int PosLastPeriod = name.LastIndexOf(delimiter);
                if (PosLastPeriod >= 0)
                    ReturnName = name.Substring(0, PosLastPeriod);
            }
            return ReturnName;
        }

        /// <summary>
        /// A helper function for getting the child name from the specified
        /// fully qualified name passed in. Assumes delimiter of '.'.
        /// e.g. if Name = Paddock.ModelB
        ///      then returns ModelB
        /// </summary>
        public static string ChildName(string name, char delimiter = '.')
        {
            string ReturnName = "";
            if (name.Length > 0)
            {
                int PosLastPeriod = name.LastIndexOf(delimiter);
                if (PosLastPeriod >= 0)
                    ReturnName = name.Substring(PosLastPeriod+1);
            }
            return ReturnName;
        }

        /// <summary>
        /// A helper function for building a string from an array of values.
        /// Format specifies the level of precision written e.g. "f2"
        /// </summary>
        public static string BuildString(IEnumerable<double> values, string format)
        {
            string ReturnString = "";
            foreach (double Value in values)
                ReturnString += "   " + Value.ToString(format);
            return ReturnString;
        }

        /// <summary>
        /// A helper function for building a string from an array of strings.
        /// Separator is inserted between each string.
        /// </summary>
        public static string BuildString<T>(IEnumerable<T> values, string separator)
        {
            if (values == null)
                return "";
            string returnString = string.Empty;
            foreach (var value in values)
            {
                if (returnString != string.Empty)
                    returnString += separator;
                returnString += value;
            }
            return returnString;
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
        public static string ReplaceEnvironmentVariables(string commandLine)
        {
            if (commandLine == null)
                return commandLine;

            int PosPercent = commandLine.IndexOf('%');
            while (PosPercent != -1)
            {
                string Value = null;
                int EndVariablePercent = commandLine.IndexOf('%', PosPercent + 1);
                if (EndVariablePercent != -1)
                {
                    string VariableName = commandLine.Substring(PosPercent + 1, EndVariablePercent - PosPercent - 1);
                    Value = System.Environment.GetEnvironmentVariable(VariableName);
                    if (Value == null)
                        Value = System.Environment.GetEnvironmentVariable(VariableName, EnvironmentVariableTarget.User);
                }

                if (Value != null)
                {
                    commandLine = commandLine.Remove(PosPercent, EndVariablePercent - PosPercent + 1);
                    commandLine = commandLine.Insert(PosPercent, Value);
                    PosPercent = PosPercent + 1;
                }

                else
                    PosPercent = PosPercent + 1;

                if (PosPercent >= commandLine.Length)
                    PosPercent = -1;
                else
                    PosPercent = commandLine.IndexOf('%', PosPercent);
            }
            return commandLine;
        }

        /// <summary>
        /// Store all macros found in the command line arguments. Macros are keyword = value
        /// </summary>
        public static Dictionary<string, string> ParseCommandLine(string[] args)
        {
            Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                StringCollection bits = SplitStringHonouringQuotes(args[i], "=");
                if (bits.Count > 0)
                {
                    string name = bits[0].Replace("\"", "");
                    string value = null;
                    if (bits.Count > 1)
                        value = bits[1].Replace("\"", "");
                    options.Add(name, value);
                }
            }
            return options;
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

        /// <summary>Counts the number of times that stringToFind exists in text.</summary>
        /// <param name="text">The text.</param>
        /// <param name="stringToFind">The string to find.</param>
        /// <returns>The number of times found.</returns>
        public static int CountSubStrings(string text, string stringToFind)
        {
            int count = 0;
            int pos = 0;
            while ((pos = text.IndexOf(stringToFind, pos)) != -1)
            {
                count++;
                pos += stringToFind.Length;
            }

            return count;
        }

        /// The following set of routines is taken from the old CPI 
        /// StdStrng.pas unit.
        /// Token-handling routines for use in parsing.  A token is either:
        ///   * a string made up of alphanumeric characters and/or the underscore
        ///   * any string enclosed in double quotes (the quotes are stripped) 
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
                MatchToken(ref parseSt, "+");       
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
                MatchToken(ref parseSt, "+");
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
        /// <summary>
        /// Removes string from end of a given string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static string RemoveTrailingString(string s, string remove)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            if (s.EndsWith(remove))
            {
                int pos = s.LastIndexOf(remove);
                return s.Substring(0, pos);
            }
            else
                return s;

        }

        /// <summary>
        /// Prepares a string for display in a Gtk control which makes use of Pango markup
        /// by quoting the ampersand character
        /// </summary>
        /// <param name="s">Input string</param>
        /// <returns>The string with any ampersand characters quoted</returns>
        public static string PangoString(string s)
        {
            return s.Replace("&", "&amp;");
        }

        /// <summary>Remove the end of a string following word and return it.</summary>
        /// <param name="st">The string.</param>
        /// <param name="word">Word to look for.</param>
        /// <returns>The value after the word or null if not found.</returns>
        public static string RemoveWordAfter(ref string st, string word)
        {
            string stringToFind = " " + word + " ";
            int posWord = st.IndexOf(stringToFind);
            if (posWord != -1)
            {
                string value = st.Substring(posWord + stringToFind.Length).Trim();
                st = st.Remove(posWord);
                return value;
            }
            else
                return null;
        }

        /// <summary>Remove the start of a string before the word.</summary>
        /// <param name="st">The string.</param>
        /// <param name="word">Word to look for.</param>
        /// <returns>The value before the word or null if not found.</returns>
        public static string RemoveWordBefore(ref string st, string word)
        {
            string stringToFind = " " + word + " ";
            int posWord = st.IndexOf(stringToFind);
            if (posWord != -1)
            {
                string value = st.Substring(0, posWord).Trim();
                st = st.Remove(0, posWord + stringToFind.Length);
                return value;
            }
            else
                return null;
        }
    }
}
