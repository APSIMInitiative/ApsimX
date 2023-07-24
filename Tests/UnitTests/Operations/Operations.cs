using APSIM.Shared.Utilities;
using NUnit.Framework;
using System;
using Models;

namespace UnitTests
{
    [TestFixture]
    public class OperationsTests
    {
        [Test]
        public void TestOperationParsing()
        {
            string[] passingStrings =
            {
                "2000-01-01 [NodeName].Function(1000)",
                " 2000-01-01 [NodeName].Function(1000) ",
                "2000-01-01\t[NodeName].Function(1000)",
                "\t2000-01-01\t[NodeName].Function(1000)\t",
                "2000/01/01 [NodeName].Function(1000)",
                "//2000-01-01 [NodeName].Function(1000)",
                " // 2000-01-01 [NodeName].Function(1000) ",
                "//\t2000-01-01\t[NodeName].Function(1000)",
                "\t//\t2000-01-01\t[NodeName].Function(1000)\t",
                "//2000/01/01 [NodeName].Function(1000)"
            };

            Operation[] expectedOperations =
            {
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[0]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[1]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[2]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[3]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[4]),
                new Operation(false, "2000-01-01", "[NodeName].Function(1000)", passingStrings[5]),
                new Operation(false, "2000-01-01", "[NodeName].Function(1000)", passingStrings[6]),
                new Operation(false, "2000-01-01", "[NodeName].Function(1000)", passingStrings[7]),
                new Operation(false, "2000-01-01", "[NodeName].Function(1000)", passingStrings[8]),
                new Operation(false, "2000-01-01", "[NodeName].Function(1000)", passingStrings[9])
            };

            for (int i = 0; i < passingStrings.Length; i++)
            {
                Operation actualOperation = Operation.ParseOperationString(passingStrings[i]);
                Assert.AreEqual(expectedOperations[i].Enabled, actualOperation.Enabled);
                Assert.AreEqual(expectedOperations[i].Date, actualOperation.Date);
                Assert.AreEqual(expectedOperations[i].Action, actualOperation.Action);
            }

            string[] failingStrings =
            {
                "2000-13-01 [NodeName].Function(1000)", //bad date
                "2000-01-01[NodeName].Function(1000)",  //missing whitespace
                "[NodeName].Function(1000) 2000-01-01", //wrong order
                "2000-01-01 ",                          //missing action
                " [NodeName].Function(1000)",           //missing date
                "",                                     //empty string
                null,                                   //null
                "///2000-01-01 [NodeName].Function(1000)", //too many comments
            };

            for (int i = 0; i < failingStrings.Length; i++)
            {
                Assert.Null(Operation.ParseOperationString(failingStrings[i]));
            }
        }
    }
}
