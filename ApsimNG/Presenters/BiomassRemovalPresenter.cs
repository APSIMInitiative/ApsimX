namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Views;
    using Interfaces;
    using Models.PMF;
    using Models.PMF.Library;
    using Models.Core;
    using System.Data;
    using EventArguments;
    using Commands;
    using System.Reflection;

    /// <summary>
    /// Presenter for the <see cref="BiomassRemoval"/> class.
    /// Displays the properties for all <see cref="OrganBiomassRemovalType"/> children.
    /// </summary>
    class BiomassRemovalPresenter : GridPresenter, IPresenter
    {
        /// <summary>
        /// The BiomassRemoval model.
        /// </summary>
        private BiomassRemoval model;

        /// <summary>
        /// Attaches the model to the view.
        /// </summary>
        /// <param name="model">The BiomassRemoval model to connect.</param>
        /// <param name="view">The view. Should be a <see cref="GridView"/>.</param>
        /// <param name="explorerPresenter">The explorer presenter controlling this presenter.</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            if (!(model is BiomassRemoval))
                throw new Exception(string.Format("{0} cannot be used to display a model of type {1}.", GetType().Name, model.GetType().Name));

            base.Attach(model, view, explorerPresenter);
            this.model = model as BiomassRemoval;
            grid.CanGrow = false;
            DataTable data = new DataTable();
            List<IModel> removalTypes = Apsim.Children(model as IModel, typeof(OrganBiomassRemovalType)).ToList();
            bool hasData = removalTypes.Any();
            data.Columns.Add(hasData ? "Description" : "No values are currently available", typeof(string));
            data.Columns.Add(hasData ? "Value" : " ", typeof(object));

            foreach (IModel child in removalTypes)
            {
                data.Rows.Add(child.Name, " ");
                grid.SetRowAsSeparator(data.Rows.Count - 1);

                PropertyPresenter propertyHandler = new PropertyPresenter();
                propertyHandler.FindAllProperties(child as Model);
                propertyHandler.FillTable(data);
            }

            grid.DataSource = data;
            grid.GetColumn(0).ReadOnly = true;
            grid.CellsChanged += OnGridChanged;
        }

        /// <summary>
        /// Detaches the model from the view.
        /// </summary>
        public override void Detach()
        {
            grid.CellsChanged -= OnGridChanged;
            base.Detach();
        }

        /// <summary>
        /// Each row in the grid displays a property of a RemovalType model.
        /// This method finds the index of the model whose data is being 
        /// displayed on a given row.
        /// </summary>
        /// <param name="row">Index of the row.</param>
        /// <returns>Index of the model, or -1 if not found.</returns>
        private int GetModelIndex(int row)
        {
            if (row >= grid.DataSource.Rows.Count)
                throw new Exception(string.Format("Attempted to get the removal type for row {0}, but the grid only contains {1} rows.", row + 1, grid.DataSource.Rows.Count));
            if (row < 0)
                throw new Exception(string.Format("Attempted to get the removal type for row {0}", row));

            // Step backwards through the rows in the grid until we find a separator row.
            for (int i = row; i >= 0; i--)
                if (grid.IsSeparator(i))
                    return i;
            return -1;
        }

        /// <summary>
        /// Invoked whenever the user modifies the contents of the grid.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnGridChanged(object sender, GridCellsChangedArgs args)
        {
            try
            {
                if (args.InvalidValue)
                    throw new Exception("The value you entered was not valid for its datatype.");
                foreach (IGridCell cell in args.ChangedCells)
                {
                    int index = GetModelIndex(cell.RowIndex);
                    IModel removalType = Apsim.Child(model, grid.GetCell(0, index).Value.ToString());
                    if (removalType != null)
                    {
                        List<MemberInfo> members = PropertyPresenter.GetMembers(removalType);
                        MemberInfo member = members[cell.RowIndex - index - 1];
                        IVariable property = null;
                        if (member is PropertyInfo)
                            property = new VariableProperty(model, member as PropertyInfo);
                        else if (member is FieldInfo)
                            property = new VariableField(model, member as FieldInfo);
                        else
                            throw new Exception(string.Format("Unable to find property {0} in model {1}", grid.GetCell(0, cell.RowIndex).Value.ToString(), removalType.Name));
                        object value = PropertyPresenter.FormatValueForProperty(property, cell.Value);

                        ChangeProperty command = new ChangeProperty(removalType, property.Name, value);
                        presenter.CommandHistory.Add(command);
                    }
                }
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }
    }
}
