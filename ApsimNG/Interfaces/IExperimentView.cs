namespace UserInterface.Interfaces
{
    using System;
    using EventArguments;
    using System.Collections.Generic;

    interface IExperimentView
    {
        /// <summary>
        /// Invoked when the user wishes to export the current factor information to a .csv file.
        /// </summary>
        event EventHandler<FileActionArgs> ExportCsv;

        /// <summary>
        /// Invoked when the user wishes to export the current factor information to a .csv file.
        /// </summary>
        event EventHandler<FileActionArgs> ImportCsv;

        /// <summary>
        /// Invoked when the user wishes to run simulations.
        /// </summary>
        event EventHandler RunSims;

        /// <summary>
        /// Invoked when the user wishes to enable the selected simulations.
        /// </summary>
        event EventHandler EnableSims;

        /// <summary>
        /// Invoked when the user wishes to disable the selected simulations.
        /// </summary>
        event EventHandler DisableSims;

        /// <summary>
        /// Invoked when the user changes the maximum number of simulations to display at once.
        /// </summary>
        event EventHandler SetMaxSims;

        /// <summary>
        /// Gets the names of the selected simulations.
        /// </summary>
        List<string> SelectedItems { get; }

        /// <summary>
        /// Gets or sets the max number of sims to display.
        /// </summary>
        string MaxSimsToDisplay { get; set; }

        /// <summary>
        /// Gets or sets the value displayed in the number of simulations label.
        /// </summary>
        string NumSims { get; set; }

        /// <summary>
        /// Populates the TreeView with data.
        /// </summary>
        /// <param name="simulations">List of rows. Each row represents a single simulation and is a tuple, made up of a string (simulation name), a list of strings (factor levels) and a boolean (whether the simulation is currently enabled).</param>
        void Populate(List<Tuple<string, List<string>, bool>> simulations);

        /// <summary>
        /// Do cleanup work.
        /// </summary>
        void Detach();

        /// <summary>
        /// Initialises and populates the TreeView.
        /// </summary>
        /// <param name="columnNames">The names of the columns.</param>
        /// <param name="simulations">List of simulations. Each simulation is a tuple comprised of the simulation name, the list of factor levels, and an enabled/disabled flag.</param>
        void Initialise(List<string> columnNames);
    }
}
