using APSIM.Core;
using DocumentFormat.OpenXml.Office2010.Excel;
using Models.Core;
using Models.Core.Run;
using Models.PreSimulationTools;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;

namespace UnitTests
{
    public class ObservationsTests
    {

        [Test]
        public void LoadExcelFile()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Simulations simulations = Utilities.GetRunnableSim();
            simulations.Node = Node.Create(simulations);

            DataStore dataStore = simulations.Node.FindChild<DataStore>();

            Observations observations = new Observations()
            {
                FileNames = ["%root%/Tests/UnitTests/PreSimulationTools/Input.xlsx"],
                SheetNames = ["Sheet1"]
            };
            dataStore.Node.AddChild(observations);

            simulations.Node = Node.Create(simulations);

            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new AggregateException("Errors: ", errors);

            DataTable dt = dataStore.Reader.GetData("Sheet1");

            Assert.That(dt.Columns[4].DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(dt.Columns[5].DataType, Is.EqualTo(typeof(string)));
            Assert.That(dt.Columns[6].DataType, Is.EqualTo(typeof(string)));
        }
    }
}
