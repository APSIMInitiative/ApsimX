using System.Collections.Generic;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// Class to provide functions to reformat code
    /// </summary>
    public static class CodeFormatting 
    {
        //Stores each line of code alongside any strings or comments that are removed while formatting
        private class ScriptLine {
            List<string> strings;

            List<List<string>> comments;

            string text;
        }

        /// <summary>
        /// </summary>
        public static string Reformat(string lines)
        {
            return Combine(Reformat(Split(lines)));
        }

        /// <summary>
        /// </summary>
        public static string[] Reformat(string[] lines)
        {
            List<string> output = new List<string>(lines);

            //Remove strings and comments and store for later
            output;

            //trim whitespace from lines
            output = clearEmptyLines(output, true);

            //make sure { } and [ ] are on new lines
            output = putSymbolsOnNewLines(output);

            //Remove unnecessary whitespace
            output = removeWhitespace(output);

            //Combine links [ ] back together
            output = combineRows(output, '[', ']');

            //Fix properties to be back to one row
            output = combinePropertyRows(output);

            //put single line Link tags back to one row
            output = combineLinkRows(output);

            //Add empty line spacing back in
            output = addSpacing(output);

            //Add tab whitespace back in
            output = addIndent(output, "\t");

            //Restore strings and comments
            output;

            return output.ToArray();
        }

        /// <summary>
        /// </summary>
        public static string Combine(string[] lines)
        {
            string output = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Replace("\r", ""); //remove \r from scripts for platform consistency
                output += line;
                if (i < lines.Length-1)
                    output += "\n";
            }
            return output;
        }

        /// <summary>
        /// </summary>
        public static string[] Split(string lines)
        {
            string output = lines.Replace("\r", "");
            return output.Split('\n');
        }

        /// <summary>
        /// Remove blank lines and trim whitespace. New copy is returned.
        /// </summary>
        private static List<string> clearEmptyLines(List<string> lines, bool doTrim)
        {
            List<string> output = new List<string>();
            foreach(string line in lines)
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
        private static List<string> putSymbolsOnNewLines(List<string> lines)
        {
            char[] newlineSymbols = new char[] {'{', '}', '[', ']'};
            List<string> output = new List<string>(lines);

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
        public static bool Contains(string line, char[] characters)
        {
            foreach(char c in line)
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
        /// Check if the given code line has any of the provided characters in it
        /// </summary>
        public static bool Contains(string line, char character)
        {
            return Contains(line, new char[] {character});
        }

        /// <summary>
        /// Check if the given code line has any of the provided characters in it
        /// </summary>
        private static bool safeContains(string line, string value)
        {
            List<string> parts = safeSplitString(line, new char[] {'"'});
            bool result = false;
            bool isString = false;
            foreach(string part in parts)
            {
                if (part.CompareTo("\"") == 0)
                    isString = !isString;
                else if (part.Contains(value) && !isString)
                    result = true;
            }
            return result;
        }

        /// <summary>
        /// Check if the given code line has any of the provided characters in it
        /// </summary>
        private static bool safeContains(string line, char value)
        {
            return safeContains(line, value.ToString());
        }

        /// <summary>
        /// Splits a string but make sure the seperator isn't within a string component
        /// Empty parts are not returned.
        /// </summary>
        private static List<string> safeSplitString(string line, char[] seperators)
        {
            List<string> output = new List<string>();
            bool inString = false;
            char previous = ' ';
            string ss = "";

            foreach(char c in line)
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
                        output = output.Remove(i, oldValue.Length);
                        output = output.Insert(i, newValue);
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
        private static List<string> removeWhitespace(List<string> lines)
        {
            List<string> output = new List<string>();
            foreach(string line in lines)
            {
                string newLine = line;
                while(safeContains(newLine, "  ")) 
                {
                    newLine = safeReplaceString(newLine, "  ", " ");
                }
                while(safeContains(newLine, "\t\t")) 
                {
                    safeReplaceString(newLine, "\t\t", "\t");
                }
                output.Add(newLine);
            }
            return clearEmptyLines(output, true);
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// optional interior can be added so that they only combine if the given string is inside.
        /// </summary>
        private static List<string> combineRows(List<string> lines, char start, char end, string interior = "")
        {
            List<string> output = new List<string>();
            for(int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(start))
                {
                    int startI = i;
                    string newLine = "";
                    bool stop = false;
                    for(int j = i; j < lines.Count && !stop; j++)
                    {
                        newLine += lines[j] + " ";
                        if (lines[j].EndsWith(end)) 
                        {
                            stop = true;
                        }
                        else 
                        {
                            i++;
                        }
                    }
                    bool writeLine = false;
                    if (interior.Length > 0) {
                        if (safeContains(newLine.Replace(" ", "").Replace("\t", ""), interior.Replace(" ", "").Replace("\t", ""))) 
                        {
                            writeLine = true;
                        }
                    } 
                    else
                    {
                        writeLine = true;
                    }
                    if (writeLine) 
                    {
                        newLine = safeReplaceString(newLine, start+" ", start.ToString());
                        newLine = safeReplaceString(newLine, " "+end, end.ToString());
                        output.Add(newLine.Trim());
                    }
                    else
                    {
                        i = startI;
                        output.Add(lines[i]);
                    }
                }
                else
                {
                    output.Add(lines[i]);
                }
            }
            return clearEmptyLines(output, true);
        }

        /// <summary>
        /// 
        /// </summary>
        private static List<string> combinePropertyRows(List<string> lines)
        {
            List<string> output = new List<string>(lines);

            //Combine {get; set;} back to one row
            output = combineRows(output, '{', '}', "get; set;");

            for(int i = 0; i < output.Count; i++)
            {
                string line = output[i];
                if (safeContains(line, "{get; set;}")) 
                {
                    output[i-1] += " " + line;
                    output.RemoveAt(i);
                }
            }
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        private static List<string> combineLinkRows(List<string> lines)
        {
            List<string> output = new List<string>(lines);

            for(int i = 0; i < output.Count; i++)
            {
                string line = output[i];
                if (safeContains(line, "[Link") && safeContains(line, "]")) 
                {
                    output[i] = line + " " + output[i+1];
                    output.RemoveAt(i+1);
                }
            }
            return output;
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// </summary>
        private static List<string> addIndent(List<string> lines, string indent)
        {
            int indents = 0;
            List<string> output = new List<string>();

            foreach(string line in lines)
            {
                string newLine = "";
                if (safeContains(line, '}') && !safeContains(line, '{'))
                {
                    indents -= 1;
                }

                for(int i = 0; i < indents; i++) {
                    newLine += indent;
                }
                newLine += line;

                if (safeContains(line, '{') && !safeContains(line, '}'))
                {
                    indents += 1;
                }

                output.Add(newLine);
            }

            return output;
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// </summary>
        private static List<string> addSpacing(List<string> lines)
        {
            List<string> output = new List<string>();
            string previousLine = " ";

            foreach(string line in lines)
            {
                bool emptyAbove = false;
                if (previousLine.Length == 0 || previousLine.StartsWith("[") || previousLine.StartsWith("{"))
                    emptyAbove = true;

                if (!emptyAbove && line.StartsWith("namespace"))
                {
                    emptyAbove = true;
                    output.Add("");
                }

                if (!emptyAbove && line.StartsWith('[') && line.EndsWith(']'))
                {
                    emptyAbove = true;
                    output.Add("");
                }

                if ((!emptyAbove) && (line.StartsWith("public") || line.StartsWith("private")))
                {
                    emptyAbove = true;
                    output.Add("");
                }
                output.Add(line);
                previousLine = line;
            }
            return output;
        }
    }
}
