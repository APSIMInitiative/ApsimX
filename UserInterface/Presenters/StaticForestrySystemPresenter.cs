namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Windows.Forms;
    using System.Linq;
    using System.Text;
    using Models.Core;
    using Models.Soils;
    using Models;
    using Views;

    public class StaticForestrySystemPresenter : IPresenter, IExportable
    {
        private StaticForestrySystem ForestryModel;
        private StaticForestrySystemView ForestryViewer;

        public double[] SoilMidpoints;
        
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            ForestryModel = model as StaticForestrySystem;
            ForestryViewer = view as StaticForestrySystemView;

            AttachData();
            ForestryViewer.OnCellEndEdit += OnCellEndEdit;         
        }

        public void Detach()
        { 
            SaveTable();
            ForestryViewer.OnCellEndEdit -= OnCellEndEdit;
        }

        private void SaveTable()
        {
            ForestryModel.NDemand = ForestryViewer.NDemand;
            ForestryModel.RootRadius = ForestryViewer.RootRadius;

            DataTable table = ForestryViewer.GetTable();

            if (table == null)
                return;
            
            for (int i = 0; i < ForestryModel.Table[1].Count; i++)
                for (int j = 2; j < table.Columns.Count + 1; j++)
                {
                    ForestryModel.Table[j][i] = table.Rows[i].Field<string>(j-1);
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
            ForestryViewer.NDemand = ForestryModel.NDemand;
            ForestryViewer.RootRadius = ForestryModel.RootRadius;

            Soil Soil;
            List<IModel> Zones = Apsim.ChildrenRecursively(ForestryModel, typeof(Zone));
            if (Zones.Count == 0)
                return;
            //get the first soil. For now we're assuming all soils have the same structure.
            Soil = Apsim.Find(Zones[0], typeof(Soil)) as Soil;

            ForestryViewer.SoilMidpoints = Soil.DepthMidPoints;
            //setup columns
            List<string> colNames = new List<string>();

            colNames.Add("Parameter");
            foreach (Zone z in Zones)
            {
                if (z is Simulation)
                    continue;
                colNames.Add(z.Name);
            }

            if (ForestryModel.Table.Count == 0)
            {
                ForestryModel.Table = new List<List<String>>();
                ForestryModel.Table.Add(colNames);

                //setup rows
                List<string> rowNames = new List<string>();

                rowNames.Add("Wind reduction (%)");
                rowNames.Add("Shade (%)");
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