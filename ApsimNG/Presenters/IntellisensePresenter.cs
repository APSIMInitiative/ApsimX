using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UserInterface.Views;
using UserInterface.Intellisense;
using UserInterface.EventArguments;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.CSharp;
using System.Reflection;
using Models.Core;
using System.Globalization;

namespace UserInterface.Presenters
{
    public class IntellisensePresenter
    {
        /// <summary>
        /// The view responsible for displaying the intellisense options on the screen.
        /// </summary>
        private IntellisenseView view = new IntellisenseView();

        /// <summary>
        /// Fired when an item is selected in the intellisense window.
        /// </summary>
        private event EventHandler<NeedContextItemsArgs> onContextItemsNeeded;

        /// <summary>
        /// Responsible for generating the completion options.
        /// </summary>
        private CSharpCompletion completion = new CSharpCompletion();
        
        /// <summary>
        /// List of intellisense options.
        /// This list isn't really needed at the moment, but I have plans to use it in the future. - DH May 2018
        /// </summary>
        private CodeCompletionResult completionResult;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IntellisensePresenter()
        {
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
                view.ItemSelected += value;
            }
            remove
            {
                view.ItemSelected -= value;
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
        /// Generates the intellisense popup.
        /// </summary>
        /// <param name="node"></param>
        public bool GenerateCompletionOptions()
        {
            NeedContextItemsArgs args = new NeedContextItemsArgs
            {
                ObjectName = "",
                Items = new List<string>(),
                AllItems = new List<NeedContextItemsArgs.ContextItem>(),
                CompletionData = new List<CompletionData>()
            };
            onContextItemsNeeded?.Invoke(this, args);
            List<CompletionData> completionList = args.CompletionData.OrderBy(x => x.CompletionText).ToList();
            view.Populate(completionList);
            return completionList.Count > 0;
        }

        /// <summary>
        /// Generates the intellisense options.
        /// After calling this, call <see cref="Show(int, int, int)"/> to show the intellisense popup.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public bool GenerateCompletionOptions(string code, int offset, bool controlSpace = false)
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
            
            if (controlSpace && !string.IsNullOrEmpty(completionResult.TriggerWord))
            {
                // Filter items.
                completionResult.CompletionData = completionResult.CompletionData.Where(item => GetMatchQuality(item.CompletionText, completionResult.TriggerWord) > 0).ToList();
            }
            List<CompletionData> completionList = completionResult.CompletionData.Select(x => x as CompletionData).Where(x => x != null).OrderBy(x => x.CompletionText).ToList();
            view.Populate(completionList);
            if (controlSpace && !string.IsNullOrEmpty(completionResult.TriggerWord))
                view.SelectItem(completionList.Max(x => GetMatchQuality(x.CompletionText, completionResult.TriggerWord)));
            return completionList.Any();
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
            int index = completionResult.CompletionData.IndexOf(completionResult.SuggestedCompletionDataItem);
            if (index >= 0)
                view.SelectItem(index);
        }

        /// <summary>
        /// Unsubscribes events, releases unmanaged resources, all that fun stuff.
        /// </summary>
        public void Cleanup()
        {
            view.Cleanup();
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
        int GetMatchQuality(string itemText, string query)
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
        static bool CamelCaseMatch(string text, string query)
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
    }
}
