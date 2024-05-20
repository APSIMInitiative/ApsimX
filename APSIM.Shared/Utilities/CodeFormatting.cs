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
            public List<string> strings;

            public List<string> comments;

            public string text;

            public bool openComment;

            public ScriptLine()
            {
                this.text = "";
                this.strings = new List<string>();
                this.comments = new List<string>();
                this.openComment = false;
            }

            public ScriptLine(string t)
            {
                this.text = t;
                this.strings = new List<string>();
                this.comments = new List<string>();
                this.openComment = false;
            }
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
            List<ScriptLine> input = new List<ScriptLine>();
            foreach(string line in lines)
            {
                ScriptLine newLine = new ScriptLine(line);
                input.Add(newLine);
            }

            //Remove strings and comments and store for later
            input = RemoveSafeText(input);

            //trim whitespace from lines
            input = clearEmptyLines(input, true);

            //make sure { } and [ ] are on new lines
            input = putSymbolsOnNewLines(input);
/*
            //Remove unnecessary whitespace
            input = removeWhitespace(input);

            //Combine links [ ] back together
            input = combineRows(input, '[', ']');

            //Fix properties to be back to one row
            input = combinePropertyRows(input);

            //put single line Link tags back to one row
            input = combineLinkRows(input);

            //Add empty line spacing back in
            input = addSpacing(input);

            //Add tab whitespace back in
            input = addIndent(input, "\t");
*/
            //Restore strings and comments
            input = RestoreSafeText(input);
            
            string[] output = new string[input.Count];
            for(int i = 0; i < input.Count; i++)
                output[i] = input[i].text;

            return output;
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
        private static List<ScriptLine> clearEmptyLines(List<ScriptLine> lines, bool doTrim)
        {
            List<ScriptLine> output = new List<ScriptLine>();
            foreach(ScriptLine line in lines)
            {
                if (line.text.Length > 0) 
                {
                    if (doTrim)
                    {
                        line.text = line.text.Trim();
                        output.Add(line);
                    }
                    else
                    {
                        output.Add(line);
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// </summary>
        private static List<ScriptLine> putSymbolsOnNewLines(List<ScriptLine> lines)
        {
            char[] newlineSymbols = new char[] {'{', '}', '[', ']'};
            List<ScriptLine> newLines = new List<ScriptLine>();

            foreach(ScriptLine line in lines)
            {
                List<ScriptLine> subLines = new List<ScriptLine>();
                //only do this if the line contains the symbol
                if (Contains(line.text, newlineSymbols) && line.text.Length > 1)
                {
                    string[] parts = Split(line.text, newlineSymbols);
                    for(int j = 0; j < parts.Length; j++)
                    {
                        ScriptLine newLine = new ScriptLine(parts[j].Trim());
                        newLine.openComment = line.openComment;
                        subLines.Add(newLine);
                    }
                }

                int commentIndex = 0;
                int stringIndex = 0;
                foreach(ScriptLine subLine in subLines)
                {
                    int i = subLine.text.IndexOf("//");
                    while (i >= 0) 
                    {
                        subLine.comments.Add(line.comments[commentIndex]);
                        commentIndex += 1;
                        i = subLine.text.IndexOf("//", i);
                    }
                    i = subLine.text.IndexOf("\"\"");
                    while (i >= 0) 
                    {
                        subLine.comments.Add(line.strings[commentIndex]);
                        stringIndex += 1;
                        i = subLine.text.IndexOf("\"\"", i);
                    }
                    newLines.Add(subLine);
                }
            }
            return newLines;
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
        public static string[] Split(string line, char[] characters)
        {
            List<string> parts = new List<string>();
            string current = "";
            foreach(char c in line)
            {
                foreach(char s in characters)
                {
                    if (c == s) 
                    {
                        if (current.Length > 0)
                        {
                            parts.Add(current);
                            current = "";
                        }
                    }
                }
            }
            if (current.Length > 0)
                parts.Add(current);

            return parts.ToArray();
        }

        /// <summary>
        /// </summary>
        private static List<ScriptLine> RemoveSafeText(List<ScriptLine> lines)
        {
            List<ScriptLine> newLines = new List<ScriptLine>();

            bool nextLineStartsAsComment = false;
            foreach(ScriptLine line in lines)
            {
                ScriptLine nLine = RemoveSafeTextFromLine(line, nextLineStartsAsComment);
                if (nLine.openComment)
                    nextLineStartsAsComment = true;
                else
                    nextLineStartsAsComment = false;
                newLines.Add(nLine);
            }
            return newLines;
        }

        /// <summary>
        /// </summary>
        private static ScriptLine RemoveSafeTextFromLine(ScriptLine line, bool startsAsComment)
        {
            ScriptLine newLine = new ScriptLine();
            newLine.text = line.text;

            bool inComment = startsAsComment;
            bool multiline = startsAsComment;
            string newText = "";
            string newComment = "";
            char previousChar = ' ';
            char previousChar2 = ' ';
            //COMMENTS
            foreach(char c in newLine.text)
            {
                if (!inComment)
                {
                    newText += c;
                    if (c == '/' && previousChar == '/' && previousChar2 != '\\')
                    {
                        inComment = true;
                        newComment = "//";
                    }
                    if (c == '*' && previousChar == '/' && previousChar2 != '\\')
                    {
                        inComment = true;
                        multiline = true;
                        newComment = "/*";
                        newText = newText.Remove(newText.Length-1);
                        newText += "/";
                    }
                }
                else
                {
                    newComment += c;
                    if (c == '/' && previousChar == '*' && previousChar2 != '\\')
                    {
                        inComment = false;
                        multiline = false;
                        newLine.comments.Add(newComment);
                        newComment = "";
                    }
                }
                previousChar2 = previousChar;
                previousChar = c;
            }
            newLine.text = newText;
            if (newComment.Length > 0)
                newLine.comments.Add(newComment);
            newLine.openComment = multiline;

            //STRINGS
            newText = "";
            bool inString = false;
            string newString = "";
            previousChar = ' ';
            foreach(char c in newLine.text)
            {
                if (!inString)
                {
                    newText += c;
                    if (c == '"' && previousChar != '\\')
                    {
                        inString = true;
                        newString = "";
                    }
                }
                else
                {
                    if (c == '"' && previousChar != '\\')
                    {
                        newText += c;
                        inString = false;
                        newLine.strings.Add(newString);
                    }
                    else
                    {
                        newString += c;
                    }
                }
                previousChar = c;
            }
            newLine.text = newText;

            return newLine;
        }

        /// <summary>
        /// </summary>
        private static List<ScriptLine> RestoreSafeText(List<ScriptLine> lines)
        {
            List<ScriptLine> newLines = new List<ScriptLine>();

            bool nextLineStartsAsComment = false;
            foreach(ScriptLine line in lines)
            {
                ScriptLine nLine = RestoreSafeTextToLine(line, nextLineStartsAsComment);
                newLines.Add(nLine);

                if (line.openComment)
                    nextLineStartsAsComment = true;
                else
                    nextLineStartsAsComment = false;
            }
            return newLines;
        }

        /// <summary>
        /// </summary>
        private static ScriptLine RestoreSafeTextToLine(ScriptLine line, bool startsAsComment)
        {
            string text = "";
            int index = 0;
            if (startsAsComment && line.comments.Count > 0) 
            {
                text += line.comments[0];
                index = 1;
            }
            text += line.text;
            
            for (int i = index; i < line.comments.Count; i++)
            {
                index = text.IndexOf("//", index);
                if (index >= 0) 
                {
                    text = text.Remove(index, 2);
                    text = text.Insert(index, line.comments[i]);
                }
            }

            index = 0;
            foreach(string stringPart in line.strings)
            {
                index = text.IndexOf("\"\"", index);
                if (index >= 0) 
                    text = text.Insert(index + 1, stringPart);
            }

            return new ScriptLine(text);
        }

        /// <summary>
        /// Removes tabs and double spaces
        /// </summary>
        private static List<ScriptLine> removeWhitespace(List<ScriptLine> lines)
        {
            List<ScriptLine> output = new List<ScriptLine>();
            foreach(ScriptLine line in lines)
            {
                string text = line.text;
                while(text.Contains("  ")) 
                {
                    text = text.Replace("  ", " ");
                }
                while(text.Contains("\t\t")) 
                {
                    text = text.Replace("\t\t", "\t");
                }
                line.text = text;
                output.Add(line);
            }
            return clearEmptyLines(output, true);
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// optional interior can be added so that they only combine if the given string is inside.
        /// </summary>
        private static List<ScriptLine> combineRows(List<ScriptLine> lines, char start, char end, string interior = "")
        {
            /*
            List<ScriptLine> output = new List<ScriptLine>();
            for(int i = 0; i < lines.Count; i++)
            {
                if (lines[i].text.StartsWith(start))
                {
                    int startI = i;
                    string text = "";
                    bool stop = false;
                    for(int j = i; j < lines.Count && !stop; j++)
                    {
                        text += lines[j].text + " ";
                        if (lines[j].text.EndsWith(end)) 
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
                        if (safeContains(text.Replace(" ", "").Replace("\t", ""), interior.Replace(" ", "").Replace("\t", ""))) 
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
                        text = safeReplaceString(text, start+" ", start.ToString());
                        text = safeReplaceString(text, " "+end, end.ToString());

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
            */
            return lines;
        }

        /// <summary>
        /// 
        /// </summary>
        private static List<ScriptLine> combinePropertyRows(List<ScriptLine> lines)
        {
            /*
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
            }*/
            return lines;
        }

        /// <summary>
        /// 
        /// </summary>
        private static List<ScriptLine> combineLinkRows(List<ScriptLine> lines)
        {
            /*List<string> output = new List<string>(lines);

            for(int i = 0; i < output.Count; i++)
            {
                string line = output[i];
                if (safeContains(line, "[Link") && safeContains(line, "]")) 
                {
                    output[i] = line + " " + output[i+1];
                    output.RemoveAt(i+1);
                }
            }
            return output;*/
            return lines;
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// </summary>
        private static List<ScriptLine> addIndent(List<ScriptLine> lines, string indent)
        {
            /*int indents = 0;
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

            return output;*/
            return lines;
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// </summary>
        private static List<ScriptLine> addSpacing(List<ScriptLine> lines)
        {
            /*
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
            return output;*/
            return lines;
        }
    }
}
