namespace StdUnits
{
    /// <summary>
    /// String utility functions
    /// </summary>
    public static class StdStrng
    {
        // The following set of routines is taken from the old CPI 
        // StdStrng.pas unit.
        // Token-handling routines for use in parsing.  A token is either:
        //   * a string made up of alphanumeric characters and/or the underscore
        //   * any string enclosed in double quotes (the quotes are stripped) 
        //   * a punctuation mark (other than double quotes) 
        // Token handling is case-insensitive.

        /// <summary>
        /// Whitespace characters
        /// </summary>
        private static char[] whitespace = { '\t', '\r', '\n', ' ' };

        /// <summary>
        /// Punctuation marks
        /// </summary>
        private static char[] punctuation = { ',', ';', ':', '(', ')', '[', ']', '{', '}', '<', '>', '=', '+', '-', '*', '/', '\\', '&', '^', '~', '%' };

        /// <summary>
        /// TextToken strips the first token from a string.
        /// </summary>
        /// <param name="inSt">String from which a token is to be taken.  It is returned as the
        /// remaining part of the input value (including any leading whitespace).
        /// </param>
        /// <param name="token">Returned as the token which has been taken from InSt.  If InSt is
        /// null or entirely whitespace, then Token will be the null string.
        /// </param>
        /// <param name="retainCase">If true, case is unchanged, otherwise the "returned" token is converted
        /// to uppercase.</param>
        public static void TextToken(ref string inSt, out string token, bool retainCase = false)
        {
            int breakPos = 0;
            token = string.Empty;
            inSt = inSt.TrimStart(whitespace); // Start by clearing any whitespace at the beginning of inSt
            // A Null string will return a null token   
            if (inSt.Length > 0)               
            {
                // Quoted token
                if (inSt[0] == '"')            
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
                else                                                   
                {
                    // With anything else, go looking for whitespace as a break point
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
                if (!retainCase)
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
            string token = string.Empty;
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
        public static bool TokenFloat(ref string inSt, ref float x)
        {
            string parseSt = inSt;
            string token = string.Empty;
            TextToken(ref parseSt, out token);  // Extract text from the front of ParseSt
            if (token == "-")
            {
                TextToken(ref parseSt, out token);
                token = "-" + token;
            }

            // The number is in exponential format
            if (token.Length > 0 && token.IndexOf('E') == token.Length - 1)  
            {
                MatchToken(ref parseSt, "+");
                int exponent = 0;
                if (TokenInt(ref parseSt, ref exponent))
                    token = token + exponent.ToString();  // Add the exponent to token
                else
                    token = string.Empty;          // This forces a FALSE return
            }

            bool result = float.TryParse(token, out x); // Parse the value
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
            string token = string.Empty;
            TextToken(ref parseSt, out token);  // Extract text from the front of ParseSt
            if (token == "-")
            {
                TextToken(ref parseSt, out token);
                token = "-" + token;
            }

            // If the number is in exponential format
            if (token.Length > 0 && token.IndexOf('E') == token.Length - 1)  
            {
                MatchToken(ref parseSt, "+");
                int exponent = 0;
                if (TokenInt(ref parseSt, ref exponent))
                    token = token + exponent.ToString();  // Add the exponent to token
                else
                    token = string.Empty;          // This forces a FALSE return
            }

            bool result = double.TryParse(token, out x); // Parse the value
            if (result)
                inSt = parseSt;
            return result;
        }

        /// <summary>
        /// Token_Date follows the same rules for its parameters as Token_Int.  It    
        /// expects day to precede months, and months to precede years, but it can    
        /// cope with D-M-Y and D-M-0 kinds of date.  Token_Date is implemented as a  
        /// state-based parser:                                                       
        /// -----                                                                           
        /// State                     Next token  Token means  Go to state            
        /// -----                     ----------  -----------  -----------            
        /// -1   Error                                                              
        /// 0   Start of parsing   Number       Day of month    1                  
        ///                          else                      -1                  
        /// 1   Past day           '/' or '-'   Delimiter       2                  
        ///                        JAN-DEC      Month           3                  
        ///                          else                       2                  
        /// 2   Numeric month      1-12         Month           3                  
        ///                          else                      -1                  
        /// 3   Past month         '/' or '-'   Delimiter       3                  
        ///                        Number       Year            4                  
        ///                          else       Year=0          4                  
        /// </summary>
        /// <param name="inputStr">Input date string</param>
        /// <param name="dateValue">Returned date</param>
        /// <returns>True is parsed ok</returns>
        public static bool TokenDate(ref string inputStr, ref int dateValue)
        {
            bool result;

            // We need to have these in upper case as that is how TextToken returns them     
            string[] monthTexts = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

            int state = 0;
            int day = 0;
            int mn = 0;
            int yr = 0;
            while ((state != -1) && (state != 4))
            {
                switch (state)
                {
                    case 0:
                        if (TokenInt(ref inputStr, ref day) && (day >= 1) && (day <= 31))               // state 0:  we expect a number for the day 
                            state = 1;
                        else
                            state = -1;
                        break;
                    case 1: if (MatchToken(ref inputStr, "/") || MatchToken(ref inputStr, "-"))           // State 1: Day found, looking for a        
                            state = 2;                                                              // delimiter or a month text              
                        else
                        {
                            mn = 1;
                            while ((mn <= 12) && (!MatchToken(ref inputStr, monthTexts[mn])))
                                mn++;
                            if (mn <= 12)
                                state = 3;
                            else
                                state = 2;
                        }
                        break;
                    case 2: if (TokenInt(ref inputStr, ref mn) && (mn >= 1) && (mn <= 12))              // State 2: Day/month delimiter found, so   
                            state = 3;                                                              // looking for a numeric month            
                        else
                            state = -1;
                        break;
                    case 3: if (MatchToken(ref inputStr, "/") || MatchToken(ref inputStr, "-"))           // State 3:  month has been found.  Clear   
                            state = 3;                                                              // away any delimiter and then look for a 
                        else if (TokenInt(ref inputStr, ref yr) && (yr >= 1))                           // year                                  
                        {
                            if (yr < 100)
                                yr = 1900 + yr;
                            state = 4;                                                              // state=4 is the exit point                
                        }
                        else
                        {
                            yr = 0;
                            state = 4;
                        }
                        break;
                }
            }

            if ((state != -1) && StdDate.DateValid(StdDate.DateVal(day, mn, yr)))
            {
                result = true;
                dateValue = StdDate.DateVal(day, mn, yr);
            }
            else
                result = false;

            return result;
        }
    }
}
