using System;

namespace StdUnits
{
    /// <summary>
    /// String utility functions
    /// </summary>
    static public class StdStrng
    {
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


        /// <summary>
        /// Token_Date follows the same rules for its parameters as Token_Int.  It    
        /// expects day to precede months, and months to precede years, but it can    
        /// cope with D-M-Y and D-M-0 kinds of date.  Token_Date is implemented as a  
        /// state-based parser:                                                       
        ///                                                                            
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
        /// <param name="InSt"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        public static bool TokenDate(ref string InSt, ref int D)
        {
            bool result;
            // We need to have these in upper case as that is how TextToken returns them     
            string[] MonthTexts = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

            int State = 0;
            int Day = 0;
            int Mn = 0;
            int Yr = 0;
            while ((State != -1) && (State != 4))
            {
                switch (State)
                {
                    case 0:
                        if (TokenInt(ref InSt, ref Day) && (Day >= 1) && (Day <= 31))     // State 0:  we expect a number for the day 
                            State = 1;
                        else
                            State = -1;
                        break;
                    case 1: if ((MatchToken(ref InSt, "/") || MatchToken(ref InSt, "-")))             // State 1: Day found, looking for a        
                            State = 2;                                                      //   delimiter or a month text              
                        else
                        {
                            Mn = 1;
                            while ((Mn <= 12) && (!MatchToken(ref InSt, MonthTexts[Mn])))
                                Mn++;
                            if (Mn <= 12)
                                State = 3;
                            else
                                State = 2;
                        }
                        break;
                    case 2: if (TokenInt(ref InSt, ref Mn) && (Mn >= 1) && (Mn <= 12))        // State 2: Day/month delimiter found, so   
                            State = 3;                                                             //   looking for a numeric month            
                        else
                            State = -1;
                        break;
                    case 3: if ((MatchToken(ref InSt, "/") || MatchToken(ref InSt, "-")))             // State 3:  month has been found.  Clear   
                            State = 3;                                                      //   away any delimiter and then look for a 
                        else if (TokenInt(ref InSt, ref Yr) && (Yr >= 1))                  //   year                                  
                        {
                            if (Yr < 100)
                                Yr = 1900 + Yr;
                            State = 4;                                                     // State=4 is the exit point                
                        }
                        else
                        {
                            Yr = 0;
                            State = 4;
                        }
                        break;
                }
            }

            if ((State != -1) && StdDate.DateValid(StdDate.DateVal(Day, Mn, Yr)))
            {
                result = true;
                D = StdDate.DateVal(Day, Mn, Yr);
            }
            else
                result = false;

            return result;
        }
    }
}
