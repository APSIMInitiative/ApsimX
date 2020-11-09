namespace UnitTests.APSIMShared
{
    using APSIM.Shared.Utilities;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class ReflectionUtilitiesTests
    {
        /// <summary>
        /// Test array conversion. Should allow for comma-separated values.
        /// </summary>
        [Test]
        public void TestStringToObjectArray()
        {
            string input = "a,b,c";
            object output = ReflectionUtilities.StringToObject(typeof(string[]), input);
            string[] expectedOutput = new string[3] { "a", "b", "c" };
            
            Assert.AreEqual(expectedOutput, output);
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
            
            Assert.AreEqual(expectedOutput, output);
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
            Assert.AreEqual(expectedOutput, output);
        }
    }
}
