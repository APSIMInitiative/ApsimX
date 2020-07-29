#if NETCOREAPP
namespace UserInterface.Intellisense
{
    using System;
    using System.Linq;
    using Gdk;
    using GLib;
    using EventArguments;
    using Gtk;
    using GtkSource;
    using Presenters;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Views;
    using System.Reflection.Metadata;

    internal class ScriptCompletionProvider : GLib.Object, ICompletionProvider, ICompletionProviderImplementor
    {
        /// <summary>
        /// A function which will display an error to the user.
        /// </summary>
        private Action<Exception> ShowError;

        /// <summary>
        /// The code editor widget.
        /// </summary>
        private SourceView view;

        /// <summary>
        /// The code completion service. This object handles generation
        /// of code completion/intellisense items.
        /// </summary>
        private CodeCompletionService service = new CodeCompletionService();

        /// <summary>
        /// A popup window which shows method signature info.
        /// </summary>
        private CompletionInfo methodSignaturePopup;

        /// <summary>
        /// Get with what kind of activation the provider should be activated.
        /// </summary>
        /// <remarks>
        /// Basically - what causes the intellisense to show up?
        /// </remarks>
        public CompletionActivation Activation
        {
            get
            {
                return CompletionActivation.Interactive | CompletionActivation.UserRequested;
            }
        }

        /// <summary>
        /// Gets the GIcon for the icon of the provider.
        /// </summary>
        /// <remarks>
        /// The icon may be specified as a GdkPixbuf, as an icon name or as a
        /// GIcon by implementing the corresponding get function. At most one
        /// of those get functions should return a value different from NULL,
        /// if they all return NULL no icon will be used.
        /// </remarks>
        public IIcon Gicon { get { return null; } }

        /// <summary>
        /// Get the GdkPixbuf for the icon of the provider.
        /// </summary>
        /// <remarks>
        /// The icon may be specified as a GdkPixbuf, as an icon name or as a
        /// GIcon by implementing the corresponding get function. At most one
        /// of those get functions should return a value different from NULL,
        /// if they all return NULL no icon will be used.
        /// </remarks>
        public Pixbuf Icon { get { return null; } }

        /// <summary>
        /// Gets the icon name.
        /// </summary>
        public string IconName { get { return null; } }

        /// <summary>
        /// Get the delay in milliseconds before starting interactive
        /// completion for this provider. A value of -1 indicates to use the
        /// default value as set by the “auto-complete-delay” property.
        /// </summary>
        public int InteractiveDelay
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Get the name of the provider. This should be a translatable name
        /// for display to the user. For example: _("Document word completion
        /// provider"). The returned string must be freed with g_free().
        /// </summary>
        public string Name
        {
            get
            {
                return null;//"Suggestions";
            }
        }

        /// <summary>
        /// Get the provider priority. The priority determines the order in
        /// which proposals appear in the completion popup. Higher priorities
        /// are sorted before lower priorities. The default priority is 0.
        /// </summary>
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ShowError">A function which will display an error to the user.</param>
        /// <param name="view">The SourceView widget. Used in creation of method signature popup.</param>
        public ScriptCompletionProvider(Action<Exception> ShowError, SourceView view) : base()
        {
            this.ShowError = ShowError;
            this.view = view;
            view.Completion.ShowHeaders = false;
        }

        /// <summary>
        /// Activate proposal at iter. When this functions returns FALSE, the
        /// default activation of proposal will take place which replaces the
        /// word at iter with the text of proposal (see
        /// gtk_source_completion_proposal_get_text()).
        /// 
        /// Here is how the default activation selects the boundaries of the
        /// word to replace. The end of the word is iter . For the start of the
        /// word, it depends on whether a start iter is defined for proposal
        /// (see gtk_source_completion_provider_get_start_iter()). If a start
        /// iter is defined, the start of the word is the start iter. Else, the
        /// word (as long as possible) will contain only alphanumerical and the
        /// "_" characters.
        /// </summary>
        /// <param name="proposal"></param>
        /// <param name="iter"></param>
        /// <returns></returns>
        public bool ActivateProposal(ICompletionProposal proposal, TextIter iter)
        {
            if (proposal is CompletionProposalAdapter adapter && adapter.Implementor is CustomScriptCompletionProposal prop && prop.Item.IsMethod)
            {
                // The user has just activated a completion item which is a method.
                // Let's show the method signature in a popup window.

                // First, remove any existing method signature popups.
                if (methodSignaturePopup != null)
                {
                    methodSignaturePopup.Hide();
                    methodSignaturePopup.Dispose();
                }

                // TBI - parameter names, descriptions.
                // Also need to look into manual activation of the popup (ie
                // ctrl + shift + space).
                methodSignaturePopup = new CompletionInfo();
                methodSignaturePopup.Add(new Label(proposal.Info));

                // We need to attach the popup window to the sourceview, so it
                // gets hidden/removed when the sourceview loses focus.
                methodSignaturePopup.AttachedTo = view;
                view.KeyPressEvent += OnCompletionInfoKeyPress;

                // Move the info window to the location of the cursor.
                methodSignaturePopup.MoveToIter(view, iter);
                methodSignaturePopup.ShowAll();
            }

            // Return false, to allow the sourceview to automatically handle
            // insertion of the selected completion item.
            return false;
        }

        [GLib.ConnectBefore]
        private void OnCompletionInfoKeyPress(object o, KeyPressEventArgs args)
        {
            try
            {
                if (args.Event.Key == Gdk.Key.Escape)
                {
                    methodSignaturePopup.Hide();
                    methodSignaturePopup.Dispose();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Get a customized info widget to show extra information of a
        /// proposal. This allows for customized widgets on a proposal basis,
        /// although in general providers will have the same custom widget for
        /// all their proposals and proposal can be ignored. The implementation
        /// of this function is optional.
        /// 
        /// If this function is not implemented, the default widget is a
        /// GtkLabel. The return value of
        /// gtk_source_completion_proposal_get_info() is used as the content of
        /// the GtkLabel.
        /// </summary>
        /// <param name="proposal"></param>
        /// <returns></returns>
        public Widget GetInfoWidget(ICompletionProposal proposal)
        {
            // tbi
            return null;
        }

        /// <summary>
        /// Get the GtkTextIter at which the completion for proposal starts.
        /// When implemented, this information is used to position the
        /// completion window accordingly when a proposal is selected in the
        /// completion window. The proposal text inside the completion window
        /// is aligned on iter.
        ///
        /// If this function is not implemented, the word boundary is taken to
        /// position the completion window. See
        /// gtk_source_completion_provider_activate_proposal() for an
        /// explanation on the word boundaries.
        ///
        /// When the proposal is activated, the default handler uses iter as
        /// the start of the word to replace. See
        /// gtk_source_completion_provider_activate_proposal() for more
        /// information.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="proposal"></param>
        /// <param name="iter"></param>
        /// <returns></returns>
        public bool GetStartIter(CompletionContext context, ICompletionProposal proposal, TextIter iter)
        {
            return false;
        }

        /// <summary>
        /// Get whether the provider match the context of completion detailed
        /// in context.
        /// </summary>
        /// <param name="context">Completion context.</param>
        /// <remarks>
        /// If implemented, gtk_source_completion_provider_update_info() must
        /// also be implemented.
        /// </remarks>
        public bool Match(CompletionContext context)
        {
            // tbi - for now just match every context.
            return context.Completion.View.IsRealized;
        }

        /// <summary>
        /// Populate context with proposals from provider added with the
        /// gtk_source_completion_context_add_proposals() function.
        /// </summary>
        /// <param name="context"></param>
        public void Populate(CompletionContext context)
        {
            try
            {
                string code = context.Iter.Buffer.Text;
                int offset = context.Iter.Buffer.CursorPosition;

                Task<IEnumerable<NeedContextItemsArgs.ContextItem>> task = service.GetCompletionItemsAsync(code, offset);
                task.Wait();
                if (task.Result == null)
                    return;

                IEnumerable<NeedContextItemsArgs.ContextItem> contextItems = task.Result.OrderBy(c => c.Name);
                List proposals = new List(contextItems.Select(c => new CompletionProposalAdapter(new CustomScriptCompletionProposal(c))).ToArray(),
                                          typeof(CompletionProposalAdapter),
                                          true,
                                          true);
                context.AddProposals(this, proposals, true);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Update extra information shown in info for proposal.
        /// </summary>
        /// <param name="proposal">Completion proposal.</param>
        /// <param name="info">Completion information.</param>
        /// <remarks>
        /// This function must be implemented when
        /// gtk_source_completion_provider_get_info_widget() is implemented.
        /// </remarks>
        public void UpdateInfo(ICompletionProposal proposal, CompletionInfo info)
        {
            // Not using this feature (for now at least).
            return;
        }
    }
}
#endif