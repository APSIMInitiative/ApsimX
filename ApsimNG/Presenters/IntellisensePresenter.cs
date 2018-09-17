namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Views;
    using Intellisense;
    using EventArguments;
    using ICSharpCode.NRefactory.Editor;
    using ICSharpCode.NRefactory.CSharp;
    using Models.Core;
    using System.Globalization;

    /// <summary>
    /// Responsible for handling intellisense operations.
    /// In order to use this class, <see cref="LoseFocus"/>, <see cref="ItemSelected"/>, and <see cref="ContextItemsNeeded"/> must be set.
    /// Instantiating this class can be somewhat expensive (~1-2 seconds).
    /// </summary>
    public class IntellisensePresenter
    {
        /// <summary>
        /// The view responsible for displaying the intellisense options on the screen.
        /// </summary>
        private IntellisenseView view = new IntellisenseView();

        /// <summary>
        /// Fired when the we need to generate intellisense suggestions.
        /// </summary>
        private event EventHandler<NeedContextItemsArgs> onContextItemsNeeded;

        /// <summary>
        /// Fired when an item is selected in the intellisense window.
        /// </summary>
        private event EventHandler<IntellisenseItemSelectedArgs> onItemSelected;

        /// <summary>
        /// Responsible for generating the completion options.
        /// </summary>
        private CSharpCompletion completion = new CSharpCompletion();
        
        /// <summary>
        /// List of intellisense options.
        /// Probably doesn't need to be a class field, but I have plans to use it in the future. - DH May 2018
        /// </summary>
        private CodeCompletionResult completionResult;

        /// <summary>
        /// The partially-finished word for which the user wants completion options. May be empty string.
        /// </summary>
        private string triggerWord = string.Empty;

        /// <summary>
        /// Constructor. Requires a reference to the view holding the text editor.
        /// </summary>
        /// <param name="textEditor">Reference to the view holding the text editor. Cannot be null.</param>
        public IntellisensePresenter(ViewBase textEditor)
        {
            if (textEditor == null)
                throw new ArgumentException("textEditor cannot be null.");
            Editor = textEditor; // ?? throw new ArgumentException("textEditor cannot be null.");

            // The way that the ItemSelected event handler works is a little complicated. If the user has half-typed 
            // a word and needs completion options for it, we can't just insert the selected completion at the caret 
            // - half of the word will be duplicated. Instead, we need to intercept the event, add the trigger word 
            // to the event args, and call the event handler provided to us. The view is then responsible for 
            // inserting the completion option at the appropriate point and removing the half-finished word.
            view.ItemSelected += (sender, e) =>
            {
                e.TriggerWord = triggerWord;
                onItemSelected?.Invoke(this, e);
            };
        }

        /// <summary>
        /// Fired when the intellisense window loses focus.
        /// </summary>
        public event EventHandler LoseFocus
        {
            add
            {
                view.LoseFocus += value;
            }
            remove
            {
                view.LoseFocus -= value;
            }
        }

        /// <summary>
        /// Fired when an item is selected in the intellisense window.
        /// </summary>
        public event EventHandler<IntellisenseItemSelectedArgs> ItemSelected
        {
            add
            {
                DetachHandlers(ref onItemSelected);
                onItemSelected += value;
            }
            remove
            {
                onItemSelected -= value;
            }
        }

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.').
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded
        {
            add
            {
                if (onContextItemsNeeded == null)
                    onContextItemsNeeded += value;
                else if (onContextItemsNeeded != value)
                {
                    foreach (var d in onContextItemsNeeded.GetInvocationList())
                    {
                        onContextItemsNeeded -= (d as EventHandler<NeedContextItemsArgs>);
                    }
                    onContextItemsNeeded += value;
                }
            }
            remove
            {
                onContextItemsNeeded -= value;
            }
        }

        /// <summary>
        /// Returns true if the intellisense is visible. False otherwise.
        /// </summary>
        public bool Visible { get { return view.Visible; } }

        /// <summary>
        /// Editor being used. Mainly used so that the view has a reference to the top-level window,
        /// which it needs in order to do coordinate-related calculations.
        /// </summary>
        public ViewBase Editor
        {
            get
            {
                return view.Editor;
            }
            set
            {
                view.Editor = value;
            }
        }

        /// <summary>
        /// Detaches all handlers from an event.
        /// </summary>
        /// <typeparam name="T">Type of the event.</typeparam>
        /// <param name="handler">Event for which handlers should be removed. Maybe handler is not such a good name.</param>
        public void DetachHandlers<T>(ref EventHandler<T> handler)
        {
            if (handler == null)
                return;
            foreach (EventHandler<T> anonymousHandler in handler.GetInvocationList())
            {
                handler -= anonymousHandler;
            }
        }

        /// <summary>
        /// Generates completion options for a report. This should also work for the property presenter.
        /// </summary>
        /// <param name="code">Source code.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns>True if any completion options are found. False otherwise.</returns>
        public bool GenerateGridCompletions(string cellContents, int offset, IModel model, bool properties, bool methods, bool events, bool controlSpace = false)
        {
            // TODO : Perhaps there should be a separate intellisense class for grid completions?
            string contentsToCursor = cellContents.Substring(0, offset);

            // Remove any potential trailing period.
            contentsToCursor = contentsToCursor.TrimEnd('.');

            // Ignore everything before the most recent comma.
            string objectName = contentsToCursor.Substring(contentsToCursor.LastIndexOf(',') + 1);

            // Set the trigger word for later use.
            triggerWord = controlSpace ? GetTriggerWord(objectName) : string.Empty;

            // Ignore everything before most recent model name in square brackets.
            // I'm assuming that model/node names cannot start with a number.
            string modelNamePattern = @"\[([A-Za-z]+[A-Za-z0-9]*)\]";
            var matches = System.Text.RegularExpressions.Regex.Matches(cellContents, modelNamePattern);
            if (matches.Count > 0)
            {
                int modelNameIndex = objectName.LastIndexOf(matches[matches.Count - 1].Value);
                if (modelNameIndex >= 0)
                    objectName = objectName.Substring(modelNameIndex);
            }
            
            List<NeedContextItemsArgs.ContextItem> results = NeedContextItemsArgs.ExamineModelForContextItemsV2(model as Model, objectName, properties, methods, events);
            view.Populate(results);
            return results.Any();
        }

        /// <summary>
        /// Generates completion options for a manager script.
        /// </summary>
        /// <param name="code">Source code.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns>True if any completion options are found. False otherwise.</returns>
        public bool GenerateScriptCompletions(string code, int offset, bool controlSpace = false)
        {
            CSharpParser parser = new CSharpParser();
            SyntaxTree syntaxTree = parser.Parse(code);
            string fileName = Path.GetTempFileName();
            if (!File.Exists(fileName))
                File.Create(fileName).Close();
            File.WriteAllText(fileName, code);
            syntaxTree.FileName = fileName;
            syntaxTree.Freeze();

            // Should probably take into account which namespaces the user is using and load the needed assemblies into the CSharpCompletion object
            // string usings = syntaxTree.Descendants.OfType<UsingDeclaration>().Select(x => x.ToString()).Aggregate((x, y) => x + /* Environment.NewLine + */ y);

            IDocument document = new ReadOnlyDocument(new StringTextSource(code), syntaxTree.FileName);
            completionResult = completion.GetCompletions(document, offset, controlSpace);

            // Set the trigger word for later use.
            triggerWord = controlSpace ? completionResult.TriggerWord : string.Empty;

            // If the user pressed control space, we assume they are trying to generate completions for a partially typed word.
            // In this situation we need to filter the results based on what they have already typed.
            // The exception is if the most recent character is a period. 
            // No idea why NRefactory can't do this for us.
            if (controlSpace && !string.IsNullOrEmpty(completionResult.TriggerWord) && code[offset - 1] != '.')
            {
                // Filter items.
                completionResult.CompletionData = completionResult.CompletionData.Where(item => GetMatchQuality(item.CompletionText, completionResult.TriggerWord) > 0).ToList();
            }
            List<CompletionData> completionList = completionResult.CompletionData.Select(x => x as CompletionData).Where(x => x != null).OrderBy(x => x.CompletionText).ToList();
            view.Populate(completionList);
            if (controlSpace && !string.IsNullOrEmpty(completionResult.TriggerWord))
                view.SelectItem(completionList.IndexOf(completionList.OrderByDescending(x => GetMatchQuality(x.CompletionText, completionResult.TriggerWord)).FirstOrDefault()));
            return completionList.Any();
        }

        public bool GenerateSeriesCompletions(string text, int offset, string tableName, IStorageReader storage)
        {
            triggerWord = text?.Substring(0, offset).Split(' ').Last().Replace("[", "").Replace("]", "");
            
            List<string> columnNames = storage.ColumnNames(tableName).ToList();
            List<NeedContextItemsArgs.ContextItem> intellisenseOptions = new List<NeedContextItemsArgs.ContextItem>();
            foreach (string columnName in columnNames)
            {
                if (string.IsNullOrEmpty(triggerWord) || string.IsNullOrEmpty(triggerWord.Replace("[", "").Replace("]", "")) || columnName.StartsWith(triggerWord.Replace("[", "").Replace("]", "")))
                intellisenseOptions.Add(new NeedContextItemsArgs.ContextItem()
                {
                    Name = columnName,
                    Units = string.Empty,
                    TypeName = string.Empty,
                    Descr = string.Empty,
                    ParamString = string.Empty
                });
            }
            if (intellisenseOptions.Any())
                view.Populate(intellisenseOptions);
            return intellisenseOptions.Any();
        }

        /// <summary>
        /// Generates the intellisense options.
        /// After calling this, call <see cref="Show(int, int, int)"/> to show the intellisense popup.
        /// </summary>
        /// <param name="code">Source code.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns></returns>
        public bool GenerateCompletionOptions(string code, int offset, bool controlSpace = false)
        {
            if (Editor?.Owner is ReportView)
                return GenerateReportCompletions(code, offset, controlSpace);
            else
                return GenerateScriptCompletions(code, offset, controlSpace);
        }

        /// <summary>
        /// Displays the intellisense popup at the given coordinates.
        /// </summary>
        /// <param name="x">x-coordinate at which the popup will be displayed.</param>
        /// <param name="y">y-coordinate at which the popup will be displayed.</param>
        /// <param name="lineHeight">Line height (in px?).</param>
        public void Show(int x, int y, int lineHeight = 17)
        {
            view.SmartShowAtCoordinates(x, y, lineHeight);
            if (completionResult != null && completionResult.SuggestedCompletionDataItem != null)
            {
                int index = completionResult.CompletionData.IndexOf(completionResult.SuggestedCompletionDataItem);
                if (index >= 0)
                    view.SelectItem(index);
            }
        }

        /// <summary>
        /// Unsubscribes events, releases unmanaged resources, all that fun stuff.
        /// </summary>
        public void Cleanup()
        {
            view?.Cleanup();
        }

        /// <summary>
        /// Gets the trigger word - the partially completed word for which the user wants completion options.
        /// </summary>
        /// <param name="textBeforeCursor">Text before the cursor.</param>
        /// <returns></returns>
        private string GetTriggerWord(string textBeforeCursor)
        {
            if (!textBeforeCursor.Contains("."))
                return textBeforeCursor;

            // Return a substring starting just after the last period in the string.
            return textBeforeCursor.Substring(textBeforeCursor.LastIndexOf('.') + 1);
        }

        /// <summary>
        /// Determines how well an item matches the completion word.
        /// </summary>
        /// <param name="itemText">Name of a completion option.</param>
        /// <param name="query">The completion word.</param>
        /// <returns>
        /// Number representing the quality of the match. Higher numbers represent closer matches.
        /// 8 represents an exact match, -1 represents no match.
        /// </returns>
        private int GetMatchQuality(string itemText, string query)
        {
            if (itemText == null)
                throw new ArgumentNullException("itemText", "ICompletionData.Text returned null");

            // Qualities:
            //  	8 = full match case sensitive
            // 		7 = full match
            // 		6 = match start case sensitive
            //		5 = match start
            //		4 = match CamelCase when length of query is 1 or 2 characters
            // 		3 = match substring case sensitive
            //		2 = match substring
            //		1 = match CamelCase
            //		-1 = no match

            if (query == itemText)
                return 8;
            if (string.Equals(itemText, query, StringComparison.InvariantCultureIgnoreCase))
                return 7;

            if (itemText.StartsWith(query, StringComparison.InvariantCulture))
                return 6;
            if (itemText.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
                return 5;

            bool? camelCaseMatch = null;
            if (query.Length <= 2)
            {
                camelCaseMatch = CamelCaseMatch(itemText, query);
                if (camelCaseMatch == true) return 4;
            }
            
            if (itemText.IndexOf(query, StringComparison.InvariantCulture) >= 0)
                return 3;
            if (itemText.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
                return 2;

            if (!camelCaseMatch.HasValue)
                camelCaseMatch = CamelCaseMatch(itemText, query);
            if (camelCaseMatch == true)
                return 1;

            return -1;
        }

        /// <summary>
        /// Checks if two strings match on the upper case letters.
        /// e.g. "CodeQualityAnalysis" matches "CQ".
        /// </summary>
        /// <param name="text">The camel case word.</param>
        /// <param name="query">The acronym/abbreviated word to test.</param>
        /// <returns>True if the upper case letters match, false otherwise.</returns>
        private bool CamelCaseMatch(string text, string query)
        {
            int i = 0;
            foreach (char upper in text.Where(c => char.IsUpper(c)))
            {
                if (i > query.Length - 1)
                    return true;    // return true here for CamelCase partial match ("CQ" matches "CodeQualityAnalysis")
                if (char.ToUpper(query[i], CultureInfo.InvariantCulture) != upper)
                    return false;
                i++;
            }
            if (i >= query.Length)
                return true;
            return false;
        }

        /// <summary>
        /// Generates completion options for a report. This should also work for the property presenter.
        /// </summary>
        /// <param name="code">Source code.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns>True if any completion options are found. False otherwise.</returns>
        private bool GenerateReportCompletions(string code, int offset, bool controlSpace = false)
        {
            //var completionOptions = NeedContextItemsArgs.ExamineModelForNames(model, e.ObjectName, true, true, false);
            string currentLine = code.Split(Environment.NewLine.ToCharArray()).Last();
            ViewBase currentView = Editor;
            while (!(currentView is ExplorerView))
            {
                currentView = currentView.Owner;
            }
            ExplorerView mainView = currentView as ExplorerView;
            return false;
        }
    }
}
