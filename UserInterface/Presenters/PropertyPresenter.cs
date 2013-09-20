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
        private object Model;
        private CommandHistory CommandHistory;
        private List<PropertyInfo> Properties = new List<PropertyInfo>();

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Grid = View as IGridView;
            this.Model = Model;
            this.CommandHistory = CommandHistory;

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
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        private void PopulateGrid()
        {
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
            foreach (PropertyInfo Property in Model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                // Only consider properties that have a public setter.
                if (Property.Name != "Name" && Property.Name != "Parent" && Property.GetAccessors().Length == 2 && Property.GetAccessors()[1].IsPublic)
                {
                    string PropertyName = Property.Name;
                    if (Property.IsDefined(typeof(Description), false))
                    {
                        Description Desc = Property.GetCustomAttributes(typeof(Description), false)[0] as Description;
                        PropertyName = Desc.ToString();
                    }
                    Table.Rows.Add(new object[] { PropertyName, Property.GetValue(Model, null) });
                    Properties.Add(Property);
                }
            }
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        private void FormatGrid()
        {
            for (int i = 0; i < Properties.Count; i++)
                Grid.SetCellEditor(1, i, Properties[i].GetValue(Model, null));
            Grid.SetColumnAutoSize(0);
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
            //Stop the recursion, the user entry is the updated value
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
                PopulateGrid();
        }


    }
}
