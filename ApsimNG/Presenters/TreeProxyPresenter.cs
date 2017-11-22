// -----------------------------------------------------------------------
// <copyright file="TreeProxyPresenter.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Models.Agroforestry;
    using Models.Core;
    using Models.Soils;
    using Views;

    /// <summary>
    /// The tree proxy presenter
    /// </summary>
    public class TreeProxyPresenter : IPresenter, IExportable
    {
        /// <summary>
        /// The forestry model object
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
        /// The mid points of the soil
        /// </summary>
        private double[] soilMidpoints;

        /// <summary>
        /// Attach the presenter
        /// </summary>
        /// <param name="model">The model object</param>
        /// <param name="view">The view object</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            forestryModel = model as TreeProxy;
            forestryViewer = view as TreeProxyView;

            AttachData();
            forestryViewer.OnCellEndEdit += OnCellEndEdit;
            forestryViewer.SetReadOnly();
            this.propertyPresenter = new PropertyPresenter();
            this.propertyPresenter.Attach(forestryModel, forestryViewer.ConstantsGrid, explorerPresenter);
        }

        /// <summary>
        /// Detach this presenter
        /// </summary>
        public void Detach()
        {
            propertyPresenter.Detach();
            SaveTable();
            forestryModel.dates = forestryViewer.SaveDates();
            forestryModel.heights = forestryViewer.SaveHeights();
            forestryModel.NDemands = forestryViewer.SaveNDemands();
            forestryModel.CanopyWidths = forestryViewer.SaveCanopyWidths();
            forestryModel.TreeLeafAreas = forestryViewer.SaveTreeLeafAreas();
            forestryViewer.OnCellEndEdit -= OnCellEndEdit;
        }

        /// <summary>
        /// Convert the object to html
        /// </summary>
        /// <param name="folder">The folder name</param>
        /// <returns>The html text</returns>
        public string ConvertToHtml(string folder)
        {
            // TODO: Implement
            return string.Empty;
        }

        /// <summary>
        /// Attach the model
        /// </summary>
        public void AttachData()
        {
            if (!(forestryModel.Parent is AgroforestrySystem))
            {
                throw new ApsimXException(forestryModel, "Error: TreeProxy must be a child of ForestrySystem.");
            }

            Soil soil;
            List<IModel> zones = Apsim.ChildrenRecursively(forestryModel.Parent, typeof(Zone));
            if (zones.Count == 0)
            {
                return;
            }

            // setup tree heights
            forestryViewer.SetupHeights(forestryModel.dates, forestryModel.heights, forestryModel.NDemands, forestryModel.CanopyWidths, forestryModel.TreeLeafAreas);

            /*      //get the distance of each Zone from Tree.
                  double zoneWidth = 0;
                  double[] ZoneWidths = new double[Zones.Count];

                  for (int i = 1; i < Zones.Count; i++) //skip first Zone with tree
                  {
                      if (Zones[i] is RectangularZone)
                          zoneWidth = (Zones[i] as RectangularZone).Width;
                      else if (Zones[i] is CircularZone)
                          zoneWidth = (Zones[i] as CircularZone).Width;

                          ZoneWidths[i] = ZoneWidths[i - 1] + zoneWidth;
                  }*/

            // get the first soil. For now we're assuming all soils have the same structure.
            soil = Apsim.Find(zones[0], typeof(Soil)) as Soil;

            forestryViewer.SoilMidpoints = soil.DepthMidPoints;
            
            // setup columns
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

                // setup rows
                List<string> rowNames = new List<string>();

                rowNames.Add("Shade (%)");
                rowNames.Add("Root Length Density (cm/cm3)");
                rowNames.Add("Depth (cm)");

                foreach (string s in soil.Depth)
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
                    // set Depth and RLD rows to empty strings
                    forestryModel.Table[i][1] = string.Empty;
                    forestryModel.Table[i][2] = string.Empty;
                }
            }
            else
            {
                // add Zones not in the table
                IEnumerable<string> except = colNames.Except(forestryModel.Table[0]);
                foreach (string s in except)
                {
                    forestryModel.Table.Add(Enumerable.Range(1, forestryModel.Table[1].Count).Select(x => "0").ToList());
                }

                forestryModel.Table[0].AddRange(except);
                for (int i = 2; i < forestryModel.Table.Count; i++) 
                {
                    // set Depth and RLD rows to empty strings
                    forestryModel.Table[i][2] = string.Empty;

                    // ForestryModel.Table[i][3] = string.Empty;
                }

                // remove Zones from table that don't exist in simulation
                except = forestryModel.Table[0].Except(colNames);
                List<int> indexes = new List<int>();
                foreach (string s in except.ToArray())
                {
                    indexes.Add(forestryModel.Table[0].FindIndex(x => s == x));
                }

                indexes.Sort();
                indexes.Reverse();

                foreach (int i in indexes)
                {
                    forestryModel.Table[0].RemoveAt(i);
                    forestryModel.Table.RemoveAt(i + 1);
                }
            }

            forestryViewer.SetupGrid(forestryModel.Table);
        }

        /// <summary>
        /// Save the data table
        /// </summary>
        private void SaveTable()
        {
            DataTable table = forestryViewer.GetTable();

            if (table == null)
            {
                return;
            }

            for (int i = 0; i < forestryModel.Table[1].Count; i++)
            {
                for (int j = 2; j < table.Columns.Count + 1; j++)
                {
                    forestryModel.Table[j][i] = table.Rows[i].Field<string>(j - 1);
                }
            }
        }

        /// <summary>
        /// Edit the cell
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnCellEndEdit(object sender, EventArgs e)
        {
            SaveTable();
            AttachData();
        }
    }
}