namespace UserInterface.Presenters
{
    using System.IO;
    using Models;
    using Views;
    using System;
    using Interfaces;
    using APSIM.Shared.Utilities;
    using Commands;

    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class MemoPresenter : IPresenter
    {
        /// <summary>The memo model.</summary>
        private Memo memoModel;

        /// <summary>The explorer presenter used.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The markdown view.</summary>
        private MarkdownView markdownView;

        /// <summary>The raw text view.</summary>
        private TextInputView textView;

        /// <summary>The edit button.</summary>
        private ButtonView editButton;

        /// <summary>The help button.</summary>
        private ButtonView helpButton;

        /// <summary>
        /// Attach the 'Model' and the 'View' to this presenter.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view object</param>
        /// <param name="parentPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            memoModel = model as Memo;
            explorerPresenter = parentPresenter;

            markdownView = (view as ViewBase).GetControl<MarkdownView>("markdownView");
            textView = (view as ViewBase).GetControl<TextInputView>("textEditor");
            editButton = (view as ViewBase).GetControl<ButtonView>("editButton");
            helpButton = (view as ViewBase).GetControl<ButtonView>("helpButton");
            helpButton.Clicked += HelpBtnClicked;
            textView.Visible = false;
            textView.WrapText = true;
            textView.ModifyFont(Utility.Configuration.Settings.EditorFontName);
            textView.Text = memoModel.Text;
            textView.Changed += OnTextHasChanged;
            markdownView.ImagePath = Path.GetDirectoryName(explorerPresenter.ApsimXFile.FileName);
            markdownView.Text = memoModel.Text;
            editButton.Clicked += OnEditButtonClick;
            helpButton.Visible = false;
        }

        private void HelpBtnClicked(object sender, EventArgs e)
        {
            try
            {
                string url = "https://apsimnextgeneration.netlify.com/usage/memo/";
                if (ProcessUtilities.CurrentOS.IsLinux)
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "xdg-open";
                    process.StartInfo.Arguments = url;
                    process.Start();
                }
                else if (ProcessUtilities.CurrentOS.IsMac)
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = "open";
                    process.StartInfo.Arguments = url;
                    process.Start();
                }
                else
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = url;
                    process.Start();
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has changed the text in the editable textview.
        /// We need to update the live markdown preview.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnTextHasChanged(object sender, EventArgs e)
        {
            markdownView.Text = textView.Text;
        }

        /// <summary>User has clicked the edit button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnEditButtonClick(object sender, EventArgs e)
        {
            if (editButton.Text == "Edit")
            {
                editButton.Text = "Hide";
                textView.Visible = true;
                helpButton.Visible = true;
                if (textView.MainWidget.Parent is Gtk.Paned paned && paned.Position == 0)
                    paned.Position = paned.Allocation.Height / 2;
            }
            else
            {
                editButton.Text = "Edit";
                textView.Visible = false;
                helpButton.Visible = false;
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            editButton.Clicked -= OnEditButtonClick;
            helpButton.Clicked -= HelpBtnClicked;
            ICommand changeText = new ChangeProperty(memoModel, nameof(memoModel.Text), textView.Text);
            explorerPresenter.CommandHistory.Add(changeText);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The model object that has changed</param>
        public void OnModelChanged(object changedModel)
        {
            if (changedModel == this.memoModel)
                this.markdownView.Text = memoModel.Text;
        }
    }
}
