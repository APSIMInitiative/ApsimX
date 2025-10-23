namespace UnitTests.APSIMShared
{
    using APSIM.Core;
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using System;
    using System.IO;

    [TestFixture]
    public class ReflectionUtilitiesTests
    {
        /// <summary>Test csv string to string[] conversion.</summary>
        [Test]
        public void TestStringToObjectArray()
        {
            Assert.That(ReflectionUtilities.StringToObject(typeof(string[]), "a,b,c"),
                Is.EqualTo(new string[3] { "a", "b", "c" }));
        }

        /// <summary>Test yyyy-mm-dd string to DateTime conversion.</summary>
        [Test]
        public void TestYYYYMMDDStringToDate()
        {
            Assert.That(ReflectionUtilities.StringToObject(typeof(DateTime), "2000-01-01"),
                Is.EqualTo(new DateTime(2000, 1, 1)));
        }

        /// <summary>Test full date string to DateTime conversion.</summary>
        [Test]
        public void TestFullDateStringToDate()
        {
            Assert.That(ReflectionUtilities.StringToObject(typeof(DateTime), "2000-01-10T00:00:00"),
                Is.EqualTo(new DateTime(2000, 1, 10)));
        }

        /// <summary>
        /// Test array conversion. Should allow for spaces between comma-separated values.
        /// </summary>
        [Test]
        public void TestStringToObjectArrayWithSpaces()
        {
            string input = "a, b , c";
            object output = ReflectionUtilities.StringToObject(typeof(string[]), input);
            string[] expectedOutput = new string[3] { "a", "b ", "c" };

            Assert.That(output, Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Test array conversion to string. Should generate comma-separated values
        /// with a space after each comma.
        /// </summary>
        [Test]
        public void TestArrayObjectToString()
        {
            string[] input = new[] { "a", "b", " c", "d " };
            string output = ReflectionUtilities.ObjectToString(input);
            string expectedOutput = "a, b,  c, d ";
            Assert.That(output, Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Test binary deserealization of SimulationException.
        /// </summary>
        [Test]
        public void TestDeserializeSimulationException()
        {
            SimulationException exception = new SimulationException("Custom message", "Name of the simulation", "The filename");
            using (Stream stream = ReflectionUtilities.JsonSerialiseToStream(exception))
            {
                stream.Seek(0, SeekOrigin.Begin);
                SimulationException cloned = (SimulationException)ReflectionUtilities.JsonDeserialise(stream);
                Assert.That(cloned.Message, Is.EqualTo(exception.Message));
                Assert.That(cloned.SimulationName, Is.EqualTo(exception.SimulationName));
                Assert.That(cloned.FileName, Is.EqualTo(exception.FileName));
            }
        }

        class ModelWithNode
        {
            public Node Node;
        }

        /// <summary>
        /// Ensure clone doesn't clone Node instances
        /// </summary>
        [Test]
        public void TestNodeDoesntClone()
        {
            ModelWithNode modelA = new()
            {
                Node = Node.Create(new Clock())
            };

            var newModelA = ReflectionUtilities.Clone(modelA) as ModelWithNode;
            Assert.That(newModelA.Node, Is.Null);
        }

        class ModelWithStructure
        {
            public IStructure Structure;
        }

        /// <summary>
        /// Ensure clone doesn't clone Structure instances
        /// </summary>
        [Test]
        public void TestStructureDoesntClone()
        {
            ModelWithStructure modelB = new()
            {
                Structure = Node.Create(new Clock())
            };

            var newModelB = ReflectionUtilities.Clone(modelB) as ModelWithStructure;
            Assert.That(newModelB.Structure, Is.Null);
        }

    }
}
