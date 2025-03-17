using System;
using Gdk;
using GLib;
using GtkSource;
using UserInterface.EventArguments;

namespace UserInterface.Intellisense
{



    /// <summary>
    /// Represents a completion item. Intended for use in a manager script but
    /// can probably be used elsewhere just fine.
    /// </summary>
    /// <remarks>
    /// The proposal interface represents a completion item in the completion
    /// window. It provides information on how to display the completion item
    /// and what action should be taken when the completion item is activated.
    ///
    /// The proposal is displayed in the completion window with a label and
    /// optionally an icon. The label may be specified using plain text or
    /// markup by implementing the corresponding get function. Only one of
    /// those get functions should return a value different from NULL. The icon
    /// may be specified as a GdkPixbuf, as an icon name or as a GIcon by
    /// implementing the corresponding get function. At most one of those get
    /// functions should return a value different from NULL, if they all return
    /// NULL no icon will be used.
    /// </remarks>
    internal class CustomScriptCompletionProposal : GLib.Object, ICompletionProposal, ICompletionProposalImplementor
    {
        private bool isProperty;

        /// <summary>
        /// Gets the GIcon for the icon of proposal.
        /// </summary>
        public IIcon Gicon
        {
            get
            {
                return null;
            }
        }

        private static readonly Pixbuf functionPixbuf = new(typeof(CustomScriptCompletionProposal).Assembly, "ApsimNG.Resources.Function.png", 16, 16);
        private static readonly Pixbuf propertyPixbuf = new(typeof(CustomScriptCompletionProposal).Assembly, "ApsimNG.Resources.Property.png", 16, 16);

        /// <summary>
        /// Gets the GdkPixbuf for the icon of proposal.
        /// </summary>
        public Pixbuf Icon
        {
            get => isProperty ? propertyPixbuf : functionPixbuf;
        }

        /// <summary>
        /// Gets the icon name of proposal.
        /// </summary>
        public string IconName
        {
            get
            {
                // tbi
                //return IsProperty ? "Property" : "Function";
                return "Icon Name";
            }
        }

        /// <summary>
        /// Gets extra information associated to the proposal. This information
        /// will be used to present the user with extra, detailed information
        /// about the selected proposal. The returned string must be freed with
        /// g_free().
        /// </summary>
        public string Info { get; private set; }

        /// <summary>
        /// Gets the label of proposal. The label is shown in the list of
        /// proposals as plain text. If you need any markup (such as bold or
        /// italic text), you have to implement
        /// gtk_source_completion_proposal_get_markup(). The returned string
        /// must be freed with g_free().
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Gets the label of proposal with markup. The label is shown in the
        /// list of proposals and may contain markup. This will be used instead
        /// of gtk_source_completion_proposal_get_label() if implemented. The
        /// returned string must be freed with g_free().
        /// </summary>
        public string Markup { get; private set; }

        /// <summary>
        /// Gets the text of proposal. The text that is inserted into the text
        /// buffer when the proposal is activated by the default activation.
        /// You are free to implement a custom activation handler in the
        /// provider and not implement this function. For more information, see
        /// gtk_source_completion_provider_activate_proposal(). The returned
        /// string must be freed with g_free().
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Additional info about the completion proposal.
        /// </summary>
        public NeedContextItemsArgs.ContextItem Item { get; private set; }

        public CustomScriptCompletionProposal(NeedContextItemsArgs.ContextItem item) : base()
        {
            isProperty = item.IsProperty;
            Label = item.Name;
            Text = item.Name;
            Markup = item.Name;
            Info = item.Descr;
            Item = item;
        }

        /// <summary>
        /// Invoked whenever the name, icon or info of the proposal have
        /// changed.
        /// </summary>
        public event EventHandler EmitChanged;

        /// <summary>
        /// Emits the "changed" signal on proposal. This should be called by
        /// implementations whenever the name, icon or info of the proposal has
        /// changed.
        /// </summary>
        public void FireChangedSignal()
        {
            EmitChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get whether two proposal objects are the same. This is used to
        /// (together with gtk_source_completion_proposal_hash()) to match
        /// proposals in the completion model. By default, it uses direct
        /// equality (g_direct_equal()).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equal(ICompletionProposal other)
        {
            return string.Equals(Text, other.Text, StringComparison.InvariantCulture);
            //return Equals(other);
        }

        /// <summary>
        /// Get the hash value of proposal. This is used to (together with
        /// gtk_source_completion_proposal_equal()) to match proposals in the
        /// completion model. By default, it uses a direct hash
        /// (g_direct_hash()).
        /// </summary>
        public uint Hash()
        {
            return Convert.ToUInt32(GetHashCode());
        }

        void ICompletionProposal.Changed()
        {
            throw new NotImplementedException();
        }
    }
}
