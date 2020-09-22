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
        /// Tests the StringToObject conversion utility.
        /// </summary>
        [Test]
        public void TestStringToObject()
        {
            // Test array conversion. Should allow for comma-separated values.
            string input = "a,b,c";
            object output = ReflectionUtilities.StringToObject(typeof(string[]), input);
            string[] expectedOutput = new string[3] { "a", "b", "c" };
            
            Assert.AreEqual(expectedOutput, output);
        }

        /// <summary>
        /// Tests the StringToObject conversion utility.
        /// </summary>
        [Test]
        public void TestStringToObjectWithSpaces()
        {
            // Test array conversion. Should allow for spaces between comma-separated values.
            string input = "a, b , c";
            object output = ReflectionUtilities.StringToObject(typeof(string[]), input);
            string[] expectedOutput = new string[3] { "a", "b ", "c" };
            
            Assert.AreEqual(expectedOutput, output);
        }
    }
}
