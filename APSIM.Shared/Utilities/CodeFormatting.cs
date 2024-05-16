using System;
using System.Collections.Generic;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// Class to provide functions to reformat code
    /// </summary>
    public static class CodeFormatting 
    {
        /// <summary>
        /// </summary>
        public static string Reformat(string code)
        {
            return Combine(Reformat(Split(code)));
        }

        /// <summary>
        /// </summary>
        public static string[] Reformat(string[] code)
        {
            List<string> lines = new List<string>(code);

            //trim whitespace from lines
            lines = clearEmptyLines(lines, true);

            //make sure { } and [ ] are on new lines
            lines = putSymbolsOnNewLines(lines);

            //Remove unnecessary whitespace
            lines = removeWhitespace(lines);

            return lines.ToArray();
        }

        /// <summary>
        /// </summary>
        public static string Combine(string[] code)
        {
            string output = "";
            for (int i = 0; i < code.Length; i++)
            {
                string line = code[i].Replace("\r", ""); //remove \r from scripts for platform consistency
                output += line;
                if (i < code.Length-1)
                    output += "\n";
            }
            return output;
        }

        /// <summary>
        /// </summary>
        public static string[] Split(string code)
        {
            string output = code.Replace("\r", "");
            return output.Split('\n');
        }

        /// <summary>
        /// Remove blank lines and trim whitespace. New copy is returned.
        /// </summary>
        private static List<String> clearEmptyLines(List<String> code, bool doTrim)
        {
            List<string> output = new List<string>();
            foreach(string line in code)
            {
                if (line.Length > 0) 
                {
                    if (doTrim)
                        output.Add(line.Trim());
                    else
                        output.Add(line);
                }
            }
            return output;
        }

        /// <summary>
        /// </summary>
        private static List<String> putSymbolsOnNewLines(List<String> code)
        {
            char[] newlineSymbols = new char[] {'{', '}', '[', ']'};
            List<string> output = new List<string>(code);

            for(int i = 0; i < output.Count; i++) 
            {
                string line = output[i];
                //only do this if the line contains the symbol
                if (Contains(line, newlineSymbols) && line.Length > 1)
                {
                    output.RemoveAt(i); //remove existing line
                    List<string> parts = safeSplitString(line, newlineSymbols);
                    for(int j = parts.Count-1; j >= 0; j--)
                        output.Insert(i, parts[j].Trim());
                }
            }
            return output;
        }

        /// <summary>
        /// Check if the given code line has any of the provided characters in it
        /// </summary>
        public static bool Contains(string code, char[] characters)
        {
            foreach(char c in code)
            {
                foreach(char s in characters)
                {
                    if (c == s) 
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Splits a string but make sure the seperator isn't within a string component
        /// Empty parts are not returned.
        /// </summary>
        private static List<String> safeSplitString(string code, char[] seperators)
        {
            List<string> output = new List<string>();
            bool inString = false;
            char previous = ' ';
            string ss = "";

            foreach(char c in code)
            {
                bool isSeperator = false;
                char seperator = ' ';
                foreach(char s in seperators)
                {
                    if (c == s) 
                    {
                        isSeperator = true;
                        seperator = s;
                    }
                }

                if (!inString && isSeperator)
                {
                    if (ss.Length > 0) 
                    {
                        output.Add(ss);
                        ss = "";
                    }
                    output.Add(seperator.ToString());
                }
                else
                {
                    ss += c;
                }

                if ((c == '"' || c== '\'') && previous != '\\')
                {
                    inString = !inString;
                }
                previous = c;
            }

            if (ss.Length > 0) 
                output.Add(ss);

            return output;
        }

        /// <summary>
        /// Splits a string but make sure the seperator isn't within a string component
        /// Empty parts are not returned.
        /// </summary>
        private static string safeReplaceString(string line, string oldValue, string newValue)
        {
            string output = line;
            bool inString = false;
            char previous = ' ';

            for(int i = 0; i < output.Length; i++) 
            {
                char c = output[i];
                if (!inString && c == oldValue[0] && output.Length - i > oldValue.Length)
                {
                    string match = "";
                    for(int j = 0; j < oldValue.Length; j++) 
                    {
                        match += output[i+j];
                    }
                    if (match.CompareTo(oldValue) == 0) {
                        output.Remove(i, oldValue.Length);
                        output.Insert(i, newValue);
                    }
                }
                if ((c == '"' || c== '\'') && previous != '\\')
                {
                    inString = !inString;
                }
                previous = c;
            }
            return output;
        }

        /// <summary>
        /// Removes tabs and double spaces
        /// </summary>
        private static List<String> removeWhitespace(List<String> code)
        {
            List<string> output = new List<string>();
            foreach(string line in code)
            {
                string newLine = line;
                while(line.Contains("  ")) 
                {
                    newLine = safeReplaceString(newLine, "  ", " ");
                }
                while(newLine.Contains("\t\t")) 
                {
                    safeReplaceString(newLine, "\t\t", "\t");
                }
                output.Add(newLine);
            }
            return clearEmptyLines(output, true);
        }
    }
}
