using System;
using System.Collections.Generic;
using Models.Graph;
using UserInterface.Views;
using Models;
using System.Data;
using System.Drawing;
using Models.Core;
using System.Reflection;

namespace UserInterface.Presenters
{
    class SeriesPresenter : IPresenter
    {
        private Graph Graph;
        private ISeriesView SeriesView;
        private DataStore DataStore;
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            //Series Series = Model as Series;
            //Graph = Series.Parent as Graph;
            Graph = Model as Graph;
            SeriesView = View as SeriesView;
            this.CommandHistory = CommandHistory;
            DataStore = Graph.Find(typeof(DataStore)) as DataStore;

            SeriesView.SeriesGrid.AddContextAction("Delete series", OnDeleteSeries);
            SeriesView.SeriesGrid.AddContextAction("Clear all series", OnClearSeries);

            SeriesView.DataGrid.ColumnHeaderClicked += OnDataColumnClicked;
            SeriesView.SeriesGrid.CellValueChanged += OnCellChanged;

            PopulateSeries();
            PopulateDataSources();

            SeriesView.XFocused = true;
            this.CommandHistory.ModelChanged += OnGraphModelChanged;

            SeriesView.DataSourceChanged += OnDataSourceChanged;
            OnDataSourceChanged(SeriesView.DataSource);
        }

        /// <summary>
        /// Detach the model and view from this presenter.
        /// </summary>
        public void Detach()
        {
            SeriesView.DataSourceChanged -= OnDataSourceChanged;
            SeriesView.DataGrid.ColumnHeaderClicked -= OnDataColumnClicked;
            CommandHistory.ModelChanged -= OnGraphModelChanged;
        }

        /// <summary>
        /// Populate the series grid in the view.
        /// </summary>
        private void PopulateSeries()
        {
            DataTable Data = new DataTable();
            if (Graph != null && Graph.Series.Count > 0)
            {
                Data.Columns.Add("X", typeof(string));
                Data.Columns.Add("Y", typeof(string));
                Data.Columns.Add("Title", typeof(string));
                Data.Columns.Add("C", typeof(int));
                Data.Columns.Add("Type", typeof(string));
                Data.Columns.Add("X on Top?", typeof(bool));
                Data.Columns.Add("Y on Right?", typeof(bool));
                Data.Columns.Add("Marker", typeof(string));
                foreach (Series S in Graph.Series)
                {
                    DataRow Row = Data.NewRow();
                    if (S.X != null)
                        Row[0] = S.X.FieldName;
                    if (S.Y != null)
                        Row[1] = S.Y.FieldName;
                    Row[2] = S.Title;
                    Row[3] = S.Colour.ToArgb();
                    Row[4] = S.Type.ToString();
                    Row[5] = S.XAxis == Axis.AxisType.Top;
                    Row[6] = S.YAxis == Axis.AxisType.Right;
                    Row[7] = S.Marker.ToString();
                    Data.Rows.Add(Row);
                }
                SeriesView.SeriesGrid.DataSource = Data;
                SeriesView.SeriesGrid.SetColumnAlignment(0, true);
                SeriesView.SeriesGrid.SetColumnAlignment(1, true);
                SeriesView.SeriesGrid.SetColumnAlignment(2, true);

                for (int Row = 0; Row < Data.Rows.Count; Row++)
                {
                    Series S = Graph.Series[Row];
                    if (S.X != null)
                    {
                        SeriesView.SeriesGrid.SetToolTipForCell(0, Row, S.X.SimulationName + "." +
                                                                        S.X.TableName);
                        SeriesView.SeriesGrid.SetCellEditor(0, Row, S.X.ValidFieldNames);
                    }
                    if (S.Y != null)
                    {
                        SeriesView.SeriesGrid.SetToolTipForCell(1, Row, S.Y.SimulationName + "." +
                                                                        S.Y.TableName);
                        SeriesView.SeriesGrid.SetCellEditor(1, Row, S.Y.ValidFieldNames);
                    }
                    SeriesView.SeriesGrid.SetCellEditor(3, Row, Color.Red); //Data.Rows[Row][3]);
                    SeriesView.SeriesGrid.SetCellEditor(4, Row, S.Type);
                    SeriesView.SeriesGrid.SetCellEditor(5, Row, Data.Rows[Row][5]);
                    SeriesView.SeriesGrid.SetCellEditor(6, Row, Data.Rows[Row][6]);
                    SeriesView.SeriesGrid.SetCellEditor(7, Row, S.Marker);
                }
            }
            else
                SeriesView.SeriesGrid.DataSource = null;
        }

        /// <summary>
        /// Populate data sources.
        /// </summary>
        private void PopulateDataSources()
        {
            // Populate the view with a list of data sources.
            List<string> DataSources = new List<string>();
            if (DataStore != null)
            {
                foreach (string SimulationName in DataStore.SimulationNames)
                    foreach (string TableName in DataStore.TableNames)
                    {
                        if (TableName != "Properties" && TableName != "Messages")
                        {
                            DataSources.Add(SimulationName + "." + TableName);
                        }
                    }
                DataSources.Sort();
            }

            List<string> FullPaths = new List<string>();
            foreach (Model Model in Graph.Models)
                FullPaths.Add(Model.FullPath);

            DataSources.AddRange(FullPaths);
            SeriesView.DataSourceItems = DataSources.ToArray();
        }

        /// <summary>
        /// User has changed a series cell.
        /// </summary>
        private void OnCellChanged(int Col, int Row, object OldValue, object NewContents)
        {
            int SeriesIndex = Row;
            List<Series> AllSeries = new List<Series>();
            AllSeries.AddRange(Graph.Series);
           

            if (SeriesIndex >= Graph.Series.Count)
            {
                // Get the simulation and table names.
                string SimulationName = null;
                string TableName = null;
                if (!SeriesView.DataSource.StartsWith("."))
                {
                    SimulationName = SeriesView.DataSource;
                    TableName = Utility.String.SplitOffAfterDelimiter(ref SimulationName, ".");
                }

                // Add a series.
                Series NewSeries = new Series() { Parent = Graph };
                NewSeries.X = new GraphValues() { Parent = NewSeries, SimulationName = SimulationName, TableName = TableName };
                NewSeries.Y = new GraphValues() { Parent = NewSeries, SimulationName = SimulationName, TableName = TableName };
                NewSeries.XAxis = Axis.AxisType.Bottom;
                NewSeries.YAxis = Axis.AxisType.Left;
                AllSeries.Add(NewSeries);
            }
            Series S = AllSeries[SeriesIndex];

            if (Col == 0) // X
                S.X.FieldName = NewContents.ToString();
            else if (Col == 1)  // Y
                S.Y.FieldName = NewContents.ToString();
            else if (Col == 2) // Title
                S.Title = NewContents.ToString();
            else if (Col == 3) // Colour
                S.Colour = Color.FromArgb((int)NewContents);
            else if (Col == 4) // Type
                S.Type = (Series.SeriesType)Enum.Parse(typeof(Series.SeriesType), NewContents.ToString());
            else if (Col == 5) // Top?
            {
                if ((bool)NewContents)
                    S.XAxis = Axis.AxisType.Top;
                else
                    S.XAxis = Axis.AxisType.Bottom;
            }
            else if (Col == 6) // Right?
            {
                if ((bool)NewContents)
                    S.YAxis = Axis.AxisType.Right;
                else
                    S.YAxis = Axis.AxisType.Left;
            }
            else if (Col == 7) // Marker
                S.Marker = (Series.MarkerType)Enum.Parse(typeof(Series.MarkerType), NewContents.ToString());

            List<Axis> AllAxes = GetAllRequiredAxes(AllSeries);
            string[] PropertyNamesChanged = new string[] { "Series", "Axes" };
            object[] PropertyValues = new object[] { AllSeries, AllAxes };
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, PropertyNamesChanged, PropertyValues);
            CommandHistory.Add(Cmd);
        }

        /// <summary>
        /// Return a list of axis objects such that every series in AllSeries has an associated axis object.
        /// </summary>
        private List<Axis> GetAllRequiredAxes(List<Series> AllSeries)
        {
            List<Axis> AllAxes = new List<Axis>();

            // Get a list of all axis types.
            List<Models.Graph.Axis.AxisType> AllAxisTypes = new List<Models.Graph.Axis.AxisType>();
            foreach (Series S in AllSeries)
            {
                AllAxisTypes.Add(S.XAxis);
                AllAxisTypes.Add(S.YAxis);
            }

            // Go through all graph axis objects. For each, check to see if it is still needed and
            // if so copy to our list.
            foreach (Axis A in Graph.Axes)
                if (AllAxisTypes.Contains(A.Type))
                    AllAxes.Add(A);

            // Go through all series and make sure an axis object is present in our AllAxes list. If
            // not then go create an axis object.
            foreach (Series S in AllSeries)
            {
                if (!FindAxis(AllAxes, S.XAxis))
                    AllAxes.Add(new Axis() { Type = S.XAxis });
                if (!FindAxis(AllAxes, S.YAxis))
                    AllAxes.Add(new Axis() { Type = S.YAxis });
            }

            return AllAxes;
        }

        /// <summary>
        /// Go through the AllAxes list and return true if the specified AxisType is found in the list.
        /// </summary>
        private static bool FindAxis(List<Axis> AllAxes, Axis.AxisType AxisTypeToFind)
        {
            foreach (Axis A in AllAxes)
                if (A.Type == AxisTypeToFind)
                    return true;
            return false;
        }

        /// <summary>
        /// The Graph model has changed - update the graph.
        /// </summary>
        private void OnGraphModelChanged(object G)
        {
            if (G == Graph)
            {
                PopulateSeries();
            }
        }

        /// <summary>
        /// The selected data source has changed.
        /// </summary>
        private void OnDataSourceChanged(string NewDataSource)
        {
            if (NewDataSource.StartsWith("."))
            {

                Model M = Graph.Get(NewDataSource) as Model;
                SeriesView.DataGrid.DataSource = GetAllArrayProperties(M);
            }
            else
            {
                string SimulationName = NewDataSource;
                string TableName = Utility.String.SplitOffAfterDelimiter(ref SimulationName, ".");
                SeriesView.DataGrid.DataSource = DataStore.GetData(SimulationName, TableName);
            }
            SeriesView.DataGrid.SetNumericFormat("N3");
        }

        /// <summary>
        /// Use reflection to go through all array properties in M, returning a DataTable
        /// with a column for each.
        /// </summary>
        private DataTable GetAllArrayProperties(Model M)
        {
            DataTable Table = new DataTable();
            foreach (PropertyInfo Property in M.GetType().GetProperties())
            {
                if (Property.PropertyType.IsArray)
                {
                    Array Values = Property.GetValue(M, null) as Array;
                    if (Values != null)
                    {
                        List<object> PropertyValues = new List<object>();
                        foreach (object Value in Values)
                            PropertyValues.Add(Value);

                        Utility.DataTable.AddColumnOfObjects(Table, M.FullPath + "." + Property.Name, PropertyValues.ToArray());
                    }
                }
            }
            return Table;
        }

        /// <summary>
        /// User has clicked a column.
        /// </summary>
        private void OnDataColumnClicked(string ColumnName)
        {
            if (SeriesView.XFocused)
            {
                // add a new row.
                SeriesView.SeriesGrid.RowCount = SeriesView.SeriesGrid.RowCount + 1;

                int Row = SeriesView.SeriesGrid.RowCount-1;
                SeriesView.SeriesGrid.SetCellValue(0, Row, ColumnName);
                OnCellChanged(0, Row, null, ColumnName);
                SeriesView.XFocused = false;
                SeriesView.YFocused = true;
            }
            else
            {
                int Row = SeriesView.SeriesGrid.RowCount - 1;
                SeriesView.SeriesGrid.SetCellValue(1, Row, ColumnName);
                OnCellChanged(1, Row, null, ColumnName);
            }
        }

        /// <summary>
        /// User has selected Delete Series.
        /// </summary>
        private void OnDeleteSeries(object Sender, EventArgs Args)
        {
            int SeriesIndex = SeriesView.SeriesGrid.CurrentCell.Y;
            if (SeriesIndex < Graph.Series.Count)
            {
                List<Series> AllSeries = new List<Series>();
                AllSeries.AddRange(Graph.Series);
                AllSeries.RemoveAt(SeriesIndex);
                Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", AllSeries);
                CommandHistory.Add(Cmd);
            }
        }

        /// <summary>
        /// User has selected Clear All Series.
        /// </summary>
        private void OnClearSeries(object Sender, EventArgs Args)
        {
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", new List<Series>());
            CommandHistory.Add(Cmd);
        }

    }
}
