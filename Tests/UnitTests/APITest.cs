// -----------------------------------------------------------------------
// <copyright file="APITest.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Importer;
    using Models;
    using Models.Core;
    using Models.Graph;
    using Models.Soils;
    using NUnit.Framework;
    using APSIM.Shared.Utilities;
    using UserInterface.Interfaces;
    using UserInterface.Views;
    using UserInterface.Commands;
    using UserInterface;
    using UserInterface.Presenters;
    using System.Reflection;
    /// <summary> 
    /// This is a test class for SystemComponentTest and is intended
    /// to contain all SystemComponentTest Unit Tests
    /// </summary>
    [TestFixture]
    public class APITest
    {
        /// <summary>
        /// A simulations instance
        /// </summary>
        private Simulations simulations;
        
        /// <summary>
        /// A simulation instance
        /// </summary>
        private Simulation simulation;

        /// <summary>
        /// Start up code for all tests.
        /// </summary>
        [SetUp]
        public void Initialise()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "UnitTests");
            Directory.CreateDirectory(tempFolder);
            Directory.SetCurrentDirectory(tempFolder);
            FileStream oldfile = new FileStream("Continuous_Wheat.apsim", FileMode.Create);
            oldfile.Write(UnitTests.Properties.Resources.Continuous_Wheat, 0, UnitTests.Properties.Resources.Continuous_Wheat.Length);
            oldfile.Close();
            
            FileStream f = new FileStream("Test.apsimx", FileMode.Create);
            f.Write(UnitTests.Properties.Resources.TestFile, 0, UnitTests.Properties.Resources.TestFile.Length);
            f.Close();
            FileStream w = new FileStream("Goondiwindi.met", FileMode.Create);
            w.Write(UnitTests.Properties.Resources.Goondiwindi, 0, UnitTests.Properties.Resources.Goondiwindi.Length);
            w.Close();
            this.simulations = Simulations.Read("Test.apsimx");
            
            string sqliteSourceFileName = TestDataStore.FindSqlite3DLL();

            string sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
            if (!File.Exists(sqliteFileName))
            {
                File.Copy(sqliteSourceFileName, sqliteFileName);
            }

            this.simulation = this.simulations.Children[0] as Simulation;
        }

        /// <summary>
        /// Clean up code for all tests.
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            //this.simulation.CleanupRun();
            //File.Delete("Test.apsimx");
            //File.Delete("Goondiwindi.met");
        }

        /// <summary>
        /// A test for the FullPath method
        /// </summary>
        [Test]
        public void FullPathTest()
        {
            Zone zone2 = this.simulation.Children[4] as Zone;
            Graph graph = zone2.Children[0] as Graph;
            Soil soil = zone2.Children[1] as Soil;
            
            Assert.AreEqual(Apsim.FullPath(this.simulation), ".Simulations.Test");
            Assert.AreEqual(Apsim.FullPath(zone2), ".Simulations.Test.Field2");
            Assert.AreEqual(Apsim.FullPath(soil), ".Simulations.Test.Field2.Soil");
        }

        /// <summary>
        /// A test to ensure that 'Models' property is valid.
        /// </summary>
        [Test]
        public void ModelsTest()
        {
            Assert.AreEqual(this.simulations.Children.Count, 4);
            Assert.AreEqual(this.simulations.Children[0].Name, "Test");

            Assert.AreEqual(this.simulation.Children.Count, 5);
            Assert.AreEqual(this.simulation.Children[0].Name, "Weather");
            Assert.AreEqual(this.simulation.Children[1].Name, "Clock");
            Assert.AreEqual(this.simulation.Children[2].Name, "Summary");
            Assert.AreEqual(this.simulation.Children[3].Name, "Field1");
            Assert.AreEqual(this.simulation.Children[4].Name, "Field2");

            Zone zone = this.simulation.Children[3] as Zone;
            Assert.AreEqual(zone.Children.Count, 2);
            Assert.AreEqual(zone.Children[0].Name, "Field1Report");
        }
        
        /// <summary>
        /// A test to ensure that Parent of type method works.
        /// </summary>
        [Test]
        public void ParentTest()
        {
            Zone zone2 = this.simulation.Children[4] as Zone;
            Graph graph = zone2.Children[0] as Graph;
            
            Assert.NotNull(Apsim.Parent(this.simulation, typeof(Simulations)));
            Assert.AreEqual(Apsim.Parent(graph, typeof(Simulations)).Name, "Simulations");
            Assert.AreEqual(Apsim.Parent(graph, typeof(Simulation)).Name, "Test");
            Assert.AreEqual(Apsim.Parent(graph, typeof(Zone)).Name, "Field2");
        }



        /// <summary>
        /// Tests for the get method
        /// </summary>
        [Test]
        public void GetTest()
        {
            Zone zone2 = this.simulation.Children[4] as Zone;
            Graph graph = zone2.Children[0] as Graph;
            Soil soil = zone2.Children[1] as Soil;

            Zone field1 = this.simulation.Children[3] as Zone;
            
            // Make sure we can get a link to a local model from Field1
            Assert.AreEqual((simulation.Get("Field1.Field1Report") as IModel).Name, "Field1Report");
            
            // Make sure we can get a variable from a local model.
            Assert.AreEqual(simulation.Get("Field1.Field1Report.Name"), "Field1Report");

            // Make sure we can get a variable from a local model using a full path.
            Assert.AreEqual((simulation.Get(".Simulations.Test.Field1.Field1Report") as IModel).Name, "Field1Report");
            Assert.AreEqual(simulation.Get(".Simulations.Test.Field1.Field1Report.Name"), "Field1Report");

            // Make sure we get a null when trying to link to a top level model from Field1
            Assert.IsNull(simulation.Get("Field1.Weather"));

            // Make sure we can get a top level model from Field1 using a full path.
            Assert.AreEqual(ReflectionUtilities.Name(simulation.Get(".Simulations.Test.Weather")), "Weather");

            // Make sure we can get a model in Field2 from Field1 using a full path.
            Assert.AreEqual(ReflectionUtilities.Name(simulation.Get(".Simulations.Test.Field2.Graph1")), "Graph1");

            // Make sure we can get a property from a model in Field2 from Field1 using a full path.
            Assert.AreEqual(simulation.Get(".Simulations.Test.Field2.Graph1.Name"), "Graph1");

            // Make sure we can get a property from a model in Field2/Field2SubZone from Field1 using a full path.
            Assert.AreEqual(simulation.Get(".Simulations.Test.Field2.Field2SubZone.Field2SubZoneReport.Name"), "Field2SubZoneReport");
            
            // Test the in scope capability of get.
            Assert.AreEqual(simulation.Get("[Graph1].Name"), "Graph1");
            Assert.AreEqual(simulation.Get("[Soil].Water.Name"), "Water");
        }
        
        /// <summary>
        /// Tests for the set method
        /// </summary>
        [Test]
        public void SetTest()
        {
            Assert.AreEqual(this.simulation.Get("[Weather].Rain"), 0.0);
            this.simulation.Set("[Weather].Rain", 111.0);
            Assert.AreEqual(this.simulation.Get("[Weather].Rain"), 111.0);
        }

        /// <summary>
        /// Tests for Children
        /// </summary>
        [Test]
        public void ChildrenTest()
        {
            List<IModel> allChildren = Apsim.Children(this.simulation, typeof(Zone));
            Assert.AreEqual(allChildren.Count, 2);
        }
        
        /// <summary>
        /// Tests for Child method
        /// </summary>
        [Test]
        public void ChildTest()
        {
            IModel clock = Apsim.Child(simulation, typeof(Clock));
            Assert.NotNull(clock);
            clock = Apsim.Child(simulation, "Clock");
            Assert.NotNull(clock);
        }        

        /// <summary>
        /// Tests for siblings method
        /// </summary>
        [Test]
        public void SiblingsTest()
        {
            IModel clock = Apsim.Child(simulation, typeof(Clock));
            List<IModel> allSiblings = Apsim.Siblings(clock);
            Assert.AreEqual(allSiblings.Count, 4);
        }
  
        /// <summary>
        /// Tests for the importer
        /// </summary>
        [Test]
        public void ImportOldAPSIM()
        {
            // test the importing of an example simulation from APSIM 7.6
            APSIMImporter importer = new APSIMImporter();
            importer.ProcessFile("Continuous_Wheat.apsim");

            Simulations testrunSimulations = Simulations.Read("Continuous_Wheat.apsimx");
           
            Assert.IsNotNull(Apsim.Find(testrunSimulations, "wheat"));
            Assert.IsNotNull(Apsim.Find(testrunSimulations, "clock"));
            Assert.IsNotNull(Apsim.Find(testrunSimulations, "SoilNitrogen"));
            Assert.IsNotNull(Apsim.Find(testrunSimulations, "SoilWater"));
        }

        /// <summary>
        /// Tests the move up down command
        /// </summary>
        [Test]
        public void MoveUpDown()
        {
            CommandHistory commandHistory = new CommandHistory();
            Model modelToMove = Apsim.Get(simulations, "APS14.Factors.NRate") as Model;

            MoveModelUpDownCommand moveCommand = new MoveModelUpDownCommand(modelToMove, true, null);
            moveCommand.Do(commandHistory);

            Model modelToMove2 = Apsim.Get(simulations, "APS14.Factors.NRate") as Model;

            Assert.AreEqual(simulations.Children[2].Children[0].Children[0].Name, "NRate");
            Assert.AreEqual(simulations.Children[2].Children[0].Children[0].Children.Count, 4);
        }

       
    }
}
