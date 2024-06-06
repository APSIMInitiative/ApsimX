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

            public ScriptLine Combine(ScriptLine line)
            {
                ScriptLine newLine = new ScriptLine();
                newLine.text = this.text + " " + line.text;
                foreach (string comment in this.comments)
                    newLine.comments.Add(comment);
                foreach (string comment in line.comments)
                    newLine.comments.Add(comment);
                foreach (string str in this.strings)
                    newLine.strings.Add(str);
                foreach (string str in line.strings)
                    newLine.strings.Add(str);
                return newLine;
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

            //Remove unnecessary whitespace
            input = removeWhitespace(input);

            //Fix properties to be back to one row
            input = combinePropertyRows(input);

            //put single line Link tags back to one row
            input = combineLinkRows(input);

            //Add empty line spacing back in
            input = addSpacing(input);

            //Add tab whitespace back in
            input = addIndent(input, "\t");

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
            char[] newlineSymbols = new char[] {'{', '}'};
            List<ScriptLine> newLines = new List<ScriptLine>();

            foreach(ScriptLine line in lines)
            {
                List<ScriptLine> subLines = new List<ScriptLine>();
                string[] parts = Split(line.text, newlineSymbols);
                for(int j = 0; j < parts.Length; j++)
                {
                    ScriptLine newLine = new ScriptLine(parts[j].Trim());
                    subLines.Add(newLine);
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
                        i = subLine.text.IndexOf("//", i+1);
                    }
                    i = subLine.text.IndexOf("\"\"");
                    while (i >= 0) 
                    {
                        subLine.strings.Add(line.strings[commentIndex]);
                        stringIndex += 1;
                        i = subLine.text.IndexOf("\"\"", i+1);
                    }
                    newLines.Add(subLine);
                }
                //set last line to have same openComment value
                newLines[newLines.Count-1].openComment = line.openComment;
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
                bool match = false;
                foreach(char s in characters)
                {
                    if (c == s) 
                    {
                        match = true;
                    }
                }
                if (match && current.Length > 0)
                {
                    parts.Add(current);
                    parts.Add(c.ToString());
                    current = "";
                }
                else
                {
                    current += c;
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
            bool dollarString = false;
            int inCurlys = 0;
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
                        if (previousChar == '$') 
                            dollarString = true;
                        else
                            dollarString = false;
                    }
                }
                else
                {
                    if (dollarString)
                    {
                        if (c == '{')
                            inCurlys += 1;
                        else if (c == '}')
                            inCurlys -= 1;
                    }
                    
                    if (c == '"' && previousChar != '\\' && inCurlys == 0)
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
            foreach(ScriptLine line in lines)
            {
                string text = line.text;
                while(text.Contains("  ") || text.Contains("\t\t") || text.Contains("\t ") || text.Contains(" \t")) 
                {
                    text = text.Replace("  ", " ");
                    text = text.Replace("\t\t", "\t");
                    text = text.Replace("\t ", "\t");
                    text = text.Replace(" \t", "\t");
                }
                line.text = text;
            }
            return clearEmptyLines(lines, true);
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// optional interior can be added so that they only combine if the given string is inside.
        /// </summary>
        private static List<ScriptLine> combineRows(List<ScriptLine> lines, char start, char end, string interior = "")
        {
            
            List<ScriptLine> newLines = new List<ScriptLine>();            
            for(int i = 0; i < lines.Count; i++)
            {
                if (lines[i].text.StartsWith(start))
                {
                    ScriptLine newLine = new ScriptLine();
                    int startI = i;
                    newLine.text = "";
                    bool stop = false;
                    for(int j = i; j < lines.Count && !stop; j++)
                    {
                        newLine = newLine.Combine(lines[j]);
                        if (lines[j].text.EndsWith(end)) 
                            stop = true;
                        else 
                            i++;
                    }
                    bool writeLine = true;
                    if (interior.Length > 0) {
                        int count = 0;
                        for (int j = 0; j < newLine.text.Length && writeLine; j++) 
                        {
                            if (newLine.text[j] == '{')
                                count += 1;
                            if (count > 1)
                                writeLine = false;
                        }
                        if (writeLine) {
                            if (newLine.text.Replace(" ", "").Replace("\t", "").Contains(interior.Replace(" ", "").Replace("\t", ""))) 
                                writeLine = true;
                            else
                                writeLine = false;
                        }
                    }
                    
                    if (writeLine) 
                    {
                        newLine.text = newLine.text.Replace(start+" ", start.ToString());
                        newLine.text = newLine.text.Replace(" "+end, end.ToString());
                        newLine.text = newLine.text.Trim();

                        newLines.Add(newLine);
                    }
                    else
                    {
                        i = startI;
                        newLines.Add(lines[i]);
                    }
                }
                else
                {
                    newLines.Add(lines[i]);
                }
            }
            return clearEmptyLines(newLines, true);
        }

        /// <summary>
        /// 
        /// </summary>
        private static List<ScriptLine> combinePropertyRows(List<ScriptLine> lines)
        {
            
            List<ScriptLine> newLines = new List<ScriptLine>(lines);
            string[] searches = new string[] {"get; set;", "get; private set;", "private get; set;", "private get; private set;"};

            //Combine {get; set;} back to one row
            for (int i = 0 ; i < searches.Length; i++) 
            {
                newLines = combineRows(newLines, '{', '}', searches[i]);
                for(int j = 1; j < newLines.Count-1; j++)
                {
                    ScriptLine line = newLines[j];
                    if (line.text.Contains(searches[i]))
                    {
                        newLines[j-1] = newLines[j-1].Combine(line);
                        newLines.RemoveAt(j);
                        //check if the next line starts with = meaning it was an assignment
                        if (newLines[j].text.StartsWith("="))
                        {
                            newLines[j-1] = newLines[j-1].Combine(newLines[j]);
                            newLines.RemoveAt(j);
                        }
                    }
                }
            }
            
            return newLines;
        }

        /// <summary>
        /// 
        /// </summary>
        private static List<ScriptLine> combineLinkRows(List<ScriptLine> lines)
        {
            List<ScriptLine> newLines = new List<ScriptLine>(lines);
            for(int i = 0; i < newLines.Count; i++)
            {
                ScriptLine line = newLines[i];
                if (line.text.StartsWith("[Link") && line.text.EndsWith("]")) 
                {
                    newLines[i] = newLines[i].Combine(newLines[i+1]);
                    newLines.RemoveAt(i+1);
                }
            }
            return newLines;
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// </summary>
        private static List<ScriptLine> addIndent(List<ScriptLine> lines, string indent)
        {
            bool oneLineIndent = false;
            int indents = 0;
            List<ScriptLine> newLines = new List<ScriptLine>();

            foreach(ScriptLine line in lines)
            {
                string newLine = "";
                if (line.text.Contains('}') && !line.text.Contains('{'))
                {
                    indents -= 1;
                }
                if (line.text.Contains('{'))
                {
                    oneLineIndent = false;
                }

                for(int i = 0; i < indents; i++) {
                    newLine += indent;
                }
                if (oneLineIndent)
                {
                    newLine += indent;
                    oneLineIndent = false;
                }

                newLine += line.text;

                if (line.text.Contains('{') && !line.text.Contains('}'))
                {
                    indents += 1;
                }
                if (line.text.Contains('(') && !line.text.Contains(')'))
                {
                    indents += 1;
                }
                if (line.text.Contains(')') && !line.text.Contains('('))
                {
                    indents -= 1;
                }
                if (line.text.StartsWith("if (") || line.text.StartsWith("if(") ||
                    line.text.CompareTo("else") == 0 ||
                    line.text.StartsWith("else if (") || line.text.StartsWith("else if("))
                {
                    oneLineIndent = true;
                }

                line.text = newLine;
                newLines.Add(line);
            }

            return newLines;
        }

        /// <summary>
        /// Combine rows between two characters back together
        /// </summary>
        private static List<ScriptLine> addSpacing(List<ScriptLine> lines)
        {
            
            List<ScriptLine> newLines = new List<ScriptLine>();

            //Add spaces before
            newLines.Add(lines[0]);
            for(int i = 1; i < lines.Count; i++)
            {
                ScriptLine line = lines[i];
                ScriptLine prevLine = lines[i-1];
                if (prevLine.text.Length != 0)
                {
                    if (line.text.StartsWith("namespace"))
                        newLines.Add(new ScriptLine());
                    else if (line.text.StartsWith('[') && line.text.EndsWith(']') && !prevLine.text.StartsWith("//"))
                        newLines.Add(new ScriptLine());
                    else if (line.text.StartsWith("//"))
                        newLines.Add(new ScriptLine());
                    else if ((line.text.StartsWith("private") || line.text.StartsWith("public")) && prevLine.text == "}")
                        newLines.Add(new ScriptLine());
                }
                newLines.Add(line);
            }

            //delete spaces after attributes
            for(int i = 0; i < newLines.Count-1; i++)
            {
                ScriptLine line = newLines[i];
                ScriptLine nextLine = newLines[i+1];
                if (nextLine.text.Length == 0)
                {
                    if (line.text.StartsWith('[') && line.text.EndsWith(']'))
                    {
                        if (newLines[i+1].text.Length == 0) {
                            newLines.RemoveAt(i+1);
                        }
                    }
                    if (line.text == "{")
                    {
                        if (newLines[i+1].text.Length == 0) {
                            newLines.RemoveAt(i+1);
                        }
                    }
                }
            }

            return newLines;
        }
    }
}
