using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PostSimulationTools;
using Models.Storage;
using Newtonsoft.Json;

namespace Models
{
    /// <summary>
    /// Test interface.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    [ValidParent(ParentType = typeof(PredictedObserved))]
    public class Tests : Model, ITestable
    {
        /// <summary>
        /// data table
        /// </summary>
        [JsonIgnore]
        public DataTable Table { get; set; }

        /// <summary>
        /// A collection of validated stats.
        /// </summary>
        public MathUtilities.RegrStats[] AcceptedStats { get; set; }

        /// <summary>
        /// A string containing the names of stats in the accepted values.
        /// Used for checking if the stats class has changed.
        /// </summary>
        public string AcceptedStatsName { get; set; }

        /// <summary>
        /// The name of the associated Predicted Observed node.
        /// </summary>
        public string POName { get; set; }

        /// <summary>The name of the predicted observed table.</summary>
        [Display]
        public string[] PONames 
        { 
            get 
            { 
                Test(accept: false, GUIrun: true); 
                return Enumerable.Repeat(POName, Variables.Count).ToArray();
            }
        }

        /// <summary>The name of the model variable.</summary>
        [Display]
        public List<string> Variables { get; private set; } = new List<string>();

        /// <summary>The name of the stat.</summary>
        [Display]
        public List<string> Stats { get; private set; } = new List<string>();

        /// <summary>The accepted values.</summary>
        [Display(Format = "F3")]
        public List<double> Accepteds { get; private set; } = new List<double>();

        /// <summary>The current values.</summary>
        [Display(Format = "F3")]
        public List<double> Currents { get; private set; } = new List<double>();

        /// <summary>The differences.</summary>
        [Display(Format = "F3")]
        public List<double> Differences { get; private set; } = new List<double>();

        /// <summary>The pass/fail.</summary>
        [Display]
        public List<string> Fails { get; private set; } = new List<string>();

        /// <summary>
        /// Run tests
        /// </summary>
        /// <param name="accept">If true, the stats from this run will be written to file as the accepted stats.</param>
        /// <param name="GUIrun">If true, do not raise an exception on test failure.</param>
        public void Test(bool accept = false, bool GUIrun = false)
        {
            PredictedObserved PO = Parent as PredictedObserved;
            if (PO == null)
                return;
            IDataStore DS = PO.Parent as IDataStore;
            MathUtilities.RegrStats[] stats;
            List<string> statNames = (new MathUtilities.RegrStats()).GetType().GetFields().Select(f => f.Name).ToList(); // use reflection, get names of stats available
            DataTable POtable = DS.Reader.GetData(PO.Name);
            List<string> columnNames;
            string sigIdent = "X";

            if (POtable == null)
            {
                object sim = PO.Parent;
                while (sim as Simulations == null)
                    sim = ((Model)sim).Parent;

                throw new ApsimXException(this, "Could not find PO table in " + (sim != null ? ((Simulations)sim).FileName : "<unknown>") + ". Has the simulation been run?");
            }
            columnNames = POtable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(); //get list of column names
            columnNames = columnNames.Where(c => c.Contains("Observed")).ToList(); //filter names that are not pred/obs pairs
            for (int i = 0; i < columnNames.Count; i++)
                columnNames[i] = columnNames[i].Replace("Observed.", "");
            columnNames.Sort(); //ensure column names are always in the same order
            columnNames.Remove("CheckpointID");
            stats = new MathUtilities.RegrStats[columnNames.Count];
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            string xstr, ystr;
            double xres;
            double yres;

            for (int c = 0; c < columnNames.Count; c++) //on each P/O column pair
            {
                string observedFieldName = "Observed." + columnNames[c];
                string predictedFieldName = "Predicted." + columnNames[c];
                if (POtable.Columns.Contains(observedFieldName) &&
                    POtable.Columns.Contains(predictedFieldName))
                {
                    x.Clear();
                    y.Clear();
                    foreach (DataRow row in POtable.Rows)
                    {
                        xstr = row[observedFieldName].ToString();
                        ystr = row[predictedFieldName].ToString();
                        if (Double.TryParse(xstr, out xres) && Double.TryParse(ystr, out yres))
                        {
                            x.Add(xres);
                            y.Add(yres);
                        }
                    }
                    if (x.Count == 0 || y.Count == 0)
                        continue;

                    stats[c] = MathUtilities.CalcRegressionStats(columnNames[c], y, x);
                }
            }

            //remove any null stats which can occur from non-numeric columns such as dates
            List<MathUtilities.RegrStats> list = new List<MathUtilities.RegrStats>(stats);
            list.RemoveAll(l => l == null);
            stats = list.ToArray();

            //remove entries from column names
            for (int i = columnNames.Count() - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < stats.Count(); j++)
                {
                    if (columnNames[i] == stats[j].Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    columnNames.RemoveAt(i);
            }

            //turn stats array into a DataTable
            //first, check if there is already an AcceptedStats array, create if not.
            //If the names don't match, then use current stats as user has dragged
            //an already existing Test to a new node.
            if (AcceptedStats == null || POName != PO.Name)
            {
                POName = PO.Name;
                AcceptedStats = stats;
                AcceptedStatsName = StringUtilities.Build(statNames, " ");
            }

            //then make sure the names and order of the accepted stats are the same as the new ones.
            if (StringUtilities.Build(statNames, " ") != AcceptedStatsName)
                throw new ApsimXException(this, "Names, number or order of accepted stats do not match class MathUtilities.RegrStats. The class has probably changed.");

            double accepted;
            double current;

            Variables.Clear();
            Stats.Clear();
            Accepteds.Clear();
            Currents.Clear();
            Differences.Clear();
            Fails.Clear();

            //accepted table
            for (int i = 0; i < AcceptedStats.Count(); i++)
                for (int j = 1; j < statNames.Count; j++) //start at 1; we don't want Name field.
                {
                    accepted = Convert.ToDouble(AcceptedStats[i].GetType().GetField(statNames[j]).GetValue(AcceptedStats[i]),
                                                System.Globalization.CultureInfo.InvariantCulture);
                    current = Convert.ToDouble(stats[i].GetType().GetField(statNames[j]).GetValue(stats[i]),
                                               System.Globalization.CultureInfo.InvariantCulture);

                    double difference = current - accepted;
                    string fail = Math.Abs(difference) > Math.Abs(accepted) * 0.01 ? sigIdent : " ";

                    Variables.Add(stats[i].Name);
                    Stats.Add(statNames[j]);
                    Accepteds.Add(accepted);
                    Currents.Add(current);
                    Differences.Add(difference);
                    Fails.Add(fail);
                }

            if (accept)
                AcceptedStats = stats;
        }
    }
}