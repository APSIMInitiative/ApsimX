// -----------------------------------------------------------------------
// <copyright file="FactorPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using EventArguments;
    using Models.Core;
    using Models.Factorial;
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
        private IEditorView factorView;

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
            this.factorView = view as IEditorView;
            this.presenter = explorerPresenter;
            intellisense = new IntellisensePresenter(factorView as ViewBase);
            this.factorView.Lines = this.factor.Specifications.ToArray();

            this.factorView.TextHasChangedByUser += this.OnTextHasChangedByUser;
            this.factorView.ContextItemsNeeded += this.OnContextItemsNeeded;
            this.presenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the objects
        /// </summary>
        public void Detach()
        {
            intellisense.Cleanup();
            factorView.TextHasChangedByUser -= this.OnTextHasChangedByUser;
            factorView.ContextItemsNeeded -= this.OnContextItemsNeeded;
            presenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Intellisense lookup.
        /// </summary>
        /// <param name="sender">The menu item</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            if (e.ObjectName == string.Empty)
            {
                e.ObjectName = ".";
            }

            try
            {
                string currentLine = GetLine(e.Code, e.LineNo - 1);
                if (intellisense.GenerateGridCompletions(currentLine, e.ColNo, factor, true, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.Item1, e.Coordinates.Item2);
                intellisense.ItemSelected += (o, args) =>
                {
                    if (args.ItemSelected == string.Empty)
                        (sender as IEditorView).InsertAtCaret(args.ItemSelected);
                    else
                        (sender as IEditorView).InsertCompletionOption(args.ItemSelected, args.TriggerWord);
                };
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
                presenter.CommandHistory.ModelChanged -= OnModelChanged;
                List<string> newPaths = new List<string>();
                foreach (string line in factorView.Lines)
                {
                    if (line != string.Empty)
                    {
                        newPaths.Add(line);
                    }
                }

                presenter.CommandHistory.Add(new Commands.ChangeProperty(factor, "Specifications", newPaths));
                presenter.CommandHistory.ModelChanged += OnModelChanged;
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
            factorView.Lines = factor.Specifications.ToArray();
        }
    }
}
