using System.Collections.Generic;
using Models.Graph;
using UserInterface.Views;
using System.Data;
using System.Drawing;
using Models.Core;
using System;

namespace UserInterface.Presenters
{
    class SeriesPresenter : IPresenter
    {
        private Graph Graph;
        private ISeriesView View;
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach this presenter to the model and view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Graph = Model as Graph;
            this.View = View as ISeriesView;
            this.CommandHistory = CommandHistory;
            this.View.AddSeriesContextAction("Delete series", OnDeleteSeries);
            this.View.AddSeriesContextAction("Clear all series", OnClearSeries);
            this.View.DatasetChanged += OnDatasetChanged;
            this.View.SeriesCellChanged += OnSeriesCellChanged;
            this.View.DataColumnClicked += OnDataColumnClicked;
            this.CommandHistory.ModelChanged += OnGraphModelChanged;

            PopulateSeries();
            PopulateDatasets();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            View.DatasetChanged -= OnDatasetChanged;
            View.SeriesCellChanged -= OnSeriesCellChanged;
            View.DataColumnClicked -= OnDataColumnClicked;
            CommandHistory.ModelChanged -= OnGraphModelChanged;
        }

        /// <summary>
        /// Populate the series grid in the view.
        /// </summary>
        private void PopulateSeries()
        {
            DataTable Data = new DataTable();
            Data.Columns.Add("Title", typeof(string));
            Data.Columns.Add("Table", typeof(string));
            Data.Columns.Add("X", typeof(string));
            Data.Columns.Add("Y", typeof(string));
            Data.Columns.Add("Type", typeof(string));
            foreach (Series S in Graph.Series)
            {
                DataRow Row = Data.NewRow();
                Row[0] = S.Title;
                Row[1] = S.TableName + "(" + S.SimulationName + ")";
                Row[2] = S.X;
                Row[3] = S.Y;
                Row[4] = S.Type.ToString();
                Data.Rows.Add(Row);
            }
            View.Series = Data;
        }

        /// <summary>
        /// Populate the series grid in the view.
        /// </summary>
        private void PopulateDatasets()
        {
            List<string> Datasets = new List<string>();

            string[] SimulationNames = Graph.DataStore.SimulationNames;
            string[] TableNames = Graph.DataStore.TableNames;
            foreach (string SimulationName in SimulationNames)
                foreach (string TableName in TableNames)
                {
                    if (TableName != "Simulations" && TableName != "Messages" && TableName != "Properties")
                        Datasets.Add(TableName + "(" + SimulationName + ")");
                }
            View.Datasets = Datasets.ToArray();
        }

        private void OnGraphModelChanged(object G)
        {
            if (G == Graph)
            {
                PopulateSeries();
            }
        }

        /// <summary>
        /// The user has changed the dataset. 
        /// </summary>
        private void OnDatasetChanged(string TableName)
        {
            string SimulationName = Utility.String.SplitOffBracketedValue(ref TableName, '(', ')');
            View.Data = Graph.DataStore.GetData(SimulationName, TableName);
        }

        /// <summary>
        /// User has clicked on a data column.
        /// </summary>
        private void OnDataColumnClicked(string ColumnText)
        {
            Point Cell = View.CurrentSeriesCellSelected;
            int SeriesIndex = Cell.Y;
            if (SeriesIndex >= Graph.Series.Length)
                AddSeries(ColumnText, Cell);
            else 
            {
                // edit an existing series.
                Series NewSeries = Graph.Series[SeriesIndex];
                if (Cell.X < 2 || Cell.X > 3)
                    AddSeries(ColumnText, Cell);
                else
                {
                    if (Cell.X == 2)      // the x column
                        NewSeries.X = ColumnText;
                    else                  // the y column
                        NewSeries.Y = ColumnText;
                    Series[] Series = Graph.Series;
                    Series[SeriesIndex] = NewSeries;
                    Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", Series);
                    CommandHistory.Add(Cmd);
                    Cell.X = 0;
                    Cell.Y = Series.Length;
                    View.CurrentSeriesCellSelected = Cell;
                }
                    
            }
            
        }

        /// <summary>
        /// User has changed a series cell.
        /// </summary>
        private void OnSeriesCellChanged(int Col, int Row, string NewContents)
        {
            int SeriesIndex = Row;
            List<Series> AllSeries = new List<Series>();
            AllSeries.AddRange(Graph.Series);

            if (SeriesIndex >= Graph.Series.Length)
            {
                // Add a series.
                AllSeries.Add(new Series());
            }

            if (Col == 0)
                AllSeries[SeriesIndex].Title = NewContents;
            else if (Col == 1)
            {
                string TableName = NewContents;
                string SimulationName = Utility.String.SplitOffBracketedValue(ref TableName, '(', ')');
                AllSeries[SeriesIndex].SimulationName = SimulationName;
                AllSeries[SeriesIndex].TableName = TableName;
            }
            else if (Col == 2)
                AllSeries[SeriesIndex].X = NewContents;
            else if (Col == 3)
                AllSeries[SeriesIndex].Y = NewContents;
            else if (Col == 4)
            {
                if (NewContents == "Bar")
                    AllSeries[SeriesIndex].Type = Series.SeriesType.Bar;
                else
                    AllSeries[SeriesIndex].Type = Series.SeriesType.Line;
            }
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", AllSeries.ToArray());
            CommandHistory.Add(Cmd);
        }

        /// <summary>
        /// User has selected Delete Series.
        /// </summary>
        private void OnDeleteSeries(object Sender, EventArgs Args)
        {
            int SeriesIndex = View.CurrentSeriesCellSelected.Y;
            if (SeriesIndex < Graph.Series.Length)
            {
                List<Series> AllSeries = new List<Series>();
                AllSeries.AddRange(Graph.Series);
                AllSeries.RemoveAt(SeriesIndex);
                Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", AllSeries.ToArray());
                CommandHistory.Add(Cmd);
            }
        }

        /// <summary>
        /// User has selected Clear All Series.
        /// </summary>
        private void OnClearSeries(object Sender, EventArgs Args)
        {
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", new Series[0]);
            CommandHistory.Add(Cmd);
        }

        /// <summary>
        /// Private function to add a new series to the graph.
        /// </summary>
        private void AddSeries(string ColumnText, Point Cell)
        {
            string TableName = View.CurrentDataset;
            string SimulationName = Utility.String.SplitOffBracketedValue(ref TableName, '(', ')');

            // create a new series.
            Series NewSeries = new Series();
            NewSeries.SimulationName = SimulationName;
            NewSeries.TableName = TableName;
            NewSeries.X = ColumnText;
            NewSeries.Title = "Series " + (Graph.Series.Length + 1).ToString();

            List<Series> NewSeriesColection = new List<Series>();
            NewSeriesColection.AddRange(Graph.Series);
            NewSeriesColection.Add(NewSeries);
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", NewSeriesColection.ToArray());
            CommandHistory.Add(Cmd);
            
            Cell.X = 3; // The y column
            View.CurrentSeriesCellSelected = Cell;
        }
    }
}
