namespace UserInterface.Presenters
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Models.Agroforestry;
    using Models.Core;
    using Models.Soils;
    using Views;
    using Commands;
    using EventArguments;

    /// <summary>
    /// The tree proxy presenter
    /// </summary>
    public class TreeProxyPresenter : IPresenter
    {
        /// <summary>
        /// The forestry model object.
        /// </summary>
        private TreeProxy forestryModel;

        /// <summary>
        /// The viewer for the forestry model
        /// </summary>
        private TreeProxyView forestryViewer;

        /// <summary>
        /// The property presenter
        /// </summary>
        private PropertyPresenter propertyPresenter;

        /// <summary>
        /// Presenter for the view's spatial data grid.
        /// </summary>
        private GridPresenter spatialGridPresenter = new GridPresenter();

        /// <summary>
        /// Presenter for the view's temporal data grid.
        /// </summary>
        private GridPresenter temporalGridPresenter = new GridPresenter();

        /// <summary>
        /// The explorer presenter.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// Attach the presenter to the model and view.
        /// </summary>
        /// <param name="model">The model object.</param>
        /// <param name="view">The view object.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            forestryModel = model as TreeProxy;
            forestryViewer = view as TreeProxyView;
            presenter = explorerPresenter;

            AttachData();
            forestryViewer.OnCellEndEdit += OnCellEndEdit;
            presenter.CommandHistory.ModelChanged += OnModelChanged;

            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(forestryModel, forestryViewer.ConstantsGrid, explorerPresenter);
            spatialGridPresenter.Attach(forestryModel, forestryViewer.SpatialDataGrid, explorerPresenter);
            temporalGridPresenter.Attach(forestryModel, forestryViewer.TemporalDataGrid, explorerPresenter);
        }

        /// <summary>
        /// Detach this presenter
        /// </summary>
        public void Detach()
        {
            spatialGridPresenter.Detach();
            temporalGridPresenter.Detach();
            propertyPresenter.Detach();
            SaveTable();
            forestryViewer.OnCellEndEdit -= OnCellEndEdit;
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Attach the model
        /// </summary>
        public void AttachData()
        {
            if (!(forestryModel.Parent is AgroforestrySystem))
                throw new ApsimXException(forestryModel, "Error: TreeProxy must be a child of ForestrySystem.");

            Soil soil;
            List<IModel> zones = Apsim.ChildrenRecursively(forestryModel.Parent, typeof(Zone));
            if (zones.Count == 0)
                return;

            // Setup tree heights.
            forestryViewer.SetupHeights(forestryModel.Dates, forestryModel.Heights, forestryModel.NDemands, forestryModel.ShadeModifiers);

            // Get the first soil. For now we're assuming all soils have the same structure.
            soil = Apsim.Find(zones[0], typeof(Soil)) as Soil;

            forestryViewer.SoilMidpoints = soil.DepthMidPoints;
            
            // Setup columns.
            List<string> colNames = new List<string>();

            colNames.Add("Parameter");
            colNames.Add("0");
            colNames.Add("0.5h");
            colNames.Add("1h");
            colNames.Add("1.5h");
            colNames.Add("2h");
            colNames.Add("2.5h");
            colNames.Add("3h");
            colNames.Add("4h");
            colNames.Add("5h");
            colNames.Add("6h");

            if (forestryModel.Table.Count == 0)
            {
                forestryModel.Table = new List<List<string>>();
                forestryModel.Table.Add(colNames);

                // Setup rows.
                List<string> rowNames = new List<string>();

                rowNames.Add("Shade (%)");
                rowNames.Add("Root Length Density (cm/cm3)");
                rowNames.Add("Depth (cm)");

                foreach (string s in APSIM.Shared.APSoil.SoilUtilities.ToDepthStrings(soil.Thickness))
                {
                    rowNames.Add(s);
                }

                forestryModel.Table.Add(rowNames);
                for (int i = 2; i < colNames.Count + 1; i++)
                {
                    forestryModel.Table.Add(Enumerable.Range(1, rowNames.Count).Select(x => "0").ToList());
                }

                for (int i = 2; i < forestryModel.Table.Count; i++)
                {
                    // Set Depth and RLD rows to empty strings.
                    forestryModel.Table[i][1] = string.Empty;
                    forestryModel.Table[i][2] = string.Empty;
                }
            }
            else
            {
                // add Zones not in the table
                IEnumerable<string> except = colNames.Except(forestryModel.Table[0]);
                foreach (string s in except)
                    forestryModel.Table.Add(Enumerable.Range(1, forestryModel.Table[1].Count).Select(x => "0").ToList());

                forestryModel.Table[0].AddRange(except);
                for (int i = 2; i < forestryModel.Table.Count; i++) 
                {
                    // Set Depth and RLD rows to empty strings.
                    forestryModel.Table[i][2] = string.Empty;
                }

                // Remove Zones from table that don't exist in simulation.
                except = forestryModel.Table[0].Except(colNames);
                List<int> indexes = new List<int>();
                foreach (string s in except.ToArray())
                    indexes.Add(forestryModel.Table[0].FindIndex(x => s == x));

                indexes.Sort();
                indexes.Reverse();

                foreach (int i in indexes)
                {
                    forestryModel.Table[0].RemoveAt(i);
                    forestryModel.Table.RemoveAt(i + 1);
                }
            }
            forestryViewer.SpatialData = forestryModel.Table;
        }

        /// <summary>
        /// Save the data table
        /// </summary>
        private void SaveTable()
        {
            // It would be better to check what precisely has been changed, but for now,
            // just load all of the UI components into one big ChangeProperty command, and
            // execute the command.
            ChangeProperty changeTemporalData = new ChangeProperty(new List<ChangeProperty.Property>()
            {
                new ChangeProperty.Property(forestryModel, "Table", forestryViewer.SpatialData),
                new ChangeProperty.Property(forestryModel, "Dates", forestryViewer.Dates.ToArray()),
                new ChangeProperty.Property(forestryModel, "Heights", forestryViewer.Heights.ToArray()),
                new ChangeProperty.Property(forestryModel, "NDemands", forestryViewer.NDemands.ToArray()),
                new ChangeProperty.Property(forestryModel, "ShadeModifiers", forestryViewer.ShadeModifiers.ToArray())
            });
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            presenter.CommandHistory.Add(changeTemporalData);
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Invoked when the model has been changed via the undo command.
        /// </summary>
        /// <param name="changedModel">The model which has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            propertyPresenter.UpdateModel(forestryModel);
            forestryViewer.SpatialData = forestryModel.Table;
            forestryViewer.SetupHeights(forestryModel.Dates, forestryModel.Heights, forestryModel.NDemands, forestryModel.ShadeModifiers);
        }

        /// <summary>
        /// Edit the cell
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnCellEndEdit(object sender, GridCellsChangedArgs e)
        {
            GridView grid = sender as GridView;
            // fixme - need some (any!) data validation but it will
            // require a partial rewrite of TreeProxy.
            foreach (GridCellChangedArgs cell in e.ChangedCells)
                grid.DataSource.Rows[cell.RowIndex][cell.ColIndex] = cell.NewValue;

            SaveTable();
            AttachData();
        }
    }
}