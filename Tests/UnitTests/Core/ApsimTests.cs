namespace UnitTests.Core
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Soils;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UserInterface;
    using UserInterface.Commands;
    using System.Linq;

    /// <summary> 
    /// This is a test class for SystemComponentTest and is intended
    /// to contain all SystemComponentTest Unit Tests
    /// </summary>
    [TestFixture]
    public class ApsimTests
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

            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimTests.xml");
            simulations = FileFormat.ReadFromString<Simulations>(xml, e => throw e, false).NewModel as Simulations;
            this.simulation = this.simulations.Children[0] as Simulation;
        }

        /// <summary>
        /// A test for the FullPath method
        /// </summary>
        [Test]
        public void FullPathTest()
        {
            Zone zone2 = this.simulation.Children[5] as Zone;
            Graph graph = zone2.Children[0] as Graph;
            Soil soil = zone2.Children[1] as Soil;
            
            Assert.That(this.simulation.FullPath, Is.EqualTo(".Simulations.Test"));
            Assert.That(zone2.FullPath, Is.EqualTo(".Simulations.Test.Field2"));
            Assert.That(soil.FullPath, Is.EqualTo(".Simulations.Test.Field2.Soil"));
        }

        /// <summary>
        /// A test to ensure that 'Models' property is valid.
        /// </summary>
        [Test]
        public void ModelsTest()
        {
            Assert.That(this.simulations.Children.Count, Is.EqualTo(4));
            Assert.That(this.simulations.Children[0].Name, Is.EqualTo("Test"));

            Assert.That(this.simulation.Children.Count, Is.EqualTo(6));
            Assert.That(this.simulation.Children[0].Name, Is.EqualTo("Weather"));
            Assert.That(this.simulation.Children[1].Name, Is.EqualTo("MicroClimate"));
            Assert.That(this.simulation.Children[2].Name, Is.EqualTo("Clock"));
            Assert.That(this.simulation.Children[3].Name, Is.EqualTo("Summary"));
            Assert.That(this.simulation.Children[4].Name, Is.EqualTo("Field1"));
            Assert.That(this.simulation.Children[5].Name, Is.EqualTo("Field2"));

            Zone zone = this.simulation.Children[4] as Zone;
            Assert.That(zone.Children.Count, Is.EqualTo(1));
            Assert.That(zone.Children[0].Name, Is.EqualTo("Field1Report"));
        }
        
        /// <summary>
        /// A test to ensure that Parent of type method works.
        /// </summary>
        [Test]
        public void ParentTest()
        {
            Zone zone2 = this.simulation.Children[5] as Zone;
            Graph graph = zone2.Children[0] as Graph;
            
            Assert.That(simulation.FindAncestor<Simulations>(), Is.Not.Null);
            Assert.That(graph.FindAncestor<Simulations>().Name, Is.EqualTo("Simulations"));
            Assert.That(graph.FindAncestor<Simulation>().Name, Is.EqualTo("Test"));
            Assert.That(graph.FindAncestor<Zone>().Name, Is.EqualTo("Field2"));
        }

        /// <summary>
        /// A test for the Apsim.Ancestor method.
        /// </summary>
        [Test]
        public void AncestorTest()
        {
            // Passing in the top-level simulations object should return null.
            Assert.That(simulations.FindAncestor<IModel>(), Is.Null);

            // Passing in an object should never return that object
            Assert.That(simulation, Is.Not.EqualTo(simulation.FindAncestor<Simulation>()));

            // Searching for any IModel ancestor should return the node's parent.
            Assert.That(simulation.Parent, Is.EqualTo(simulation.FindAncestor<IModel>()));
        }

        /// <summary>
        /// Tests for the get method
        /// </summary>
        [Test]
        public void GetTest()
        {
            Zone zone2 = this.simulation.Children[5] as Zone;
            Graph graph = zone2.Children[0] as Graph;
            Soil soil = zone2.Children[1] as Soil;

            Zone field1 = this.simulation.Children[4] as Zone;
            
            // Make sure we can get a link to a local model from Field1
            Assert.That((simulation.Get("Field1.Field1Report") as IModel).Name, Is.EqualTo("Field1Report"));
            
            // Make sure we can get a variable from a local model.
            Assert.That(simulation.Get("Field1.Field1Report.Name"), Is.EqualTo("Field1Report"));

            // Make sure we can get a variable from a local model using a full path.
            Assert.That((simulation.Get(".Simulations.Test.Field1.Field1Report") as IModel).Name, Is.EqualTo("Field1Report"));
            Assert.That(simulation.Get(".Simulations.Test.Field1.Field1Report.Name"), Is.EqualTo("Field1Report"));

            // Make sure we get a null when trying to link to a top level model from Field1
            Assert.That(simulation.Get("Field1.Weather"), Is.Null);

            // Make sure we can get a top level model from Field1 using a full path.
            Assert.That(ReflectionUtilities.Name(simulation.Get(".Simulations.Test.Weather")), Is.EqualTo("Weather"));

            // Make sure we can get a model in Field2 from Field1 using a full path.
            Assert.That(ReflectionUtilities.Name(simulation.Get(".Simulations.Test.Field2.Graph1")), Is.EqualTo("Graph1"));

            // Make sure we can get a property from a model in Field2 from Field1 using a full path.
            Assert.That(simulation.Get(".Simulations.Test.Field2.Graph1.Name"), Is.EqualTo("Graph1"));

            // Make sure we can get a property from a model in Field2/Field2SubZone from Field1 using a full path.
            Assert.That(simulation.Get(".Simulations.Test.Field2.Field2SubZone.Field2SubZoneReport.Name"), Is.EqualTo("Field2SubZoneReport"));
            
            // Test the in scope capability of get.
            Assert.That(simulation.Get("[Graph1].Name"), Is.EqualTo("Graph1"));
            Assert.That(simulation.Get("[Soil].Physical.Name"), Is.EqualTo("Physical"));
        }
        
        /// <summary>
        /// Tests for the set method
        /// </summary>
        [Test]
        public void SetTest()
        {
            Assert.That(this.simulation.Get("[Weather].Rain"), Is.EqualTo(0.0));
            this.simulation.Set("[Weather].Rain", 111.0);
            Assert.That(this.simulation.Get("[Weather].Rain"), Is.EqualTo(111.0));

            double[] thicknessBefore = (double[])simulation.FindByPath("[Physical].Thickness")?.Value;
            Assert.That(thicknessBefore.Length, Is.EqualTo(6)); // If APITest.xml is modified, this test will fail and must be updated.
            simulation.FindByPath("[Physical].Thickness[1]").Value = "20";
            double[] thicknessAfter = (double[])simulation.FindByPath("[Physical].Thickness")?.Value;

            Assert.That(thicknessAfter.Length, Is.EqualTo(thicknessBefore.Length));
            Assert.That(thicknessAfter[0], Is.EqualTo(20));
        }

        /// <summary>
        /// Tests for Children
        /// </summary>
        [Test]
        public void ChildrenTest()
        {
            IEnumerable<Zone> allChildren = simulation.FindAllChildren<Zone>();
            Assert.That(allChildren.Count(), Is.EqualTo(2));
        }
        
        /// <summary>
        /// Tests for Child method
        /// </summary>
        [Test]
        public void ChildTest()
        {
            IModel clock = simulation.FindChild<Clock>();
            Assert.That(clock, Is.Not.Null);
            clock = simulation.FindChild("Clock");
            Assert.That(clock, Is.Not.Null);
        }        

        /// <summary>
        /// Tests for siblings method
        /// </summary>
        [Test]
        public void SiblingsTest()
        {
            IModel clock = simulation.FindChild<Clock>();
            List<IModel> allSiblings = clock.FindAllSiblings().ToList();
            Assert.That(allSiblings.Count, Is.EqualTo(5));
        }

        /// <summary>
        /// Tests the move up down command
        /// </summary>
        [Test]
        public void MoveUpDown()
        {
            var tree = new MockTreeView();
            CommandHistory commandHistory = new CommandHistory(tree);
            Model modelToMove = simulations.FindByPath("APS14.Factors.Permutation.NRate")?.Value as Model;

            MoveModelUpDownCommand moveCommand = new MoveModelUpDownCommand(modelToMove, true);
            moveCommand.Do(tree, _ => {});

            Model modelToMove2 = simulations.FindByPath("APS14.Factors.NRate")?.Value as Model;

            Assert.That(simulations.Children[2].Children[0].Children[0].Children[0].Name, Is.EqualTo("NRate"));
            Assert.That(simulations.Children[2].Children[0].Children[0].Children[0].Children.Count, Is.EqualTo(4));
        }

       
    }
}
