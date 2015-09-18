namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Windows.Forms;
    using System.Linq;
    using System.Text;
    using Models.Agroforestry;
    using Models.Core;
    using Models.Soils;
    using Models.Zones;
    using Models;
    using Views;

    public class TreeProxyPresenter : IPresenter, IExportable
    {
        private TreeProxy ForestryModel;
        private TreeProxyView ForestryViewer;
        private PropertyPresenter propertyPresenter;

        public double[] SoilMidpoints;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            ForestryModel = model as TreeProxy;
            ForestryViewer = view as TreeProxyView;

            AttachData();
            ForestryViewer.OnCellEndEdit += OnCellEndEdit;

            this.propertyPresenter = new PropertyPresenter();
            this.propertyPresenter.Attach(ForestryModel, ForestryViewer.ConstantsGrid, explorerPresenter);

        }

        public void Detach()
        {
            propertyPresenter.Detach();
            SaveTable();
            ForestryModel.dates = ForestryViewer.SaveDates();
            ForestryModel.heights = ForestryViewer.SaveHeights();
            ForestryModel.NDemands = ForestryViewer.SaveNDemands();
            ForestryViewer.OnCellEndEdit -= OnCellEndEdit;
        }

        private void SaveTable()
        {

            DataTable table = ForestryViewer.GetTable();

            if (table == null)
                return;

            for (int i = 0; i < ForestryModel.Table[1].Count; i++)
                for (int j = 2; j < table.Columns.Count + 1; j++)
                {
                    ForestryModel.Table[j][i] = table.Rows[i].Field<string>(j - 1);
                }
        }

        public string ConvertToHtml(string folder)
        {
            // TODO: Implement
            return string.Empty;
        }

        private void OnCellEndEdit(object sender, EventArgs e)
        {
            SaveTable();
            AttachData();
        }

        public void AttachData()
        {
            if (!(ForestryModel.Parent is ForestrySystem))
                throw new ApsimXException(ForestryModel, "Error: TreeProxy must be a child of ForestrySystem.");

            Soil Soil;
            List<IModel> Zones = Apsim.ChildrenRecursively(ForestryModel.Parent, typeof(Zone));
            if (Zones.Count == 0)
                return;

            //setup tree heights
            ForestryViewer.SetupHeights(ForestryModel.dates, ForestryModel.heights,ForestryModel.NDemands);

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

            //get the first soil. For now we're assuming all soils have the same structure.
            Soil = Apsim.Find(Zones[0], typeof(Soil)) as Soil;

            ForestryViewer.SoilMidpoints = Soil.DepthMidPoints;
            //setup columns
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

            if (ForestryModel.Table.Count == 0)
            {
                ForestryModel.Table = new List<List<String>>();
                ForestryModel.Table.Add(colNames);

                //setup rows
                List<string> rowNames = new List<string>();

                rowNames.Add("Shade (%)");
                rowNames.Add("Nitrogen Demand (kg/ha)");
                rowNames.Add("Root Length Density (cm/cm3)");
                rowNames.Add("Depth (cm)");

                foreach (string s in Soil.Depth)
                {
                    rowNames.Add(s);
                }

                ForestryModel.Table.Add(rowNames);
                for (int i = 2; i < colNames.Count + 1; i++)
                {
                    ForestryModel.Table.Add(Enumerable.Range(1, rowNames.Count).Select(x => "0").ToList());
                }

                for (int i = 2; i < ForestryModel.Table.Count; i++) //set Depth and RLD rows to empty strings
                {
                    ForestryModel.Table[i][2] = string.Empty;
                    ForestryModel.Table[i][3] = string.Empty;
                }
            }
            else
            {
                // add Zones not in the table
                IEnumerable<string> except = colNames.Except(ForestryModel.Table[0]);
                foreach (string s in except)
                    ForestryModel.Table.Add(Enumerable.Range(1, ForestryModel.Table[1].Count).Select(x => "0").ToList());
                ForestryModel.Table[0].AddRange(except);
                for (int i = 2; i < ForestryModel.Table.Count; i++) //set Depth and RLD rows to empty strings
                {
                    ForestryModel.Table[i][2] = string.Empty;
                    ForestryModel.Table[i][3] = string.Empty;
                }

                // remove Zones from table that don't exist in simulation
                except = ForestryModel.Table[0].Except(colNames);
                List<int> indexes = new List<int>();
                foreach (string s in except.ToArray())
                {
                    indexes.Add(ForestryModel.Table[0].FindIndex(x => s == x));
                }

                indexes.Sort();
                indexes.Reverse();

                foreach (int i in indexes)
                {
                    ForestryModel.Table[0].RemoveAt(i);
                    ForestryModel.Table.RemoveAt(i + 1);
                }
            }
            ForestryViewer.SetupGrid(ForestryModel.Table);
        }
    }
}