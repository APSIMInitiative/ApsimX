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
                    //ignoreProperty |= parameter.PropertyType.GetInterface("IList") != null;   // No List<T>
                    ignoreProperty |= parameter.Name == "Name";   // No Name properties.
                    ignoreProperty |= !parameter.CanRead;         // Must be read
                    ignoreProperty |= !parameter.CanWrite;         // Must be write

                    if (parameter.PropertyType.GetInterface("IList") != null &&
                        parameter.PropertyType != typeof(double[]) &&
                        parameter.PropertyType != typeof(int[]) &&
                        parameter.PropertyType != typeof(string[]))

                        ignoreProperty = true;

                    if (!ignoreProperty)
                    {
                        string PropertyName = parameter.Name;
                        Attribute description = Utility.Reflection.GetAttribute(parameter, typeof(Description), true);
                        if (description != null)
                            PropertyName = description.ToString();
                        Table.Rows.Add(new object[] { PropertyName, GetPropertyValue(parameter) });
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
                Grid.SetCellEditor(1, i, GetPropertyValue(Properties[i]));
            Grid.SetColumnSize(0);
            Grid.SetColumnSize(1);
            Grid.SetColumnReadOnly(0, true);
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        private void OnCellValueChanged(int Col, int Row, object OldValue, object NewValue)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            SetPropertyValue(Properties[Row], NewValue);
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Get the value of the specified property
        /// </summary>
        private object GetPropertyValue(PropertyInfo property)
        {
            object value = property.GetValue(Model, null);
            if (value == null)
                return null;

            if (property.PropertyType.IsArray)
            {
                string stringValue = "";
                Array arr = value as Array;
                for (int j = 0; j < arr.Length; j++)
                {
                    if (j > 0)
                        stringValue += ",";
                    stringValue += arr.GetValue(j).ToString();
                }
                value = stringValue;
            }
            return value;
        }

         /// <summary>
        /// Set the value of the specified property
        /// </summary>
        private void SetPropertyValue(PropertyInfo property, object newValue)
        {
            object value;
            if (property.PropertyType.IsArray)
            {
                string[] stringValues = newValue.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (property.PropertyType == typeof(double[]))
                    value = Utility.Math.StringsToDoubles(stringValues);
                else if (property.PropertyType == typeof(int[]))
                    value = Utility.Math.StringsToDoubles(stringValues);
                else if (property.PropertyType == typeof(string[]))
                    value = stringValues;
                else
                    throw new ApsimXException(Model.FullPath, "Invalid property type: " + property.PropertyType.ToString());
            }
            else
                value = newValue;

            Commands.ChangePropertyCommand Cmd = new Commands.ChangePropertyCommand(Model,
                                                                                    property.Name,
                                                                                    value);
            ExplorerPresenter.CommandHistory.Add(Cmd, true);
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
