using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Models.Core;
using Models.PostSimulationTools;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// Test interface. TODO: Ensure it handles addition and removal
    /// of tests in Apsim.Shared.Utilities.MathUtilities.RegrStats
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(PostSimulationTools.PredictedObserved))]
    public class Tests : Model, ITestable
    {
        /// <summary>
        /// data table
        /// </summary>
        [Description("A table holding the results of the comparison")]
        public DataTable table { get; set; }

        /// <summary>
        /// Run tests
        /// </summary>
        public void Test()
        {
            PredictedObserved PO = Parent as PredictedObserved;
            DataStore DS = PO.Parent as DataStore;
            List<string> statNames = (new MathUtilities.RegrStats()).GetType().GetFields().Select(f => f.Name).ToList(); // use reflection, get names of stats available
            DataTable POtable = DS.GetData("*", PO.Name);
            List<string> columnNames; 
            MathUtilities.RegrStats[] stats;

            if (POtable == null)
                throw new ApsimXException(this, "Could not find PO table. Has the simulation been run?");
            columnNames= POtable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(); //get list of column names;
            columnNames = columnNames.Where(c => c.Contains("Observed")).ToList(); //filter names that are not pred/obs pairs
            stats = new MathUtilities.RegrStats[columnNames.Count];
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            string xstr, ystr;
            double xres;
            double yres;
            for (int c = 0; c < columnNames.Count; c++) //on each P/O column pair
            {
                foreach (DataRow row in POtable.Rows)
                {
                    xstr = row[columnNames[c]].ToString();
                    ystr = row[columnNames[c].Replace("Observed", "Predicted")].ToString(); ;
                    if (Double.TryParse(xstr, out xres) && Double.TryParse(ystr, out yres))
                    {
                        x.Add(xres);
                        y.Add(yres);
                    }
                }
                stats[c] = MathUtilities.CalcRegressionStats(x, y);
            }

            //turn stats array into a DataTable
            //create table if it doesn't exist
            if (table == null)
            {
                table = new DataTable("StatTests");
                table.Columns.Add("Variable", typeof(string));
                table.Columns.Add("Test", typeof(string));
                table.Columns.Add("Accepted", typeof(double));
                table.Columns.Add("Current", typeof(double));
                table.Columns.Add("Difference", typeof(double));
                table.Columns.Add("Sig.", typeof(char));

                for (int i = 0; i < columnNames.Count; i++)
                    for (int j = 0; j < statNames.Count; j++)
                        table.Rows.Add(columnNames[i].Replace("Observed.", ""),
                            statNames[j], stats[i].GetType().GetField(statNames[j]).GetValue(stats[i]), stats[i].GetType().GetField(statNames[j]).GetValue(stats[i]), 0, ' ');
            }
            else //update 'new' data and calculate differences
            {
                for (int i = 0; i < columnNames.Count; i++)
                    for (int j = 0; j < statNames.Count; j++)
                    {
                        table.Rows[i * statNames.Count + j]["Current"] = stats[i].GetType().GetField(statNames[j]).GetValue(stats[i]);
                        table.Rows[i * statNames.Count + j]["Difference"] = table.Rows[i * statNames.Count + j].Field<double>("Current") - table.Rows[i * statNames.Count + j].Field<double>("Accepted");
                        table.Rows[i * statNames.Count + j]["Sig."] = Math.Abs(table.Rows[i * statNames.Count + j].Field<double>("Difference")) > Math.Abs(table.Rows[i * statNames.Count + j].Field<double>("Accepted")) * 0.01 ? "X" : " ";
                    }
            }

            //TODO: check for sig diffs and handle as required
        }
    }
}