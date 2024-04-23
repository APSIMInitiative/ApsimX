﻿namespace UserInterface.Presenters
{
    using EventArguments;
    using Models.Factorial;
    using System;
    using System.Drawing;
    using Views;

    /// <summary>
    /// Connects a Factor model to a FactorView.
    /// </summary>
    public class FactorPresenter : IPresenter
    {
        /// <summary>
        /// The factor object
        /// </summary>
        private Factor factor;

        /// <summary>
        /// The view object
        /// </summary>
        private IFactorView factorView;

        /// <summary>
        /// The presenter
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// The intellisense object used to generate completion options.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.factor = model as Factor;
            this.factorView = view as IFactorView;
            this.presenter = explorerPresenter;
            intellisense = new IntellisensePresenter(factorView as ViewBase);
            this.factorView.Specification.Text = factor.Specification;

            this.factorView.Specification.Leave += this.OnTextHasChangedByUser;
            this.factorView.Specification.IntellisenseItemsNeeded += this.OnContextItemsNeeded;
            this.presenter.CommandHistory.ModelChanged += this.OnModelChanged;
            intellisense.ItemSelected += OnIntellisenseItemSelected;
        }

        /// <summary>
        /// Detach the objects
        /// </summary>
        public void Detach()
        {
            OnTextHasChangedByUser(this, new EventArgs());
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            factorView.Specification.Leave -= this.OnTextHasChangedByUser;
            factorView.Specification.IntellisenseItemsNeeded -= this.OnContextItemsNeeded;
            presenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Intellisense lookup.
        /// </summary>
        /// <param name="sender">The menu item</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            if (string.IsNullOrEmpty(e.ObjectName))
            {
                e.ObjectName = ".";
            }

            try
            {
                string currentLine = GetLine(e.Code, e.LineNo - 1);
                if (e.ControlShiftSpace)
                    intellisense.ShowMethodCompletion(factor, e.Code, e.Offset, new Point(e.Coordinates.X, e.Coordinates.Y));
                else if (intellisense.GenerateGridCompletions(currentLine, e.ColNo, factor, true, false, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Gets a specific line of text, preserving empty lines.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="lineNo">0-indexed line number.</param>
        /// <returns>String containing a specific line of text.</returns>
        /// <remarks>This method is a duplicate of ReportPresenter.GetLine().</remarks>
        private string GetLine(string text, int lineNo)
        {
            // string.Split(Environment.NewLine.ToCharArray()) doesn't work well for us on Windows - Mono.TextEditor seems 
            // to use unix-style line endings, so every second element from the returned array is an empty string.
            // If we remove all empty strings from the result then we also remove any lines which were deliberately empty.

            // TODO : move this to APSIM.Shared.Utilities.StringUtilities?
            string currentLine;
            using (System.IO.StringReader reader = new System.IO.StringReader(text))
            {
                int i = 0;
                while ((currentLine = reader.ReadLine()) != null && i < lineNo)
                {
                    i++;
                }
            }
            return currentLine;
        }

        /// <summary>
        /// User has changed the paths. Save to model.
        /// </summary>
        /// <param name="sender">The text control</param>
        /// <param name="e">Event arguments</param>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            try
            {
                if (factor.Specification != factorView.Specification.Text)
                {
                    presenter.CommandHistory.ModelChanged -= OnModelChanged;
                    presenter.CommandHistory.Add(new Commands.ChangeProperty(factor, "Specification", factorView.Specification.Text));
                    presenter.CommandHistory.ModelChanged += OnModelChanged;
                }
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The model has changed probably by an undo.
        /// </summary>
        /// <param name="changedModel">The model</param>
        private void OnModelChanged(object changedModel)
        {
            factorView.Specification.Text = factor.Specification;
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            if (string.IsNullOrEmpty(args.ItemSelected))
                factorView.Specification.InsertAtCursorInSquareBrackets(args.ItemSelected);
            else
                factorView.Specification.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
        }
    }
}
