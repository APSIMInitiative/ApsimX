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
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            //Series Series = Model as Series;
            //Graph = Series.Parent as Graph;
            Graph = Model as Graph;
            SeriesView = View as SeriesView;
            ExplorerPresenter = explorerPresenter;
            DataStore = Graph.DataStore;

            SeriesView.SeriesGrid.AddContextAction("Delete series", OnDeleteSeries);
            SeriesView.SeriesGrid.AddContextAction("Clear all series", OnClearSeries);

            SeriesView.DataGrid.ColumnHeaderClicked += OnDataColumnClicked;
            SeriesView.SeriesGrid.CellValueChanged += OnCellChanged;

            PopulateDataSources();
            PopulateSeries();

            SeriesView.XFocused = true;
            ExplorerPresenter.CommandHistory.ModelChanged += OnGraphModelChanged;

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
            ExplorerPresenter.CommandHistory.ModelChanged -= OnGraphModelChanged;
        }

        /// <summary>
        /// Populate the series grid in the view.
        /// </summary>
        private void PopulateSeries()
        {
            DataTable Data = new DataTable();
            if (Graph != null && Graph.Series.Count > 0)
            {
                Data.Columns.Add("Data source", typeof(string));
                Data.Columns.Add("X", typeof(string));
                Data.Columns.Add("Y", typeof(string));
                Data.Columns.Add("Title", typeof(string));
                Data.Columns.Add("C", typeof(int));
                Data.Columns.Add("Type", typeof(string));
                Data.Columns.Add("X on Top?", typeof(bool));
                Data.Columns.Add("Y on Right?", typeof(bool));
                Data.Columns.Add("Line", typeof(string));
                Data.Columns.Add("Marker", typeof(string));
                Data.Columns.Add("Regression?", typeof(bool));
                foreach (Series S in Graph.Series)
                {
                    DataRow Row = Data.NewRow();
                    if (S.X != null)
                    {
                        if (S.X.SimulationName == null)
                            Row[0] = S.X.TableName;
                        else
                            Row[0] = S.X.SimulationName + "." + S.X.TableName;
                        Row[1] = S.X.FieldName;
                    }
                    if (S.Y != null)
                        Row[2] = S.Y.FieldName;
                    Row[3] = S.Title;
                    Row[4] = S.Colour.ToArgb();
                    Row[5] = S.Type.ToString();
                    Row[6] = S.XAxis == Axis.AxisType.Top;
                    Row[7] = S.YAxis == Axis.AxisType.Right;
                    Row[8] = S.Line.ToString();
                    Row[9] = S.Marker.ToString();
                    Row[10] = S.ShowRegressionLine;
                    Data.Rows.Add(Row);
                }
                SeriesView.SeriesGrid.DataSource = Data;
                SeriesView.SeriesGrid.SetColumnAlignment(0, true);
                SeriesView.SeriesGrid.SetColumnAlignment(1, true);
                SeriesView.SeriesGrid.SetColumnAlignment(2, true);
                SeriesView.SeriesGrid.SetColumnAlignment(3, true);

                for (int Row = 0; Row < Data.Rows.Count; Row++)
                {
                    SeriesView.SeriesGrid.SetCellEditor(0, Row, SeriesView.DataSourceItems);
                    Series S = Graph.Series[Row];
                    if (S.X != null)
                        SeriesView.SeriesGrid.SetCellEditor(1, Row, Graph.GetValidFieldNames(S.X));
                    if (S.Y != null)
                        SeriesView.SeriesGrid.SetCellEditor(2, Row, Graph.GetValidFieldNames(S.Y));
                    SeriesView.SeriesGrid.SetCellEditor(4, Row, Color.Red);
                    SeriesView.SeriesGrid.SetCellEditor(5, Row, S.Type);
                    SeriesView.SeriesGrid.SetCellEditor(6, Row, Data.Rows[Row][6]);
                    SeriesView.SeriesGrid.SetCellEditor(7, Row, Data.Rows[Row][7]);
                    SeriesView.SeriesGrid.SetCellEditor(8, Row, S.Line);
                    SeriesView.SeriesGrid.SetCellEditor(9, Row, S.Marker);
                    SeriesView.SeriesGrid.SetCellEditor(10, Row, S.ShowRegressionLine);
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
                        if (TableName != "Messages")
                            DataSources.Add(SimulationName + "." + TableName);
                DataSources.Sort();

                // Add in the raw table names e.g. report
                foreach (string TableName in DataStore.TableNames)
                    if (TableName != "Messages")
                        DataSources.Insert(0, TableName);

                // Add in the all simulations in scope tables e.g. *.report
                foreach (string TableName in DataStore.TableNames)
                    if (TableName != "Messages") 
                        DataSources.Insert(0, "*." + TableName);
            }

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
                if (!SeriesView.DataSource.Contains("."))
                {
                    TableName = SeriesView.DataSource;
                }
                else if (!SeriesView.DataSource.StartsWith("."))
                {
                    SimulationName = SeriesView.DataSource;
                    TableName = Utility.String.SplitOffAfterDelimiter(ref SimulationName, ".");
                }

                // Add a series.
                Series NewSeries = new Series();
                NewSeries.X = new GraphValues() { SimulationName = SimulationName, TableName = TableName };
                NewSeries.Y = new GraphValues() { SimulationName = SimulationName, TableName = TableName };
                NewSeries.XAxis = Axis.AxisType.Bottom;
                NewSeries.YAxis = Axis.AxisType.Left;
                AllSeries.Add(NewSeries);
            }
            if (SeriesIndex != -1)
            {
                Series S = AllSeries[SeriesIndex];

                if (Col == 0) // Data source
                {
                    string SimulationName = NewContents.ToString();
                    string TableName;
                    if (SimulationName.Contains("."))
                        TableName = Utility.String.SplitOffAfterDelimiter(ref SimulationName, ".");
                    else
                    {
                        TableName = SimulationName;
                        SimulationName = null;
                    }
                    S.X.SimulationName = SimulationName;
                    S.X.TableName = TableName;
                    S.Y.SimulationName = SimulationName;
                    S.Y.TableName = TableName;
                }
                else if (Col == 1) // X
                    S.X.FieldName = NewContents.ToString();
                else if (Col == 2)  // Y
                    S.Y.FieldName = NewContents.ToString();
                else if (Col == 3) // Title
                    S.Title = NewContents.ToString();
                else if (Col == 4) // Colour
                    S.Colour = Color.FromArgb((int)NewContents);
                else if (Col == 5) // Type
                    S.Type = (Series.SeriesType)Enum.Parse(typeof(Series.SeriesType), NewContents.ToString());
                else if (Col == 6) // Top?
                {
                    if ((bool)NewContents)
                        S.XAxis = Axis.AxisType.Top;
                    else
                        S.XAxis = Axis.AxisType.Bottom;
                }
                else if (Col == 7) // Right?
                {
                    if ((bool)NewContents)
                        S.YAxis = Axis.AxisType.Right;
                    else
                        S.YAxis = Axis.AxisType.Left;
                }
                else if (Col == 8) // Line
                    S.Line = (Series.LineType)Enum.Parse(typeof(Series.LineType), NewContents.ToString());

                else if (Col == 9) // Marker
                    S.Marker = (Series.MarkerType)Enum.Parse(typeof(Series.MarkerType), NewContents.ToString());

                else if (Col == 10) // Regression line?
                    S.ShowRegressionLine = (bool)NewContents;

                List<Axis> AllAxes = GetAllRequiredAxes(AllSeries);
                string[] PropertyNamesChanged = new string[] { "Series", "Axes" };
                object[] PropertyValues = new object[] { AllSeries, AllAxes };
                Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, PropertyNamesChanged, PropertyValues);
                ExplorerPresenter.CommandHistory.Add(Cmd);
            }
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

                Model M = Graph.Variables.Get(NewDataSource) as Model;
                SeriesView.DataGrid.DataSource = GetAllArrayProperties(M);
            }
            else if (!NewDataSource.Contains("."))
            {
                string TableName = NewDataSource;
                SeriesView.DataGrid.DataSource = DataStore.GetData(null, TableName);
            }
            else
            {
                string SimulationName = NewDataSource;
                string TableName = Utility.String.SplitOffAfterDelimiter(ref SimulationName, ".");
                SeriesView.DataGrid.DataSource = DataStore.GetData(SimulationName, TableName);
            }
            if (SeriesView.DataGrid.DataSource != null)
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
                SeriesView.SeriesGrid.SetCellValue(0, Row, SeriesView.DataSource);
                OnCellChanged(1, Row, null, ColumnName);
                SeriesView.XFocused = false;
                SeriesView.YFocused = true;
            }
            else
            {
                int Row = SeriesView.SeriesGrid.RowCount - 1;
                SeriesView.SeriesGrid.SetCellValue(1, Row, ColumnName);
                OnCellChanged(2, Row, null, ColumnName);
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
                ExplorerPresenter.CommandHistory.Add(Cmd);
            }
        }

        /// <summary>
        /// User has selected Clear All Series.
        /// </summary>
        private void OnClearSeries(object Sender, EventArgs Args)
        {
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Graph, "Series", new List<Series>());
            ExplorerPresenter.CommandHistory.Add(Cmd);
        }

    }
}
