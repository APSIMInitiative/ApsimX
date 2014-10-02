// -----------------------------------------------------------------------
// <copyright file="Report.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using Models.Core;

    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    public class Report : Model
    {


        // privates
        private List<ReportColumn> Members = null;

        [Link]
        public Simulation Simulation = null;

        // Properties read in.
        [Summary]
        [Description("Output variables")]
        public string[] VariableNames {get; set;}

        [Summary]
        [Description("Output frequency")]
        public string[] EventNames { get; set; }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<string> eventNames = new List<string>();
            for (int i = 0; i < EventNames.Length; i++ )
            {
                if (EventNames[i] != "")
                {
                    eventNames.Add(EventNames[i].Trim());
                    Apsim.Subscribe(this, EventNames[i].Trim(), OnReport);
                }
            }
            EventNames = eventNames.ToArray();

            // sanitise the variable names and remove duplicates
            List<string> variableNames = new List<string>();
            for (int i = 0; i < VariableNames.Length; i++)
            {
                bool isDuplicate = Utility.String.IndexOfCaseInsensitive(variableNames, VariableNames[i].Trim()) != -1;
                if (!isDuplicate && VariableNames[i] != "")
                    variableNames.Add(VariableNames[i].Trim());
            }
            VariableNames = variableNames.ToArray();
            FindVariableMembers();
        }

        /// <summary>
        /// Event handler for the report event.
        /// </summary>
        public void OnReport(object sender, EventArgs e)
        {
            foreach (ReportColumn Variable in Members)
                Variable.StoreValue();
        }

        /// <summary>
        /// Public method to allow reporting from scripts.
        /// </summary>
        public void DoReport()
        {
            foreach (ReportColumn Variable in Members)
                Variable.StoreValue();
        }

        /// <summary>
        /// Fill the Members list with VariableMember objects for each variable.
        /// </summary>
        private void FindVariableMembers()
        {
            Members = new List<ReportColumn>();

            List<string> Names = new List<string>();
            List<Type> Types = new List<Type>();
            foreach (string FullVariableName in VariableNames)
            {
                if (FullVariableName != "")
                    Members.Add(ReportColumn.Create(FullVariableName, this));
            }
        }

        /// <summary>
        /// Simulation has completed - write the report table.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Simulation != null)
            {
                // Get rid of old data in .db
                DataStore DataStore = new DataStore(this);
                DataStore.DeleteOldContentInTable(Simulation.Name, Name);

                // Write and store a table in the DataStore
                if (Members != null && Members.Count > 0)
                {
                    DataTable table = new DataTable();

                    foreach (ReportColumn Variable in Members)
                        Variable.AddColumnsToTable(table);

                    foreach (ReportColumn Variable in Members)
                        Variable.AddRowsToTable(table);

                    DataStore.WriteTable(Simulation.Name, Name, table);

                    Members.Clear();
                    Members = null;
                }

                UnsubscribeAllEventHandlers();
                DataStore.Disconnect();
                DataStore = null;
            }
        }

        private void UnsubscribeAllEventHandlers()
        {
            // Unsubscribe to all events.
            foreach (string Event in EventNames)
                if ( (Event != null) && (Event != "") )
                    Apsim.Unsubscribe(this, Event, OnReport);
        }

    }
}