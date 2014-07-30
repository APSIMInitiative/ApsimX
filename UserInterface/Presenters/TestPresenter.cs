using System;
using System.Data;
using UserInterface.Views;
using Models;
using System.IO;
using UserInterface.Interfaces;
using UserInterface.EventArguments;
using System.Collections.Generic;

namespace UserInterface.Presenters
{
    class TestPresenter : IPresenter
    {
        private IGridView Grid;
        private Tests Tests;
        private ExplorerPresenter ExplorerPresenter;
        private DataStore DataStore = null;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Grid = View as IGridView;
            this.Tests = Model as Tests;
            ExplorerPresenter = explorerPresenter;

            Models.Core.Model RootModel = Tests;
            while (RootModel.Parent != null)
                RootModel = RootModel.Parent;

            if (RootModel != null && RootModel is Models.Core.Simulations)
            {
                Models.Core.Simulations simulations = RootModel as Models.Core.Simulations;
                DataStore = new Models.DataStore(Tests);
            }

            PopulateGrid();
            Grid.CellsChanged += OnCellValueChanged;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

         /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            Grid.CellsChanged -= OnCellValueChanged;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
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
            Test.TestType dummy = new Test.TestType();
            this.Grid.RowCount = 100;
            for (int Row = 0; Row < Grid.RowCount; Row++)
            {
                PopulateComboItemsInRow(Row);
                IGridCell testTypeCell = this.Grid.GetCell(3, Row);

                testTypeCell.EditorType = EditorTypeEnum.DropDown;
                testTypeCell.DropDownStrings = Utility.String.EnumToStrings(dummy);
            }

            this.Grid.GetColumn(2).Width = -1;
        }

        /// <summary>
        /// Populate the specified row with the correct combo boxes.
        /// </summary>
        /// <param name="Row">Row index</param>
        private void PopulateComboItemsInRow(int Row)
        {
            DataTable Table = Grid.DataSource;
            
            IGridCell simulationCell = this.Grid.GetCell(0, Row);
            simulationCell.EditorType = EditorTypeEnum.DropDown;
            simulationCell.DropDownStrings = DataStore.SimulationNames;

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

            IGridCell tableNameCell = this.Grid.GetCell(1, Row);
            tableNameCell.EditorType = EditorTypeEnum.DropDown;
            tableNameCell.DropDownStrings = TableNames;

            IGridCell columnNameCell = this.Grid.GetCell(2, Row);
            columnNameCell.EditorType = EditorTypeEnum.DropDown;
            columnNameCell.DropDownStrings = ColumnNames;
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
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            // The ChangePropertyCommand below will trigger a call to OnModelChanged. We don't need to 
            // repopulate the grid so stop the event temporarily until end of this method.
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;

            // Get a list of rows that have changed.
            SortedSet<int> changedRows = new SortedSet<int>();
            foreach (IGridCell cell in e.ChangedCells)
            {
                changedRows.Add(cell.RowIndex);
            }

            // Repopulate each changed row.
            foreach (int rowIndex in changedRows)
            {
                this.PopulateComboItemsInRow(rowIndex);
            }

            // Convert grid datatable back to an array of test objects and store in Tests model
            // via a command.
            Test[] AllTests = DataTableToTests(Grid.DataSource);
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Tests, "AllTests", AllTests);
            ExplorerPresenter.CommandHistory.Add(Cmd, true);

            // Reinstate the model changed event.
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
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
