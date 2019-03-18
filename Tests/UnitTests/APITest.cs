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
    using Models.Core.ApsimFile;
    using System.Linq;

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

            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.APITest.xml");
            List<Exception> creationExceptions;
            simulations = FileFormat.ReadFromString<Simulations>(xml, out creationExceptions);

            //string sqliteSourceFileName = TestDataStore.FindSqlite3DLL();

            //string sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
            //if (!File.Exists(sqliteFileName))
            //{
            //    File.Copy(sqliteSourceFileName, sqliteFileName);
            //}

            this.simulation = this.simulations.Children[0] as Simulation;
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
            Assert.AreEqual(zone.Children.Count, 1);
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
        /// A test for the Apsim.Ancestor method.
        /// </summary>
        [Test]
        public void AncestorTest()
        {
            // Passing in null should return null.
            Assert.Null(Apsim.Ancestor<IModel>(null));

            // Passing in the top-level simulations object should return null.
            Assert.Null(Apsim.Ancestor<IModel>(simulations));

            // Passing in an object should never return that object
            Assert.AreNotEqual(simulation, Apsim.Ancestor<Simulation>(simulation));

            // Searching for any IModel ancestor should return the node's parent.
            Assert.AreEqual(simulation.Parent, Apsim.Ancestor<IModel>(simulation));
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

            double[] thicknessBefore = (double[])Apsim.Get(simulation, "[Water].Thickness");
            Assert.AreEqual(6, thicknessBefore.Length); // If APITest.xml is modified, this test will fail and must be updated.
            Apsim.Set(simulation, "[Water].Thickness[1]", "20");
            double[] thicknessAfter = (double[])Apsim.Get(simulation, "[Water].Thickness");

            Assert.AreEqual(thicknessBefore.Length, thicknessAfter.Length);
            Assert.AreEqual(20, thicknessAfter[0]);
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
