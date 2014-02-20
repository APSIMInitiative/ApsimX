using System;
using System.Data;
using UserInterface.Views;
using Models;
using System.IO;

namespace UserInterface.Presenters
{
    class TestPresenter : IPresenter
    {
        private IGridView Grid;
        private Tests Tests;
        private CommandHistory CommandHistory;
        private DataStore DataStore = null;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Grid = View as IGridView;
            this.Tests = Model as Tests;
            this.CommandHistory = CommandHistory;

            Models.Core.Model RootModel = Tests;
            while (RootModel.Parent != null)
                RootModel = RootModel.Parent;

            if (RootModel != null && RootModel is Models.Core.Simulations)
            {
                Models.Core.Simulations simulations = RootModel as Models.Core.Simulations;
                DataStore = new Models.DataStore();
                DataStore.Connect(Path.ChangeExtension(simulations.FileName, ".db"), readOnly: true);
            }

            PopulateGrid();
            Grid.CellValueChanged += OnCellValueChanged;
            CommandHistory.ModelChanged += OnModelChanged;
        }

         /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            Grid.CellValueChanged -= OnCellValueChanged;
            CommandHistory.ModelChanged -= OnModelChanged;
            DataStore.Disconnect();
            DataStore = null;
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        private void PopulateGrid()
        {
            DataTable Table = TestsToDataTable();
            Grid.DataSource = Table;

            // Set up cell editors for all cells.
            Test.TestType Dummy = new Test.TestType();
            Grid.RowCount = 100;
            for (int Row = 0; Row < Grid.RowCount; Row++)
            {
                PopulateComboItemsInRow(Row);
                Grid.SetCellEditor(3, Row, Dummy);
            }

            Grid.SetColumnSize(2);
        }

        /// <summary>
        /// Populate the specified row with the correct combo boxes.
        /// </summary>
        /// <param name="Row"></param>
        private void PopulateComboItemsInRow(int Row)
        {
            DataTable Table = Grid.DataSource;
            Grid.SetCellEditor(0, Row, DataStore.SimulationNames);

            string[] TableNames = new string[0];
            string[] ColumnNames = new string[0];

            if (Row < Table.Rows.Count)
            {
                string SimulationName = Table.Rows[Row][0].ToString();
                if (SimulationName != "")
                {
                    TableNames = DataStore.TableNames;
                    string TableName = Table.Rows[Row][1].ToString();
                    if (TableName != "")
                        ColumnNames = Utility.DataTable.GetColumnNames(DataStore.GetData(SimulationName, TableName));
                }
            }

            Grid.SetCellEditor(1, Row, TableNames);
            Grid.SetCellEditor(2, Row, ColumnNames);
        }

        /// <summary>
        /// Convert all tests to a datatable.
        /// </summary>
        private DataTable TestsToDataTable()
        {
            DataTable Table = new DataTable();
            Table.Columns.Add("Simulation name", typeof(string));
            Table.Columns.Add("Table name", typeof(string));
            Table.Columns.Add("Column name", typeof(string));
            Table.Columns.Add("Test type", typeof(string));
            Table.Columns.Add("Parameters", typeof(string));

            if (Tests.AllTests != null)
                foreach (Test Test in Tests.AllTests)
                {
                    Table.Rows.Add(new string[] { Test.SimulationName,
                                                  Test.TableName, 
                                                  Test.ColumnNames,
                                                  Test.Type.ToString(), 
                                                  Test.Parameters});
                }

            return Table;
        }

        /// <summary>
        /// Convert the specified datatable to an array of tests.
        /// </summary>
        private Test[] DataTableToTests(DataTable Table)
        {
            Test[] AllTests = new Test[Table.Rows.Count];
            for (int Row = 0; Row < Table.Rows.Count; Row++)
            {
                AllTests[Row] = new Test();
                AllTests[Row].SimulationName = Table.Rows[Row][0].ToString();
                AllTests[Row].TableName = Table.Rows[Row][1].ToString();
                AllTests[Row].ColumnNames = Table.Rows[Row][2].ToString();
                if (Table.Rows[Row][3].ToString() != "")
                    AllTests[Row].Type = (Test.TestType)Enum.Parse(typeof(Test.TestType), Table.Rows[Row][3].ToString());
                AllTests[Row].Parameters = Table.Rows[Row][4].ToString();
            }
            return AllTests;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        private void OnCellValueChanged(int Col, int Row, object OldValue, object NewValue)
        {
            // The ChangePropertyCommand below will trigger a call to OnModelChanged. We don't need to 
            // repopulate the grid so stop the event temporarily until end of this method.
            CommandHistory.ModelChanged -= OnModelChanged;

            PopulateComboItemsInRow(Row);

            // Convert grid datatable back to an array of test objects and store in Tests model
            // via a command.
            Test[] AllTests = DataTableToTests(Grid.DataSource);
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Tests, "AllTests", AllTests);
            CommandHistory.Add(Cmd, true);

            // Reinstate the model changed event.
            CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        private void OnModelChanged(object ChangedModel)
        {
            if (ChangedModel == Tests)
                PopulateGrid();
        }


    }
}
