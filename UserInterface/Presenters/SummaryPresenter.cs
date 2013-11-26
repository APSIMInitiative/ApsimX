using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using UserInterface.Views;
using System.IO;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter class for working with HtmlView
    /// </summary>
    public class SummaryPresenter : IPresenter
    {
        private ISummary Summary;
        private ISummaryView View;
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Summary = model as ISummary;
            View = view as ISummaryView;
            CommandHistory = commandHistory;
            PopulateView();

            View.AutoCreateChanged += OnAutoCreateChanged;
            View.CreateButtonClicked += OnCreateButtonClicked;
            View.HTMLChanged += OnHTMLChanged;
            View.StateVariablesChanged += OnStateVariablesChanged;
            CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            View.AutoCreateChanged -= OnAutoCreateChanged;
            View.CreateButtonClicked -= OnCreateButtonClicked;
            View.HTMLChanged -= OnHTMLChanged;
            CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Populate the summary view.
        /// </summary>
        private void PopulateView()
        {
            View.AutoCreate = Summary.AutoCreate;
            View.html = Summary.html;
            View.StateVariables = Summary.StateVariables;

            Utility.Configuration configuration = new Utility.Configuration();
            string contents = Summary.GetSummary(configuration.SummaryPngFileName);
            View.SetSummary(contents, Summary.html);
        }

        /// <summary>
        /// User has changed the HTML state.
        /// </summary>
        private void OnHTMLChanged(object sender, EventArgs e)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(Summary, "html", View.html));
        }

        /// <summary>
        /// User has changed the auto create state.
        /// </summary>
        private void OnAutoCreateChanged(object sender, EventArgs e)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(Summary, "AutoCreate", View.AutoCreate));
        }

        /// <summary>
        /// User has changed the state variables state.
        /// </summary>
        void OnStateVariablesChanged(object sender, EventArgs e)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(Summary, "StateVariables", View.StateVariables));
        }

        /// <summary>
        /// User has clicked the create button.
        /// </summary>
        void OnCreateButtonClicked(object sender, EventArgs e)
        {
            Summary.CreateReportFile(baseline: false);
            Summary.CreateReportFile(baseline: true);
        }

        /// <summary>
        /// The model has changed probably because of undo/redo.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            if (changedModel == Summary)
                PopulateView();
        }

    }
}
