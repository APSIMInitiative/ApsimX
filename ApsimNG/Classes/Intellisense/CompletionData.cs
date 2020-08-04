using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Completion;
using UserInterface.Interfaces;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// A class to store data such as type, units, description, etc. about a completion option.
    /// </summary>
    public class CompletionData : ICompletionData, ICompletionItem
    {
        /// <summary>
        /// The overloaded versions of this member.
        /// </summary>
        private readonly List<ICompletionData> overloadedData = new List<ICompletionData>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CompletionData()
        {
            // Do nothing.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">Completion text to be displayed.</param>
        public CompletionData(string text)
        {
            DisplayText = CompletionText = Description = text;
        }

        /// <summary>
        /// The word in the text editor for which we are attempting to generate completion options.
        /// </summary>
        public string TriggerWord { get; set; }

        /// <summary>
        /// Length of <see cref="TriggerWord"/>.
        /// </summary>
        public int TriggerWordLength { get; set; }

        /// <summary>
        /// No idea what this is; it's not used, but we need to have it in order to implement <see cref="ICompletionData"/>.
        /// </summary>
        public CompletionCategory CompletionCategory { get; set; }

        /// <summary>
        /// This seems to be very similar to <see cref="CompletionText"/>, except that this sometimes includes the namespace.
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// The description text of the member, including the member's definition.
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// The name of the completion option - e.g. ApsimVersion
        /// </summary>
        public string CompletionText { get; set; }

        /// <summary>
        /// This lets us define flags which control how the completion text is displayed (e.g. bold).
        /// This is currently unused, but we need to define this property in order to implement <see cref="ICompletionData"/>.
        /// </summary>
        public DisplayFlags DisplayFlags { get; set; }

        /// <summary>
        /// True if the member has overloads. False otherwisee.
        /// </summary>
        public bool HasOverloads
        {
            get { return overloadedData.Count > 0; }
        }

        /// <summary>
        /// Gets the overloaded versions of this member.
        /// </summary>
        public IEnumerable<ICompletionData> OverloadedData
        {
            get { return overloadedData; }
        }

        /// <summary>
        /// Adds a new item to the list of overloaded versions of this member.
        /// </summary>
        /// <param name="data">Overloaded item to be added to the list.</param>
        public void AddOverload(ICompletionData data)
        {
            if (overloadedData.Count == 0)
                overloadedData.Add(this);
            overloadedData.Add(data);
        }

        /// <summary>
        /// The icon for this completion image, with any necessary overlays.
        /// </summary>
        public Gdk.Pixbuf Image { get; set; }
        
        /// <summary>
        /// Quality of the match against the <see cref="TriggerWord"/>.
        /// Higher priority means this completion option matches more closely.
        /// </summary>
        private double priority = 1;

        /// <summary>
        /// Gets or sets the quality of the match against the <see cref="TriggerWord"/>.
        /// </summary>
        public virtual double Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        /// <summary>
        /// Units of the member (e.g. kg/ha).
        /// </summary>
        public string Units { get; protected set; }

        /// <summary>
        /// Gets or sets the return type of a function or the type of a variable.
        /// </summary>
        public string ReturnType { get; protected set; }

        /// <summary>
        /// Gets the type of a variable. Currently unused, but may be useful if we 
        /// decide we want to view the namespace as well as the type.
        /// </summary>
        /// <param name="withNamespace"></param>
        /// <returns></returns>
        public string Type(bool withNamespace = false)
        {
            if (this is EntityCompletionData)
                return (this as EntityCompletionData).GetReturnType(withNamespace);
            if (this is VariableCompletionData)
                return withNamespace ? (this as VariableCompletionData).Variable.Type.FullName : (this as VariableCompletionData).Variable.Type.Name;

            return "Unknown";
        }

        public bool IsMethod
        {
            get
            {
                if (this is EntityCompletionData)
                    return (this as EntityCompletionData).Entity.SymbolKind == ICSharpCode.NRefactory.TypeSystem.SymbolKind.Method;
                return false;
            }
        }
    }
}
