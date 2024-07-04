using Models;
using Models.Core;
using Models.Core.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Models.Functions;

namespace UnitTests.Functions
{
    [TestFixture]
    class TrackerFunctionTests
    {
        [Serializable]
        class ValuesFunction : Model, IFunction
        {
            private double[] valuesToReturn;
            private int i = 0;
            public ValuesFunction(double[] values)
            {
                valuesToReturn = values;
            }
            public double Value(int arrayIndex = -1)
            {
                return valuesToReturn[i++];
            }
        }

        /// <summary>Ensure the tracker function actually works.</summary>
        [Test]
        public void TrackerFunction_EnsureWorks()
        {
            // Create a tree with a root node for our models.
            TrackerFunction tracker = new TrackerFunction();
            tracker.Statistic = "value back 14";

            Utilities.InjectLink(tracker, "variable", new ValuesFunction(new double[] { 10, 20, 30, 40, 50 }));
            Utilities.InjectLink(tracker, "referenceVariable", new ValuesFunction(new double[] { 1, 2, 3, 4, 5 }));

            Utilities.CallEvent(tracker, "StartEvent", null);

            Utilities.CallEvent(tracker, "DoDailyTracking", null);
            Utilities.CallEvent(tracker, "DoDailyTracking", null);
            Utilities.CallEvent(tracker, "DoDailyTracking", null);
            Utilities.CallEvent(tracker, "DoDailyTracking", null);
            Utilities.CallEvent(tracker, "DoDailyTracking", null);

            Assert.That(tracker.Value(), Is.EqualTo(20));

            Utilities.CallEvent(tracker, "EndEvent", null);

        }



    }
}
