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
        private ExplorerPresenter ExplorerPresenter;
        private List<PropertyInfo> Properties = new List<PropertyInfo>();

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Grid = View as IGridView;
            this.Model = Model as Model;
            this.ExplorerPresenter = explorerPresenter;

            PopulateGrid(this.Model);
            Grid.CellValueChanged += OnCellValueChanged;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            Grid.CellValueChanged -= OnCellValueChanged;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
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
                foreach (PropertyInfo parameter in Model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    // Ignore properties that have an [XmlIgnore], are an array or are 'Name'
                    Attribute XmlIgnore = Utility.Reflection.GetAttribute(parameter, typeof(System.Xml.Serialization.XmlIgnoreAttribute), true);
                    bool ignoreProperty = XmlIgnore != null;                                 // No [XmlIgnore]
                    ignoreProperty |= parameter.PropertyType.GetInterface("IList") != null;   // No List<T>
                    ignoreProperty |= parameter.Name == "Name";   // No Name properties.
                    ignoreProperty |= !parameter.CanWrite;         // Must be readwrite

                    if (!ignoreProperty)
                    {
                        string PropertyName = parameter.Name;
                        Attribute description = Utility.Reflection.GetAttribute(parameter, typeof(Description), true);
                        if (description != null)
                            PropertyName = description.ToString();
                        Table.Rows.Add(new object[] { PropertyName, parameter.GetValue(Model, null) });
                        Properties.Add(parameter);
                    }
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
            Grid.SetColumnSize(0);
            Grid.SetColumnSize(1);
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
            // Stop the recursion. The users entry is the updated value in the grid.
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            ExplorerPresenter.CommandHistory.Add(Cmd, true);
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
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
