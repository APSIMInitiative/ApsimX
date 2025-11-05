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

            Assert.That(dt.Columns[4].DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(dt.Columns[5].DataType, Is.EqualTo(typeof(string)));
            Assert.That(dt.Columns[6].DataType, Is.EqualTo(typeof(string)));

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
            string[] mergeDate = ["01/01/2000"];
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
            string[] zeroDate = ["01/01/2000"];

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
                SheetNames = ["Sheet1"]
            };

            dataStore.Node.AddChild(observations);
            simulations.Node = Node.Create(simulations);

            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new AggregateException("Errors: ", errors);

        }

        [Test]
        public void ObservationsDerivedStats()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("SimulationName");
            dataTable.Columns.Add("Plant.Leaf.NConc");
            dataTable.Columns.Add("Plant.Leaf.NC");
            dataTable.Columns.Add("Plant.Leaf.Wt");

            dataTable.Columns.Add("Plant.Leaf.Dead.Wt");
            dataTable.Columns.Add("Plant.Leaf.Live.Wt");

            dataTable.Columns.Add("Plant.Leaf.SpecificAreaCanopy");
            dataTable.Columns.Add("Plant.Leaf.LAI");

            DataRow row;

            row = dataTable.NewRow();
            row["SimulationName"] = "Test1";
            row["Plant.Leaf.NConc"] = 5;
            row["Plant.Leaf.NC"] = "Test1";
            row["Plant.Leaf.Wt"] = "Test1";
            row["Plant.Leaf.Dead.Wt"] = "Test1";
            row["Plant.Leaf.Live.Wt"] = "Test1";
            row["Plant.Leaf.SpecificAreaCanopy"] = "Test1";
            row["Plant.Leaf.LAI"] = "Test1";
            dataTable.Rows.Add(row);

            List<DerivedInfo> infos = DerivedInfo.AddDerivedColumns(dataTable);

/*
            infos.AddRange(DeriveColumn(dataTable, ".NConc", ".N", "/", ".Wt"));
            infos.AddRange(DeriveColumn(dataTable, ".N", ".NConc", "*", ".Wt"));
            infos.AddRange(DeriveColumn(dataTable, ".Wt", ".N", "/", ".NConc"));

            infos.AddRange(DeriveColumn(dataTable, ".", ".Live.", "+", ".Dead."));
            infos.AddRange(DeriveColumn(dataTable, ".Live.", ".", "-", ".Dead."));
            infos.AddRange(DeriveColumn(dataTable, ".Dead.", ".", "-", ".Live."));

            infos.AddRange(DeriveColumn(dataTable, "Leaf.SpecificAreaCanopy", "Leaf.LAI", "/", "Leaf.Live.Wt"));
            infos.AddRange(DeriveColumn(dataTable, "Leaf.LAI", "Leaf.SpecificAreaCanopy", "*", "Leaf.Live.Wt"));
            infos.AddRange(DeriveColumn(dataTable, "Leaf.Live.Wt", "Leaf.LAI", "/", "Leaf.SpecificAreaCanopy"));
*/
            string[] derivedName = ["Plant.Leaf.NConc", "Plant.Leaf.NC", "Plant.Leaf.Wt", "Plant.Leaf.Dead.Wt", "Plant.Leaf.Live.Wt", "Plant.Leaf.SpecificAreaCanopy", "Plant.Leaf.LAI"];
            string[] derivedFunction = ["Wheat.Grain.N / Wheat.Grain.Wt"];
            string[] derivedVariable = ["double"];
            int[] derivedAdded = [3];
            int[] derivedExisting = [0];

            for (int i = 0; i < infos.Count; i++)
            {
                DerivedInfo info = observations.DerivedData[i];
                Assert.That(info.Name, Is.EqualTo(derivedName[i]));
                Assert.That(info.Function, Is.EqualTo(derivedFunction[i]));
                Assert.That(info.DataType, Is.EqualTo(derivedVariable[i]));
                Assert.That(info.Added, Is.EqualTo(derivedAdded[i]));
                Assert.That(info.Existing, Is.EqualTo(derivedExisting[i]));
            }

        }
    }
}
