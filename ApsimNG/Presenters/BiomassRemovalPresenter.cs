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
    class BiomassRemovalPresenter : PropertyPresenter, IPresenter
    {
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
        }

        /// <summary>
        /// Detaches the model from the view.
        /// </summary>
        public override void Detach()
        {
            base.Detach();
        }

        /// <summary>
        /// Overrides the property presenter's FindAllProperties method.
        /// Finds properties for all child models of type <see cref="OrganBiomassRemovalType"/>.
        /// </summary>
        /// <param name="model">Base model.</param>
        protected override void FindAllProperties(IModel model)
        {
            List<IModel> children = Apsim.Children(model, typeof(OrganBiomassRemovalType));
            if (children == null)
                return;

            foreach (IModel child in children)
                base.FindAllProperties(child);
        }

        /// <summary>
        /// Fills the table with data to be displayed by the grid view.
        /// Differs to the base class method in that at the start of each
        /// new model's properties it inserts a separator row showing the
        /// name of the new model.
        /// </summary>
        /// <param name="table"></param>
        protected override void FillTable(DataTable table)
        {
            // Model to which the previous property belonged
            IModel previous = null;

            foreach (IVariable property in properties)
            {
                IModel current = property.Object as IModel;

                if (property.Object != previous)
                {
                    // If this property's model is different to the
                    // previous property's model, insert a separator
                    // row showing the name of the new model.
                    table.Rows.Add(current.Name, " ");
                    grid.SetRowAsSeparator(table.Rows.Count - 1);
                }

                AddPropertyToTable(table, property);

                previous = current;
            }
        }

        protected override IVariable GetProperty(int row, int column)
        {
            if (row >= grid.DataSource.Rows.Count)
                throw new Exception($"Attempted to get the property in row '{row}', but the grid only contains '{grid.DataSource.Rows.Count}' rows.");
            if (row < 0)
                throw new Exception($"Attempted to get the property in row '{row}'");

            // The index of the property will be the row number minus
            // the number of separator rows before this row.
            int numSeparators = 0;
            for (int i = 0; i < row; i++)
                if (grid.IsSeparator(i))
                    numSeparators++;

            return properties[row - numSeparators];
        }
    }
}
