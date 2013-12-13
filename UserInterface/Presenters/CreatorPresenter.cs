using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;
using System.Data;
using Models.Core;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A presenter class for conncecting a Creator "Model" to a "GridView"
    /// </summary>
    public class CreatorPresenter : IPresenter
    {
        private Creator Creator;
        private IGridView Grid;
        private CommandHistory CommandHistory;
        private List<string> SimulationNames = new List<string>();
        private List<string> ModelNames = new List<string>();
        private List<string> VariableNames = new List<string>();

        /// <summary>
        /// Attach Model to View.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Creator = model as Creator;
            Grid = view as IGridView;
            CommandHistory = commandHistory;

            // create a list of simulation names, model names and variable names.
            foreach (Model modelInScope in Creator.FindAll())
            {
                if (modelInScope is Simulation)
                    SimulationNames.Add(modelInScope.Name);
                ModelNames.Add(modelInScope.FullPath);

                foreach (Utility.IVariable variable in Utility.ModelFunctions.Parameters(modelInScope))
                    VariableNames.Add(modelInScope.FullPath + "." + variable.Name);
            }
            
            PopulateGrid();
            Grid.CellValueChanged += OnCellValueChanged;
            CommandHistory.ModelChanged += OnModelChanged;
        }


        /// <summary>
        /// Detach Model from View.
        /// </summary>
        public void Detach()
        {
            Grid.CellValueChanged -= OnCellValueChanged;
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        private void PopulateGrid()
        {
            DataTable table = GetDataTableFromModel();
            Grid.DataSource = table;
            Grid.RowCount = 50;


            // Set up cell editors for all cells.
            for (int rowIndex = 0; rowIndex < Grid.RowCount; rowIndex++)
            {
                Grid.SetCellEditor(1, rowIndex, SimulationNames.ToArray());
                Grid.SetCellEditor(2, rowIndex, Creator.Description.ActionSpecifier.ActionEnum.Set);
                Grid.SetCellEditor(3, rowIndex, VariableNames.ToArray());

                object actionName = Grid.GetCellValue(2, rowIndex);
                object path = Grid.GetCellValue(3, rowIndex);
                if (actionName != null && path != null)
                {
                    object value = Creator.Get(path.ToString());
                    if (value != null)
                        Grid.SetCellEditor(4, rowIndex, value);
                }
            }
            Grid.SetColumnSize(0, 100);
            Grid.SetColumnSize(1, 100);
            Grid.SetColumnSize(2, 100);
            Grid.SetColumnSize(3, 400);
            Grid.SetColumnSize(4, 100);
        }

        /// <summary>
        /// Convert the Creator to a DataTable ready for the grid.
        /// </summary>
        private DataTable GetDataTableFromModel()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Base", typeof(string));
            table.Columns.Add("Action", typeof(string));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Value", typeof(string));

            if (Creator.Descriptions != null)
                foreach (Creator.Description description in Creator.Descriptions)
                {
                    DataRow row = table.NewRow();

                    row[0] = description.Name;
                    row[1] = description.Base;
                    if (description.Actions != null && description.Actions.Count > 0)
                    {
                        DescriptionActionToDataRow(description.Actions[0], row);
                        table.Rows.Add(row);

                        for (int i = 1; i < description.Actions.Count; i++)
                        {
                            row = table.NewRow();

                            row[0] = description.Name;
                            row[1] = description.Base;
                            DescriptionActionToDataRow(description.Actions[i], row);
                            table.Rows.Add(row);
                        }
                    }
                }

            return table;
        }

        /// <summary>
        /// Convert the specified description action specifier to the DataRow.
        /// </summary>
        private static void DescriptionActionToDataRow(Creator.Description.ActionSpecifier actionSpecifier, DataRow row)
        {
            row[2] = actionSpecifier.Action.ToString();
            if (actionSpecifier.Path != null)
                row[3] = actionSpecifier.Path;
            if (actionSpecifier.Value != null)
                row[4] = actionSpecifier.Value;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        void OnCellValueChanged(int Col, int Row, object OldValue, object NewValue)
        {
            List<Creator.Description> descriptions = new List<Creator.Description>();
            string previousName = null;
            for (int rowIndex = 0; rowIndex < Grid.RowCount; rowIndex++)
            {
                if (!Grid.RowIsEmpty(rowIndex))
                {
                    // determine the index into the clonedModels array 
                    object newModelName = Grid.GetCellValue(0, rowIndex);
                    object baseName = Grid.GetCellValue(1, rowIndex);
                    object actionName = Grid.GetCellValue(2, rowIndex);
                    object path = Grid.GetCellValue(3, rowIndex);
                    object value = Grid.GetCellValue(4, rowIndex);


                    int descriptionIndex = Row;
                    //if (previousName != null && newModelName != null && newModelName != previousName)
                    //{
                    //    descriptionIndex++;
                    //}

                    // make sure we have the correct number of descriptions in the array.
                    if (descriptionIndex >= descriptions.Count)
                        descriptions.Add(new Models.Creator.Description());

                    // Set the description and base names.
                    if (newModelName != null)
                        descriptions[descriptionIndex].Name = newModelName.ToString();

                    if (baseName != null)
                        descriptions[descriptionIndex].Base = baseName.ToString();

                    // Add an action if we have an actionName or a Path or a Value
                    Creator.Description.ActionSpecifier action = new Creator.Description.ActionSpecifier();
                    if (actionName != null || path != null || value != null)
                        descriptions[descriptionIndex].Actions.Add(action);

                    // Set the action
                    if (actionName != null)
                        action.Action = (Creator.Description.ActionSpecifier.ActionEnum) Enum.Parse(typeof(Creator.Description.ActionSpecifier.ActionEnum), actionName.ToString());

                    // Set the path
                    if (path != null)
                        action.Path = path.ToString();

                    // Set the value
                    if (value != null)
                        action.Value = value;

                    if (newModelName != null)
                        previousName = newModelName.ToString();
                }
            }

            CommandHistory.Add(new Commands.ChangePropertyCommand(Creator, "Descriptions", descriptions.ToArray()));
        }

        /// <summary>
        /// The model has changed - update our view.
        /// </summary>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == Creator)
            {
                PopulateGrid();
            }
        }

    }
}
