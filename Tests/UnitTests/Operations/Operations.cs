using Models;
using NUnit.Framework;
using System.Reflection;

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
                "//2000/01/01 [NodeName].Function(1000)",
                "",
                "\n\t\r"
            };

            Operation[] expectedOperations =
            {
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[0]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[1]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[2]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[3]),
                new Operation(true, "2000-01-01", "[NodeName].Function(1000)", passingStrings[4]),
                new Operation(false, null, null, passingStrings[5]),
                new Operation(false, null, null, passingStrings[6]),
                new Operation(false, null, null, passingStrings[7]),
                new Operation(false, null, null, passingStrings[8]),
                new Operation(false, null, null, passingStrings[9]),
                new Operation(false, null, null, ""),
                new Operation(false, null, null, "")
            };

            for (int i = 0; i < passingStrings.Length; i++)
            {
                Operation actualOperation = Operation.ParseOperationString(passingStrings[i]);
                Assert.That(actualOperation.Enabled, Is.EqualTo(expectedOperations[i].Enabled));
                Assert.That(actualOperation.Date, Is.EqualTo(expectedOperations[i].Date));
                Assert.That(actualOperation.Action, Is.EqualTo(expectedOperations[i].Action));
            }

            string[] failingStrings =
            {
                "2000-13-01 [NodeName].Function(1000)", //bad date
                "2000-01-01[NodeName].Function(1000)",  //missing whitespace
                "[NodeName].Function(1000) 2000-01-01", //wrong order
                "2000-01-01 ",                          //missing action
                " [NodeName].Function(1000)",           //missing date
                null,                                   //null
                "/2000-01-01 [NodeName].Function(1000)", //not enough comments
            };

            for (int i = 0; i < failingStrings.Length; i++)
            {
                Assert.That(Operation.ParseOperationString(failingStrings[i]), Is.Null);
            }
        }

        private void Method1(int a, string b) { }

        /// <summary>Ensure that named arguments work on an operations line.</summary>
        [Test]
        public void EnsureNamedArgumentsWork()
        {

            var method1 = GetType().GetMethod("Method1", BindingFlags.Instance | BindingFlags.NonPublic);

            Operations operations = new();
            var argumentValues = Utilities.CallMethod(operations, "GetArgumentsForMethod", new object[] { new string[] { "b:1", "a:2" }, method1 }) as object[];

            Assert.That(argumentValues[0], Is.EqualTo(2));
            Assert.That(argumentValues[1], Is.EqualTo("1"));
        }

        private void Method2(int a, int[] b) { }

        /// <summary>Ensure that an array argument works on an operations line.</summary>
        [Test]
        public void EnsureArrayArgumentsWork()
        {
            var method2 = GetType().GetMethod("Method2", BindingFlags.Instance | BindingFlags.NonPublic);

            Operations operations = new();
            var arguments = new string[] { "1", "2 3" };
            var argumentValues = Utilities.CallMethod(operations, 
                                                      "GetArgumentsForMethod", 
                                                      new object[] { arguments, method2 }) as object[];

            Assert.That(argumentValues[0], Is.EqualTo(1));
            Assert.That(argumentValues[1], Is.EqualTo(new int[] { 2, 3 }));
        }

        /// <summary>Ensure yearless dates fire every year on that day-month, and yearful dates only on that day-month-year combo.</summary>
        [Test]
        public void TestTriggersOnDate()
        {
            var op1 = Operation.ParseOperationString("01-Jan [Plant].Sow()");
            var op2 = Operation.ParseOperationString("2026-01-01 [Plant].Sow()");

            Assert.That(op1.TriggersOnDate(new(2026, 1, 1)), Is.EqualTo(true));
            Assert.That(op1.TriggersOnDate(new(1900, 1, 1)), Is.EqualTo(true));
            Assert.That(op1.TriggersOnDate(new(2026, 2, 2)), Is.EqualTo(false));

            Assert.That(op2.TriggersOnDate(new(2026, 1, 1)), Is.EqualTo(true));
            Assert.That(op2.TriggersOnDate(new(1900, 1, 1)), Is.EqualTo(false));
            Assert.That(op2.TriggersOnDate(new(2026, 2, 2)), Is.EqualTo(false));
        }
    }
}
