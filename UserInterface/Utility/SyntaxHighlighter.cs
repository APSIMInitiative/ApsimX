using System.Drawing;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Utility
{
    public class SyntaxHighlighter : System.ComponentModel.Component
    {
        #region Members
        private bool compiled = false;
        private Regex keywordsRegexp = null;
        private Regex functionsRegexp = null;
        private Regex typeNamesRegexp = null;
        private Regex stringsRegexp = null;
        private Regex commentsRegexp = null;
        #endregion

        [System.ComponentModel.Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        public List<string> CodeWords_Keywords { get; set; }
        [System.ComponentModel.Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        public List<string> CodeWords_Functions { get; set; }
        [System.ComponentModel.Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        public List<string> CodeWords_Types { get; set; }
        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        public List<string> CodeWords_Comments { get; set; }

        public Color CodeColor_PlainText { get; set; }
        public Color CodeColor_Keyword { get; set; }
        public Color CodeColor_Type { get; set; }
        public Color CodeColor_Function { get; set; }
        public Color CodeColor_Comment { get; set; }

        #region Constructor
        public SyntaxHighlighter()
        {
            CodeWords_Keywords = new List<string>();
            CodeWords_Functions = new List<string>();
            CodeWords_Types = new List<string>();
            CodeWords_Comments = new List<string>();
        }
        #endregion

        #region Methods
        public void DoSyntaxHightlight_CurrentLine(Editor codeTextbox)
        {
            #region Compile regexs if necessary
            if (!compiled)
            {
                Update(codeTextbox);
            }
            #endregion

            string line = codeTextbox.GetCurrentLine();
            int lineStart = codeTextbox.GetCurrentLineStartIndex();

            ProcessLine(codeTextbox, line, lineStart);
        }
        public void DoSyntaxHightlight_Selection(Editor codeTextbox, int selectionStart, int selectionLength)
        {
            #region Compile regexs if necessary
            if (!compiled)
            {
                Update(codeTextbox);
            }
            #endregion

            ProcessSelection(codeTextbox, selectionStart, selectionLength);
        }
        public void DoSyntaxHightlight_AllLines(Editor codeTextbox)
        {
            #region Compile regexs if necessary
            if (!compiled)
            {
                Update(codeTextbox);
            }
            #endregion

            ProcessAllLines(codeTextbox);
        }
        /// <summary>
        /// Compiles the necessary regexps
        /// </summary>
        /// <param name="syntaxSettings"></param>
        public void Update(Editor codeTextbox)
        {
            string keywords = string.Empty;
            string functions = string.Empty;
            string typeNames = string.Empty;
            string comments = string.Empty;

            #region Build the strings above for regexs
            #region Build keywords
            for (int i = 0; i < CodeWords_Keywords.Count; i++)
            {
                string strKeyword = CodeWords_Keywords[i];

                if (i == CodeWords_Keywords.Count - 1)
                    keywords += "\\b" + strKeyword + "\\b";
                else
                    keywords += "\\b" + strKeyword + "\\b|";
            }
            #endregion

            #region Build functions
            for (int i = 0; i < CodeWords_Functions.Count; i++)
            {
                string strFunction = CodeWords_Functions[i];

                if (i == CodeWords_Functions.Count - 1)
                    functions += "\\b" + strFunction + "\\b";
                else
                    functions += "\\b" + strFunction + "\\b|";
            }
            #endregion

            #region Build typeNames
            for (int i = 0; i < CodeWords_Types.Count; i++)
            {
                string strType = CodeWords_Types[i];

                if (i == CodeWords_Types.Count - 1)
                    typeNames += "\\b" + strType + "\\b";
                else
                    typeNames += "\\b" + strType + "\\b|";
            }
            #endregion

            #region Build comments
            for (int i = 0; i < CodeWords_Comments.Count; i++)
            {
                string strComments = CodeWords_Comments[i];

                if (i == CodeWords_Comments.Count - 1)
                    comments += "" + strComments + ".*$";
                else
                    comments += "" + strComments + ".*$|";
            }
            #endregion
            #endregion

            if (keywords != "")
                keywordsRegexp = new Regex(keywords, RegexOptions.Compiled | RegexOptions.Multiline);
            if (typeNames  != "")
                typeNamesRegexp = new Regex(typeNames, RegexOptions.Compiled | RegexOptions.Multiline);
            if (functions != "")
                functionsRegexp = new Regex(functions, RegexOptions.Compiled | RegexOptions.Multiline);
            if (comments != "")
                commentsRegexp = new Regex(comments, RegexOptions.Compiled | RegexOptions.Multiline);
            stringsRegexp = new Regex("\"[^\"\\\\\\r\\n]*(?:\\\\.[^\"\\\\\\r\\n]*)*\"", RegexOptions.Compiled | RegexOptions.Multiline);
            
            //commentsRegexp = new Regex(syntaxSettings.CommentString + ".*$", RegexOptions.Compiled | RegexOptions.Multiline);

            //Set compiled flag to true
            compiled = true;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Processes a regex.
        /// </summary>
        /// <param name="richTextbox"></param>
        /// <param name="line"></param>
        /// <param name="lineStart"></param>
        /// <param name="regexp"></param>
        /// <param name="color"></param>
        private void ProcessRegex(Editor codeTextbox, string line, int lineStart, Regex regexp, Color color)
        {
            if (regexp == null)
            {
                // for uninitialized typename regexp
                return;
            }

            Match regMatch;

            for (regMatch = regexp.Match(line); regMatch.Success; regMatch = regMatch.NextMatch())
            {
                // Process the words
                int nStart = lineStart + regMatch.Index;
                int nLenght = regMatch.Length;
                codeTextbox.SelectionStart = nStart;
                codeTextbox.SelectionLength = nLenght;
                codeTextbox.SelectionColor = color;
            }
        }
        /// <summary>
        /// Processes syntax highlightning for a line.
        /// </summary>
        /// <param name="richTextbox"></param>
        /// <param name="syntaxSettings"></param>
        /// <param name="line"></param>
        /// <param name="lineStart"></param>
        private void ProcessLine(Editor codeTextbox, string line, int lineStart)
        {
            //codeTextbox.EnablePainting = false;

            // Save the position and make the whole line black
            int nPosition = codeTextbox.SelectionStart;
            codeTextbox.SelectionStart = lineStart;
            codeTextbox.SelectionLength = line.Length;
            codeTextbox.SelectionColor = Color.Black;


            // Process the keywords
            ProcessRegex(codeTextbox, line, lineStart, keywordsRegexp, CodeColor_Keyword);

            // Process cached type names
            ProcessRegex(codeTextbox, line, lineStart, typeNamesRegexp, CodeColor_Type);

            //process functions
            ProcessRegex(codeTextbox, line, lineStart, functionsRegexp, CodeColor_Function);

            //process strings
            ProcessRegex(codeTextbox, line, lineStart, stringsRegexp, CodeColor_PlainText);

            // Process comments
            if (CodeWords_Comments.Count>0)
            {
                ProcessRegex(codeTextbox, line, lineStart, commentsRegexp, CodeColor_Comment);
            }

            codeTextbox.SelectionStart = nPosition;
            codeTextbox.SelectionLength = 0;
            codeTextbox.SelectionColor = Color.Black;

            //codeTextbox.EnablePainting = true;
        }
        private void ProcessSelection(Editor codeTextbox, int selectionStart, int selectionLength)
        {
            //codeTextbox.EnablePainting = false;

            // Save the position and make the whole line black
            int nPosition = selectionStart;
            
            codeTextbox.SelectionStart = selectionStart;
            codeTextbox.SelectionLength = selectionLength;
            string text = codeTextbox.SelectedText;

            codeTextbox.SelectionColor = Color.Black;


            // Process the keywords
            ProcessRegex(codeTextbox, text, selectionStart, keywordsRegexp, CodeColor_Keyword);

            // Process cached type names
            ProcessRegex(codeTextbox, text, selectionStart, typeNamesRegexp, CodeColor_Type);

            //process functions
            ProcessRegex(codeTextbox, text, selectionStart, functionsRegexp, CodeColor_Function);

            //process strings
            ProcessRegex(codeTextbox, text, selectionStart, stringsRegexp, CodeColor_PlainText);

            // Process comments
            if (CodeWords_Comments.Count > 0)
            {
                ProcessRegex(codeTextbox, text, selectionStart, commentsRegexp, CodeColor_Comment);
            }

            codeTextbox.SelectionStart = nPosition;
            codeTextbox.SelectionLength = 0;
            codeTextbox.SelectionColor = Color.Black;

            //codeTextbox.EnablePainting = true;
        }
        public void ProcessAllLines(Editor codeTextbox)
        {
            //codeTextbox.EnablePainting = false;

            // Save the position and make the whole line black
            int nPosition = codeTextbox.SelectionStart;
            codeTextbox.SelectionStart = 0;
            codeTextbox.SelectionLength = codeTextbox.Text.Length;
            codeTextbox.SelectionColor = Color.Black;

            // Process the keywords
            ProcessRegex(codeTextbox, codeTextbox.Text, 0, keywordsRegexp, CodeColor_Keyword);

            // Process cached type names
            ProcessRegex(codeTextbox, codeTextbox.Text, 0, typeNamesRegexp, CodeColor_Type);

            //process functions
            ProcessRegex(codeTextbox, codeTextbox.Text, 0, functionsRegexp, CodeColor_Function);

            // Process strings
            ProcessRegex(codeTextbox, codeTextbox.Text, 0, stringsRegexp, CodeColor_PlainText);

            // Process comments
            if (CodeWords_Comments.Count>0)
            {
                ProcessRegex(codeTextbox, codeTextbox.Text, 0, commentsRegexp, CodeColor_Comment);
            }

            codeTextbox.SelectionStart = nPosition;
            codeTextbox.SelectionLength = 0;
            codeTextbox.SelectionColor = Color.Black;


            //suppressHightlighting = false;
            //codeTextbox.EnablePainting = true;
        }
        #endregion
    }
}
