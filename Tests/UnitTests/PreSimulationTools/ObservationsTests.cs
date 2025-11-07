using APSIM.Core;
using Models.Core;
using Models.Core.Run;
using Models.PreSimulationTools;
using Models.PreSimulationTools.ObservationsInfo;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace UnitTests
{
    public class ObservationsTests
    {

        [Test]
        public void LoadExcelFile()
        {
            //This test was ported from ExcelInput

            /*
            Edge cases that this tests for:
            - Rows with same simulation id and date that must be merged together
            - merging with conflicts
            - rows without a clock.today
            - date columns with different and mixed formats
            - lots of empty cells
            - merging without conflicts
            - merging when values are the same (no conflict)
            */

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Simulations simulations = Utilities.GetRunnableSim(useInMemoryDb: true);
            simulations.Node = Node.Create(simulations);
            simulations.Node.FindChild<Simulation>().Name = "Simulation1";

            DataStore dataStore = simulations.Node.FindChild<DataStore>();

            Observations observations = new Observations()
            {
                FileNames = ["%root%/Tests/UnitTests/PreSimulationTools/Input.xlsx"],
                SheetNames = ["Observed"]
            };

            dataStore.Node.AddChild(observations);
            simulations.Node = Node.Create(simulations);

            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new AggregateException("Errors: ", errors);

            dataStore.Writer.Stop();
            dataStore.Reader.Refresh();
            DataTable dt = dataStore.Reader.GetData("Observed");

            Assert.That(dt.Columns[5].DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(dt.Columns[6].DataType, Is.EqualTo(typeof(string)));
            Assert.That(dt.Columns[7].DataType, Is.EqualTo(typeof(string)));

            string filename = "%root%/Tests/UnitTests/PreSimulationTools/Input.xlsx";

            //Check column information
            //
            string[] columns = ["Clock.Today", "DayMonth", "String", "Wheat.Grain.Wt", "ValueWithSpace", "Wheat.Grain.N"];
            foreach (string name in observations.ColumnNames)
                Assert.That(columns.Contains(name), Is.True);

            string[] columnsAPSIM = ["Yes", "No", "No", "Not Found", "No", "Not Found"];
            Type[] columnsVariable = [typeof(DateTime), null, null, null, null, null];
            Type[] columnsTypes = [typeof(DateTime), typeof(string), typeof(string), typeof(int), typeof(int), typeof(int)];
            bool[] columnsError = [false, false, false, false, false, false];

            for (int i = 0; i < observations.ColumnData.Count; i++)
            {
                ColumnInfo info = observations.ColumnData[i];
                Assert.That(info.Name, Is.EqualTo(columns[i]));
                Assert.That(info.IsApsimVariable, Is.EqualTo(columnsAPSIM[i]));
                Assert.That(info.VariableType, Is.EqualTo(columnsVariable[i]));
                Assert.That(info.DataType, Is.EqualTo(columnsTypes[i]));
                Assert.That(info.HasErrorColumn, Is.EqualTo(columnsError[i]));
                Assert.That(info.File, Is.EqualTo(filename));
            }

            //Check Derived information
            //
            string[] derivedName = ["Wheat.Grain.NConc"];
            string[] derivedFunction = ["Wheat.Grain.N / Wheat.Grain.Wt"];
            string[] derivedVariable = ["double"];
            int[] derivedAdded = [3];
            int[] derivedExisting = [0];

            for (int i = 0; i < observations.DerivedData.Count; i++)
            {
                DerivedInfo info = observations.DerivedData[i];
                Assert.That(info.Name, Is.EqualTo(derivedName[i]));
                Assert.That(info.Function, Is.EqualTo(derivedFunction[i]));
                Assert.That(info.DataType, Is.EqualTo(derivedVariable[i]));
                Assert.That(info.Added, Is.EqualTo(derivedAdded[i]));
                Assert.That(info.Existing, Is.EqualTo(derivedExisting[i]));
            }
            
            //Check Merge information
            //
            string[] mergeName = [""];
            string[] mergeDate = ["2000-01-01"];
            string[] mergeColumn = ["ValueWithSpace"];
            string[] mergeValue1 = ["10"];
            string[] mergeValue2 = ["1000"];
            
            for (int i = 0; i < observations.MergeData.Count; i++)
            {
                MergeInfo info = observations.MergeData[i];
                Assert.That(info.Name, Is.EqualTo(mergeName[i]));
                Assert.That(info.Date, Is.EqualTo(mergeDate[i]));
                Assert.That(info.Column, Is.EqualTo(mergeColumn[i]));
                Assert.That(info.Value1, Is.EqualTo(mergeValue1[i]));
                Assert.That(info.Value2, Is.EqualTo(mergeValue2[i]));
                Assert.That(info.File, Is.EqualTo(filename));
            }

            //Check Zeros information
            //
            string[] zeroName = [""];
            string[] zeroColumn = ["Wheat.Grain.Wt"];
            string[] zeroDate = ["2000-01-01"];

            for (int i = 0; i < observations.ZeroData.Count; i++)
            {
                ZeroInfo info = observations.ZeroData[i];
                Assert.That(info.Name, Is.EqualTo(zeroName[i]));
                Assert.That(info.Column, Is.EqualTo(zeroColumn[i]));
                Assert.That(info.Date, Is.EqualTo(zeroDate[i]));
                Assert.That(info.File, Is.EqualTo(filename));
            }
        }

        [Test]
        public void ObservationsWorksWithBlankLine()
        {
            //This test was ported from ExcelInput
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Simulations simulations = Utilities.GetRunnableSim(useInMemoryDb: true);
            simulations.Node = Node.Create(simulations);

            DataStore dataStore = simulations.Node.FindChild<DataStore>();

            Observations observations = new Observations()
            {
                FileNames = ["%root%/Tests/UnitTests/PreSimulationTools/Input.xlsx", ""],
                SheetNames = ["Observed"]
            };

            dataStore.Node.AddChild(observations);
            simulations.Node = Node.Create(simulations);

            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new AggregateException("Errors: ", errors);

        }

        /// <summary>
        /// This test involves deriving values that haven't been provided, including doing 2nd order deriving where one derived values allows the calculation of a 2nd value.
        /// Every combination of derived value is checked here to make sure they produce the correct values.
        /// This test should be expanded as more derived values are created.
        /// </summary>
        [Test]
        public void ObservationsDerivedStats()
        {
            string[] derivedColumns = ["Plant.Leaf.Live.NConc", "Plant.Leaf.Live.N", "Plant.Leaf.Wt", "Plant.Leaf.Dead.Wt", "Plant.Leaf.Live.Wt", "Plant.Leaf.SpecificAreaCanopy", "Plant.Leaf.LAI"];

            using (DataTable dataTable = new DataTable())
            {
                dataTable.Columns.Add("SimulationName");
                foreach (string name in derivedColumns)
                    dataTable.Columns.Add(name);

                DataRow row1 = dataTable.NewRow();
                row1["SimulationName"] = "Simulation";
                row1["Plant.Leaf.Live.NConc"] = 0.032391931056;
                row1["Plant.Leaf.Live.N"] = 5.62;
                row1["Plant.Leaf.Live.Wt"] = 173.4999988202;
                dataTable.Rows.Add(row1);

                DataRow row2 = dataTable.NewRow();
                row2["SimulationName"] = "Simulation";
                row2["Plant.Leaf.Wt"] = 187.3;
                row2["Plant.Leaf.Dead.Wt"] = 13.800001179800006;
                row2["Plant.Leaf.Live.Wt"] = 173.4999988202;
                dataTable.Rows.Add(row2);

                DataRow row3 = dataTable.NewRow();
                row3["SimulationName"] = "Simulation";
                row3["Plant.Leaf.Live.Wt"] = 173.4999988202;
                row3["Plant.Leaf.SpecificAreaCanopy"] = 0.028818444;
                row3["Plant.Leaf.LAI"] = 5;
                dataTable.Rows.Add(row3);

                DataRow rowAll = dataTable.NewRow();
                rowAll["SimulationName"] = "Simulation";
                rowAll["Plant.Leaf.Live.NConc"] = 0.032391931056;
                rowAll["Plant.Leaf.Live.N"] = 5.62;
                rowAll["Plant.Leaf.Live.Wt"] = 173.4999988202;
                rowAll["Plant.Leaf.Wt"] = 187.3;
                rowAll["Plant.Leaf.Dead.Wt"] = 13.800001179800006;
                rowAll["Plant.Leaf.SpecificAreaCanopy"] = 0.028818444;
                rowAll["Plant.Leaf.LAI"] = 5;
                dataTable.Rows.Add(rowAll);

                foreach (string name in derivedColumns)
                {
                    DataTable tempDataTable = dataTable.Copy();
                    List<double> values = new List<double>();
                    foreach (DataRow row in tempDataTable.Rows)
                    {
                        string stringValue = row[name].ToString();
                        //store value
                        double valuebefore = double.NaN;
                        if (!string.IsNullOrEmpty(stringValue))
                            valuebefore = double.Parse(stringValue);
                        values.Add(valuebefore);
                        //null result
                        row[name] = null;
                    }

                    DerivedInfo.AddDerivedColumns(tempDataTable);

                    for (int i = 0; i < tempDataTable.Rows.Count; i++)
                    {
                        DataRow row = tempDataTable.Rows[i];
                        double valuebefore = values[i];
                        double valueAfter = double.NaN;
                        string stringValue = row[name].ToString();
                        if (!string.IsNullOrEmpty(stringValue))
                            valueAfter = double.Parse(stringValue);

                        Assert.That(valueAfter, Is.EqualTo(valuebefore).Within(0.0001));
                    }
                }
            }
        }
    }
}
