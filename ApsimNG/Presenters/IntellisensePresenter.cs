using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using UserInterface.Views;
using UserInterface.Intellisense;
using ICSharpCode.NRefactory.Completion;
using UserInterface.EventArguments;
using System.Collections.ObjectModel;

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
        /// List of intellisense options.
        /// </summary>
        public List<ICompletionData> CompletionOptions { get; private set; }

        // TODO : find a better solution.
        public Gtk.Window MainWindow
        {
            get
            {
                return view.MainWindow;
            }
            set
            {
                view.MainWindow = value;
            }
        }
        /// <summary>
        /// Generates the intellisense popup.
        /// </summary>
        /// <param name="node"></param>
        public bool GenerateCompletionOptions()
        {
            // TODO : take a potential period into account   
            NeedContextItemsArgs args = new NeedContextItemsArgs
            {
                ObjectName = "",
                Items = new List<string>(),
                AllItems = new List<NeedContextItemsArgs.ContextItem>(),
                CompletionData = new List<ICompletionData>()
            };
            onContextItemsNeeded?.Invoke(this, args);
            CompletionOptions = args.CompletionData;
            //FilterCompletionData(args.ObjectName);
            //SelectItemFiltering(node);
            view.Populate(CompletionOptions);
            return CompletionOptions.Count > 0;
        }

        public void Show(int x, int y, int lineHeight = 17)
        {
            view.SmartShowAtCoordinates(x, y, lineHeight);
        }

        public void Cleanup()
        {
            view.Cleanup();
        }

        /// <summary>
        /// Takes a list of completion data and filters out the records irrelevant to the word which the user has just typed.
        /// </summary>
        /// <param name="wordToMatch"></param>
        /// <returns></returns>
        private void FilterCompletionData(string wordToMatch)
        {
            CompletionOptions = CompletionOptions.Where(x => GetMatchQuality(x.CompletionText, wordToMatch) > 0).ToList();
        }
        /// <summary>
        /// Filters CompletionList items to show only those matching given query, and selects the best match.
        /// </summary>
        void SelectItemFiltering(string query)
        {
            // if the user just typed one more character, don't filter all data but just filter what we are already displaying
            var listToFilter = CompletionOptions;

            var matchingItems =
                from item in listToFilter
                let quality = GetMatchQuality((item as CompletionData).Text, query)
                where quality > 0
                select new { Item = item, Quality = quality };

            // e.g. "DateTimeKind k = (*cc here suggests DateTimeKind*)"
            ICompletionData suggestedItem = null;

            var listBoxItems = new ObservableCollection<ICompletionData>();
            int bestIndex = -1;
            int bestQuality = -1;
            double bestPriority = 0;
            int i = 0;
            foreach (var matchingItem in matchingItems)
            {
                double priority = matchingItem.Item == suggestedItem ? double.PositiveInfinity : (matchingItem.Item as CompletionData).Priority;
                int quality = matchingItem.Quality;
                if (quality > bestQuality || (quality == bestQuality && (priority > bestPriority)))
                {
                    bestIndex = i;
                    bestPriority = priority;
                    bestQuality = quality;
                }
                listBoxItems.Add(matchingItem.Item);
                i++;
            }
            CompletionOptions = listBoxItems.ToList();
        }

        /// <summary>
        /// Evaluates how closely two strings match and returns an integer representing the quality of the match.
        /// </summary>
        /// <param name="itemText"></param>
        /// <param name="query"></param>
        /// <returns>
        /// Qualities:
        ///  	8 = full match case sensitive
        /// 	7 = full match
        /// 	6 = match start case sensitive
        ///		5 = match start
        ///		4 = match CamelCase when length of query is 1 or 2 characters
        /// 	3 = match substring case sensitive
        ///		2 = match substring
        ///		1 = match CamelCase
        ///	   -1 = no match
        /// TODO : create an enum that represents these values?
        /// </returns>
        private int GetMatchQuality(string itemText, string query)
        {
            if (itemText == null)
                throw new ArgumentNullException("itemText", "ICompletionData.Text returned null");
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

            // search by substring
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
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="query"></param>
        /// <returns></returns>
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
    }
}
