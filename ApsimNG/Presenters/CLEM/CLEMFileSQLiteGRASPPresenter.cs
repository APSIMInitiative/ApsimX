
namespace UserInterface.Presenters
{
    using System;
    using Models;
    using Models.Core;
    using Views;

    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class CLEMFileSQLiteGRASPPresenter : IPresenter
    {
        /// <summary>
        /// The filecrop  model
        /// </summary>
        private Models.CLEM.FileSQLiteGRASP model;

        /// <summary>
        /// The filecrop view
        /// </summary>
        private ICLEMFileSQLiteGRASPView view;

        /// <summary>
        /// The Explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Starting Year for the grid.
        /// For speed purposes the grid will only display a few years worth of 
        /// GRASP data at a time instead of all of it.
        /// </summary>
        private int startYearForGrid;
        private int endYearForGrid;

        /// <summary>
        /// Number or years to display in the grid.
        /// </summary>
        private int numberOfYearsToDisplayInGrid = 4;

        /// <summary>
        /// First year in the SQLite database file
        /// Needed for setting bounds on StartYearForGrid
        /// </summary>
        private int firstYearInFile;

        /// <summary>
        /// Last year in the SQLite database file
        /// Needed for setting bounds on EndYearForGrid
        /// </summary>
        private int lastYearInFile;

        private void InitialiseStartYear()
        {
            if (this.model.FileExists)
            {
                //set the start year using SQLite file's data
                double[] yearsInFile = this.model.GetYearsInFile();
                firstYearInFile = (int)yearsInFile[0];
                lastYearInFile = (int)yearsInFile[yearsInFile.Length - 1];

                SetStartYear(firstYearInFile);
            }
        }

        private void SetStartYear(int year)
        {
            startYearForGrid = year;
            if (startYearForGrid >= lastYearInFile)
            {
                startYearForGrid = lastYearInFile - numberOfYearsToDisplayInGrid;
            }

            if (startYearForGrid < firstYearInFile)
            {
                startYearForGrid = firstYearInFile;
            }

            endYearForGrid = startYearForGrid + numberOfYearsToDisplayInGrid;

            if (endYearForGrid > lastYearInFile)
            {
                endYearForGrid = lastYearInFile;
            }
        }

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        /// <param name="model">The model to attach</param>
        /// <param name="view">The View to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Models.CLEM.FileSQLiteGRASP;
            this.view = view as ICLEMFileSQLiteGRASPView;
            this.explorerPresenter = explorerPresenter;
            this.view.BrowseButtonClicked += this.OnBrowseButtonClicked;
            this.view.BackButtonClicked += this.OnBackButtonClicked;
            this.view.NextButtonClicked += this.OnNextButtonClicked;

            InitialiseStartYear();

            this.OnModelChanged(model);  // Updates the view

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            this.view.BrowseButtonClicked -= this.OnBrowseButtonClicked;
            this.view.BackButtonClicked -= this.OnBackButtonClicked;
            this.view.NextButtonClicked -= this.OnNextButtonClicked;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Browse button was clicked by user.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">The params</param>
        private void OnBrowseButtonClicked(object sender, OpenDialogArgs e)
        {
            try
            {
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.model, "FullFileName", e.FileName));

                //reset the start year using new SQLite file's data.
                InitialiseStartYear();
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Next button was clicked by user.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">The params</param>
        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            try
            {
                SetStartYear(startYearForGrid + numberOfYearsToDisplayInGrid);
                this.OnModelChanged(model);
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Back button was clicked by user.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">The params</param>
        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            try
            {
                SetStartYear(startYearForGrid - numberOfYearsToDisplayInGrid);
                this.OnModelChanged(model);
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowError(err);
            }
        }


        /// <summary>
        /// The model has changed - update the view.
        /// ie.  Commands.ChangeProperty() has been called. 
        /// </summary>
        /// <param name="changedModel">The model object</param>
        private void OnModelChanged(object changedModel)
        {
            this.view.FileName = this.model.FullFileName;
            this.view.GridView.DataSource = this.model.GetTable(startYearForGrid, endYearForGrid);
            if (this.view.GridView.DataSource == null)
            {
                this.view.WarningText = this.model.ErrorMessage;
            }
            else
            {
                this.view.WarningText = string.Empty;
            }
        }
    }
}
