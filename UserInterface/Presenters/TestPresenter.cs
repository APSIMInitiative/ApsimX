using System;
using System.Data;
using UserInterface.Views;
using Model.Components;

namespace UserInterface.Presenters
{
    class TestPresenter : IPresenter
    {
        private IGridView Grid;
        private Tests Tests;
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Grid = View as IGridView;
            this.Tests = Model as Tests;
            this.CommandHistory = CommandHistory;

            PopulateGrid();
            Grid.CellValueChanged += OnCellValueChanged;
            CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        private void PopulateGrid()
        {
            DataTable Table = new DataTable();
            Table.Columns.Add("Simulation name", typeof(string));
            Table.Columns.Add("Table name", typeof(string));
            Table.Columns.Add("Column name", typeof(string));
            Table.Columns.Add("Test type", typeof(string));
            Table.Columns.Add("Parameters", typeof(string));

            foreach (Test Test in Tests.AllTests)
            {
                Table.Rows.Add(new string[] { Test.SimulationName,
                                              Test.TableName, 
                                              Test.ColumnNames,
                                              Test.Type.ToString(), 
                                              Test.Parameters});
            }

            Grid.DataSource = Table;
            for (int Row = 0; Row < Table.Rows.Count; Row++)
                PopulateRow(Row);
            Grid.SetColumnAutoSize(2);
        }

        /// <summary>
        /// Populate the specified row with the correct combo boxes.
        /// </summary>
        /// <param name="Row"></param>
        private void PopulateRow(int Row)
        {
            DataTable Table = Grid.DataSource as DataTable;
            Grid.SetCellEditor(0, Row, Tests.DataStore.SimulationNames);

            string SimulationName = Table.Rows[Row][0].ToString();

            string TableName = Table.Rows[Row][1].ToString();
            string[] ColumnNames = Utility.DataTable.GetColumnNames(Tests.DataStore.GetData(SimulationName, TableName));

            Grid.SetCellEditor(1, Row, Tests.DataStore.TableNames);
            Grid.SetCellEditor(2, Row, ColumnNames);
            Grid.SetCellEditor(3, Row, typeof(Test.TestType));
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        private void OnCellValueChanged(int Col, int Row, object OldValue, object NewValue)
        {
            PopulateRow(Row);

            DataTable Table = Grid.DataSource as DataTable;

            Test[] AllTests = Tests.AllTests;
            AllTests[Row].SimulationName = Table.Rows[Row][0].ToString();
            AllTests[Row].TableName = Table.Rows[Row][1].ToString();
            AllTests[Row].ColumnNames = Table.Rows[Row][2].ToString();
            AllTests[Row].Type = (Test.TestType)Enum.Parse(typeof(Test.TestType), Table.Rows[Row][3].ToString());
            AllTests[Row].Parameters = Table.Rows[Row][4].ToString();

            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Tests, 
                                                                                    "AllTests",
                                                                                    AllTests);
            CommandHistory.Add(Cmd, true);
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
