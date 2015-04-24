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

    public class ForestryPresenter : IPresenter, IExportable
    {
        private Forestry ForestryModel;
        private ForestryView ForestryViewer;
        private Simulation Sim;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            ForestryModel = model as Forestry;
            ForestryViewer = view as ForestryView;
            Sim = (Simulation)ForestryModel.Parent;

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
            DataTable table = ForestryViewer.GetTable();
            
            for (int i = 0; i < ForestryModel.Table[1].Count; i++)
                for (int j = 2; j < table.Columns.Count + 1; j++)
                {
                  //  if (i == 2 && j > 0)
                  //      ForestryModel.Table[j][i] = string.Empty;
                  //  else
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
            List<IModel> Zones = Apsim.FindAll(Sim, typeof(Zone));
            Soil Soil = Apsim.Find(Sim, typeof(Soil)) as Soil;
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

                rowNames.Add("% Wind");
                rowNames.Add("% Shade");
                rowNames.Add("Depth");

                foreach (string s in Soil.Depth)
                {
                    rowNames.Add(s);
                }

                ForestryModel.Table.Add(rowNames);
                for (int i = 2; i < colNames.Count + 1; i++)
                {
                    ForestryModel.Table.Add(Enumerable.Range(1, rowNames.Count).Select(x => "0").ToList());
                }
            }
            else
            {
                // add Zones not in the table
                IEnumerable<string> except = colNames.Except(ForestryModel.Table[0]);
                foreach (string s in except)
                    ForestryModel.Table.Add(Enumerable.Range(1, ForestryModel.Table[1].Count).Select(x => "0").ToList());
                ForestryModel.Table[0].AddRange(except);
                for (int i = 2; i < ForestryModel.Table.Count; i++) //set Depth row to empty strings
                      ForestryModel.Table[i][2] = string.Empty;

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

                //check number of columns equals number of Zones, adjust if not. check column names match Zone names, remove columns that don't match a Zone name.
            }

            ForestryViewer.SetupGrid(ForestryModel.Table);
        }
    }
}