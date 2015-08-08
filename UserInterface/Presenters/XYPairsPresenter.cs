// -----------------------------------------------------------------------
// <copyright file="InitialWaterPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using System.Reflection;
    using EventArguments;
    using Interfaces;
    using Models.PMF.Functions;
    using Models.Core;
    using Models.Graph;
    using Views;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// The presenter class for populating an InitialWater view with an InitialWater model.
    /// </summary>
    public class XYPairsPresenter : IPresenter
    {
        /// <summary>
        /// The XYPairs model.
        /// </summary>
        private XYPairs xyPairs;

        /// <summary>
        /// The initial XYPairs view;
        /// </summary>
        private XYPairsView xyPairsView;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// A reference to the 'graphPresenter' responsible for our graph.
        /// </summary>
        private GraphPresenter graphPresenter;

        /// <summary>
        /// Our graph.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// A list of all properties in the variables grid.
        /// </summary>
        private List<VariableProperty> propertiesInGrid = new List<VariableProperty>();

        /// <summary>
        /// Attach the view to the model.
        /// </summary>
        /// <param name="model">The initial water model</param>
        /// <param name="view">The initial water view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.xyPairs = model as XYPairs;
            this.xyPairsView = view as XYPairsView;
            this.explorerPresenter = explorerPresenter as ExplorerPresenter;

            // Create a list of profile (array) properties. PpoulateView wil create a table from them and 
            // hand the table to the variables grid.
            this.FindAllProperties(this.xyPairs);

            this.PopulateView();

            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            // Populate the graph.
            this.graph = Utility.Graph.CreateGraphFromResource(model.GetType().Name + "Graph");
            this.xyPairs.Children.Add(this.graph);
            this.graph.Parent = this.xyPairs;
            this.graphPresenter = new GraphPresenter();
            this.graphPresenter.Attach(this.graph, this.xyPairsView.Graph, this.explorerPresenter);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            this.DisconnectViewEvents();
            this.xyPairs.Children.Remove(this.graph);
        }

        /// <summary>
        /// Populate all controls on the view.
        /// </summary>
        private void PopulateView()
        {
            this.PopulateGrid();
            
            this.DisconnectViewEvents();
            this.ConnectViewEvents();

            // Refresh the graph.
            if (this.graph != null)
                this.graphPresenter.DrawGraph();
        }

        /// <summary>
        /// Populate the grid with data and formatting.
        /// </summary>
        private void PopulateGrid()
        {
            DataTable table = this.CreateTable();
            this.xyPairsView.VariablesGrid.DataSource = table;
            this.xyPairsView.VariablesGrid.RowCount = 100;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                this.xyPairsView.VariablesGrid.GetColumn(i).Width = 100;
                this.xyPairsView.VariablesGrid.GetColumn(i).Width = 100;
            }
        }

        /// <summary>
        /// Setup the variables grid based on the properties in the model.
        /// </summary>
        /// <param name="model">The underlying model we are to use to find the properties</param>
        private void FindAllProperties(Model model)
        {
            // Properties must be public with a getter and a setter. They must also
            // be either double[] or string[] type.
            foreach (PropertyInfo property in model.GetType().GetProperties())
            {
                bool hasDescription = property.IsDefined(typeof(DescriptionAttribute), false);
                if (hasDescription && property.CanRead)
                {
                    if (property.PropertyType == typeof(double[]) ||
                        property.PropertyType == typeof(string[]))
                    {
                        this.propertiesInGrid.Add(new VariableProperty(model, property));
                    }
                }
            }
        }        
        /// <summary>
        /// Setup the profile grid based on the properties in the model.
        /// The column index of the cell that has changed.
        /// </summary>
        /// <returns>The filled data table. Never returns null.</returns>
        private DataTable CreateTable()
        {
            DataTable table = new DataTable();

            foreach (VariableProperty property in this.propertiesInGrid)
            {
                string columnName = property.Description;
                if (property.Units != null)
                {
                    columnName += "\r\n(" + property.Units + ")";
                }

                Array values = property.Value as Array;

                if (table.Columns.IndexOf(columnName) == -1)
                {
                    table.Columns.Add(columnName, property.DataType.GetElementType());
                }
                else
                {

                }

                DataTableUtilities.AddColumnOfObjects(table, columnName, values);
            }

            return table;
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
            // Trap the invoking of the ProfileGrid 'CellValueChanged' event so that
            // we can save the contents.
            this.xyPairsView.VariablesGrid.CellsChanged += this.OnVariablesGridCellValueChanged;

            // Trap the model changed event so that we can handle undo.
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            this.xyPairsView.VariablesGrid.ResizeControls();

            //this.initialWaterView.OnDepthWetSoilChanged += this.OnDepthWetSoilChanged;
            //this.initialWaterView.OnFilledFromTopChanged += this.OnFilledFromTopChanged;
            //this.initialWaterView.OnPAWChanged += this.OnPAWChanged;
            //this.initialWaterView.OnPercentFullChanged += this.OnPercentFullChanged;
            //this.initialWaterView.OnRelativeToChanged += this.OnRelativeToChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            this.xyPairsView.VariablesGrid.CellsChanged -= this.OnVariablesGridCellValueChanged;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// User has changed the value of one or more cells in the profile grid.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnVariablesGridCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            this.SaveGrid();

            // Refresh all calculated columns.
            this.RefreshCalculatedColumns();

            // Refresh the graph.
            if (this.graph != null)
            {
                this.graphPresenter.DrawGraph();
            }
        }

        /// <summary>
        /// Save the grid back to the model.
        /// </summary>
        private void SaveGrid()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            // Get the data source of the profile grid.
            DataTable data = this.xyPairsView.VariablesGrid.DataSource;

            // Maintain a list of all property changes that we need to make.
            List<Commands.ChangeProperty.Property> properties = new List<Commands.ChangeProperty.Property>();

            //add missing data as 0 otherwise it will throw an exception
            //could make this work as an entire row, but will stick to X & Y columns for now
            for (int Row = 0; Row != data.Rows.Count; Row++)
            {
                if (data.Rows[Row]["Y"].ToString() == "" && data.Rows[Row]["X"].ToString() != "")
                    data.Rows[Row]["Y"] = "0";
                if (data.Rows[Row]["X"].ToString() == "" && data.Rows[Row]["Y"].ToString() != "")
                    data.Rows[Row]["X"] = "0";
                if (data.Rows[Row]["Y"].ToString() == "" && data.Rows[Row]["X"].ToString() == "")
                    break;
            }
            
            // Loop through all non-readonly properties, get an array of values from the data table
            // for the property and then set the property value.
            for (int i = 0; i < this.propertiesInGrid.Count; i++)
            {
                // If this property is NOT readonly then set its value.
                if (!this.propertiesInGrid[i].IsReadOnly)
                {
                    // Get an array of values for this property.
                    Array values;
                    if (this.propertiesInGrid[i].DataType.GetElementType() == typeof(double))
                    {
                        values = DataTableUtilities.GetColumnAsDoubles(data, data.Columns[i].ColumnName);
                        if (!MathUtilities.ValuesInArray((double[])values))
                            values = null;
                        else
                            values = MathUtilities.RemoveMissingValuesFromBottom((double[])values);
                    }
                    else
                    {
                        values = DataTableUtilities.GetColumnAsStrings(data, data.Columns[i].ColumnName);
                        values = MathUtilities.RemoveMissingValuesFromBottom((string[])values);
                    }

                    // Is the value any different to the former property value?
                    bool changedValues;
                    if (this.propertiesInGrid[i].DataType == typeof(double[]))
                    {
                        changedValues = !MathUtilities.AreEqual((double[])values, (double[])this.propertiesInGrid[i].Value);
                    }
                    else
                    {
                        changedValues = !MathUtilities.AreEqual((string[])values, (string[])this.propertiesInGrid[i].Value);
                    }

                    if (changedValues)
                    {
                        // Store the property change.
                        Commands.ChangeProperty.Property property = new Commands.ChangeProperty.Property();
                        property.Name = this.propertiesInGrid[i].Name;
                        property.Obj = this.propertiesInGrid[i].Object;
                        property.NewValue = values;
                        properties.Add(property);
                    }
                }
            }

            // If there are property changes pending, then commit the changes in a block.
            if (properties.Count > 0)
            {
                Commands.ChangeProperty command = new Commands.ChangeProperty(properties);
                this.explorerPresenter.CommandHistory.Add(command);
            }

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Refresh the values of all calculated columns in the profile grid.
        /// </summary>
        private void RefreshCalculatedColumns()
        {
            // Loop through all calculated properties, get an array of values from the property
            // a give to profile grid.
            for (int i = 0; i < this.propertiesInGrid.Count; i++)
            {
                if (this.propertiesInGrid[i].IsReadOnly && i > 0)
                {
                    VariableProperty property = this.propertiesInGrid[i];
                    int col = i;
                    int row = 0;
                    foreach (object value in property.Value as IEnumerable<double>)
                    {
                        object valueForCell = value;
                        bool missingValue = (double)value == MathUtilities.MissingValue || double.IsNaN((double)value);

                        if (missingValue)
                        {
                            valueForCell = null;
                        }

                        IGridCell cell = this.xyPairsView.VariablesGrid.GetCell(col, row);
                        cell.Value = valueForCell;

                        row++;
                    }

                    // add a total to the column header if necessary.
                    double total = property.Total;
                    if (!double.IsNaN(total))
                    {
                        string columnName = property.Description;
                        if (property.Units != null)
                        {
                            columnName += "\r\n(" + property.Units + ")";
                        }

                        columnName = columnName + "\r\n" + total.ToString("N1") + " mm";

                        IGridColumn column = this.xyPairsView.VariablesGrid.GetColumn(col);
                        column.HeaderText = columnName;
                    }
                }
            }
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        void OnModelChanged(object changedModel)
        {
            if (changedModel == this.xyPairs)
                this.PopulateView();
        }
    }
}
