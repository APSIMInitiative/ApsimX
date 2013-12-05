using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using UserInterface.Views;
using System.Reflection;
using Models.Core;

namespace UserInterface.Presenters
{
    class PropertyPresenter : IPresenter
    {
        private IGridView Grid;
        private Model Model;
        private CommandHistory CommandHistory;
        private List<Utility.IVariable> Properties = new List<Utility.IVariable>();

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Grid = View as IGridView;
            this.Model = Model as Model;
            this.CommandHistory = CommandHistory;

            PopulateGrid(this.Model);
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
        }

        /// <summary>
        /// Return true if the grid is empty of rows.
        /// </summary>
        public bool IsEmpty { get { return Grid.RowCount == 0; } }

        /// <summary>
        /// Populate the grid
        /// </summary>
        public void PopulateGrid(Model model)
        {
            Model = model;
            DataTable Table = new DataTable();
            Table.Columns.Add("Description", typeof(string));
            Table.Columns.Add("Value", typeof(object));

            GetAllProperties(Table);
            Grid.DataSource = Table;
            FormatGrid();
        }

        /// <summary>
        /// Get a list of all properties from the model that we're going to work with.
        /// </summary>
        /// <param name="Table"></param>
        private void GetAllProperties(DataTable Table)
        {
            Properties.Clear();
            if (Model != null)
            {
                foreach (Utility.IVariable parameter in Utility.ModelFunctions.Parameters(Model))
                {
                    string PropertyName = parameter.Name;
                    if (parameter.Description != null)
                        PropertyName = parameter.Description;
                    Table.Rows.Add(new object[] { PropertyName, parameter.Value });
                    Properties.Add(parameter);

                }
            }
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        private void FormatGrid()
        {
            for (int i = 0; i < Properties.Count; i++)
                Grid.SetCellEditor(1, i, Properties[i].Value);
            Grid.SetColumnAutoSize(0);
            Grid.SetColumnAutoSize(1);
            
            Grid.SetColumnReadOnly(0, true);
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        private void OnCellValueChanged(int Col, int Row, object OldValue, object NewValue)
        {
            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Model,
                                                                                    Properties[Row].Name,
                                                                                    NewValue);
            //Stop the recursion. The users entry is the updated value in the grid.
            CommandHistory.ModelChanged -= OnModelChanged;
            CommandHistory.Add(Cmd, true);
            CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        private void OnModelChanged(object ChangedModel)
        {
            if (ChangedModel == Model)
                PopulateGrid(Model);
        }

    }
}
