using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Data;
using System.Text;
using Models.Core;
using Models.PostSimulationTools;
using APSIM.Shared.Utilities;
using System.ComponentModel;
using Models.Storage;
using Models.Interfaces;

namespace Models
{
    /// <summary>
    /// Test interface.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.TablePresenter")]
    [ValidParent(ParentType = typeof(PostSimulationTools.PredictedObserved))]
    public class Tests : Model, ITestable, IModelAsTable, ICustomDocumentation
    {
        /// <summary>
        /// data table
        /// </summary>
        [XmlIgnore]
        public DataTable Table { get; set; }

        /// <summary>
        /// A collection of validated stats.
        /// </summary>
        [Models.Core.Description("An array of validated regression stats.")]
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

        /// <summary>
        /// Implementation of IModelAsTable - required for UI to work properly.
        /// </summary>
        public List<DataTable> Tables { get { return new List<DataTable>() { Table }; } }

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

            Table = new DataTable("StatTests");
            Table.Columns.Add("Name", typeof(string));
            Table.Columns.Add("Variable", typeof(string));
            Table.Columns.Add("Test", typeof(string));
            Table.Columns.Add("Accepted", typeof(double));
            Table.Columns.Add("Current", typeof(double));
            Table.Columns.Add("Difference", typeof(double));
            Table.Columns.Add("Fail?", typeof(string));

            double accepted;
            double current;
            DataTable AcceptedTable = Table.Copy();
            DataTable CurrentTable = Table.Copy();

            //accepted table
            for (int i = 0; i < AcceptedStats.Count(); i++)
                for (int j = 1; j < statNames.Count; j++) //start at 1; we don't want Name field.
                {
                    accepted = Convert.ToDouble(AcceptedStats[i].GetType().GetField(statNames[j]).GetValue(AcceptedStats[i]), 
                                                System.Globalization.CultureInfo.InvariantCulture);
                    AcceptedTable.Rows.Add(PO.Name,
                                    AcceptedStats[i].Name,
                                    statNames[j],
                                    accepted,
                                    null,
                                    null,
                                    null);
                }

            //current table
            Table = AcceptedTable.Copy();
            int rowIndex = 0;
            for (int i = 0; i < stats.Count(); i++)
                for (int j = 1; j < statNames.Count; j++) //start at 1; we don't want Name field.
                {
                    current = Convert.ToDouble(stats[i].GetType().GetField(statNames[j]).GetValue(stats[i]), 
                                               System.Globalization.CultureInfo.InvariantCulture);
                    CurrentTable.Rows.Add(PO.Name,
                                    stats[i].Name,
                                    statNames[j],
                                    null,
                                    current,
                                    null,
                                    null);
                    Table.Rows[rowIndex]["Current"] = current;
                    rowIndex++;
                }

            //Merge overwrites rows, so add the correct data back in
            foreach(DataRow row in Table.Rows)
            {
                DataRow[] rowAccepted = AcceptedTable.Select("Name = '" + row["Name"] + "' AND Variable = '" + row["Variable"] + "' AND Test = '" + row["Test"] + "'");
                DataRow[] rowCurrent  = CurrentTable.Select ("Name = '" + row["Name"] + "' AND Variable = '" + row["Variable"] + "' AND Test = '" + row["Test"] + "'");

                if (rowAccepted.Count() == 0)
                    row["Accepted"] = DBNull.Value;
                else
                    row["Accepted"] = rowAccepted[0]["Accepted"];

                if (rowCurrent.Count() == 0)
                    row["Current"] = DBNull.Value;
                else
                    row["Current"] = rowCurrent[0]["Current"];

                if (row["Accepted"] != DBNull.Value && row["Current"] != DBNull.Value)
                {
                    row["Difference"] = Convert.ToDouble(row["Current"], 
                                                         System.Globalization.CultureInfo.InvariantCulture) - 
                                               Convert.ToDouble(row["Accepted"], System.Globalization.CultureInfo.InvariantCulture);
                    row["Fail?"] = Math.Abs(Convert.ToDouble(row["Difference"], System.Globalization.CultureInfo.InvariantCulture)) 
                                       > Math.Abs(Convert.ToDouble(row["Accepted"], System.Globalization.CultureInfo.InvariantCulture)) * 0.01 ? sigIdent : " ";
                }
                else
                {
                    row["Difference"] = DBNull.Value;
                    row["Fail?"] = sigIdent;
                }
            }
            //Tables could be large so free the memory.
            AcceptedTable = null;
            CurrentTable = null;

            if (accept)
                AcceptedStats = stats;
            else
            {
                foreach (DataRow row in Table.Rows)
                    if (row["Fail?"].ToString().Equals(sigIdent))
                    {
                        if (!GUIrun)
                        {
                            object sim = PO.Parent;
                            while (sim as Simulations == null)
                                sim = ((Model)sim).Parent;

                            throw new ApsimXException(this, "Significant differences found during regression testing of " + PO.Name + " in " + (sim != null ? ((Simulations)sim).FileName : "<unknown>"));
                        }
                    }
            }
        }

        /// <summary>Document the stats.</summary>
        /// <param name="tags"></param>
        /// <param name="headingLevel"></param>
        /// <param name="indent"></param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                if (Parent != null)
                    tags.Add(new AutoDocumentation.Heading(Parent.Name, headingLevel));

                // Run test suite so that data table is full.
                Test(accept: false, GUIrun: true);

                // Get stat names.
                List<string> statNames = (new MathUtilities.RegrStats()).GetType().GetFields().Select(f => f.Name).ToList(); // use reflection, get names of stats available
                statNames.RemoveAt(0);

                // Grab the columns of data we want.
                DataTable dataForDoc = new DataTable();
                dataForDoc.Columns.Add("Variable", typeof(string));
                for (int statIndex = 0; statIndex < statNames.Count; statIndex++)
                {
                    if (statNames[statIndex] != "SEintercept" &&
                        statNames[statIndex] != "SEslope" &&
                        statNames[statIndex] != "RSR")
                        dataForDoc.Columns.Add(statNames[statIndex], typeof(string));
                }

                int rowIndex = 0;
                while (Table != null && rowIndex < Table.Rows.Count)
                {
                    DataRow row = dataForDoc.NewRow();
                    dataForDoc.Rows.Add(row);
                    string variableName = Table.Rows[rowIndex][1].ToString();
                    row[0] = variableName;

                    int i = 1;
                    for (int statIndex = 0; statIndex < statNames.Count; statIndex++)
                    {
                        if (statNames[statIndex] != "SEintercept" &&
                            statNames[statIndex] != "SEslope" &&
                            statNames[statIndex] != "RSR")
                        {
                            object currentValue = Table.Rows[rowIndex]["Current"];
                            string formattedValue;
                            if (currentValue.GetType() == typeof(double))
                            {
                               double doubleValue = Convert.ToDouble(currentValue, 
                                                                     System.Globalization.CultureInfo.InvariantCulture);
                                if (!double.IsNaN(doubleValue))
                                {
                                    if (statIndex == 0)
                                        formattedValue = doubleValue.ToString("F0");
                                    else
                                        formattedValue = doubleValue.ToString("F3");
                                }
                                else
                                    formattedValue = currentValue.ToString();
                            }
                            else
                                formattedValue = currentValue.ToString();

                            row[i] = formattedValue;
                            i++;
                        }
                        rowIndex++;
                    }
                }

                // add data to doc table.
                tags.Add(new AutoDocumentation.Table(dataForDoc, headingLevel));
            }
        }

    }
}