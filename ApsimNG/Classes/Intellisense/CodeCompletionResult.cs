using System.Collections.Generic;
using ICSharpCode.NRefactory.Completion;

namespace UserInterface.Intellisense
{
    public class CodeCompletionResult
    {
        /// <summary>
        /// List of completion options.
        /// </summary>
        public List<ICompletionData> CompletionData = new List<ICompletionData>();

        /// <summary>
        /// The 'recommended' option - the option that most closesly matches the <see cref="TriggerWord"/>.
        /// </summary>
        public ICompletionData SuggestedCompletionDataItem;

        /// <summary>
        /// Length of <see cref="TriggerWord"/>.
        /// </summary>
        public int TriggerWordLength;

        /// <summary>
        /// The word which we are generating completion options for.
        /// </summary>
        public string TriggerWord;

        /// <summary>
        /// Default overload provider.
        /// </summary>
        public IOverloadProvider OverloadProvider;
    }
}
