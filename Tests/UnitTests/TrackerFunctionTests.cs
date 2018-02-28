// -----------------------------------------------------------------------
// <copyright file="TrackerFunctionTests.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UnitTests
{
    using Models;
    using Models.Core;
    using Models.Core.Interfaces;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using Models.PMF.Functions;

    [TestFixture]
    class TrackerFunctionTests
    {
        [Serializable]
        class ValuesFunction : IFunction
        {
            private double[] valuesToReturn;
            private int i = 0;
            public ValuesFunction(double[] values)
            {
                valuesToReturn = values;
            }
            public double Value()
            {
                return valuesToReturn[i++];
            }

            public double[] Values()
            {
                throw new NotImplementedException();
            }

            /// <summary>Gets the value, either a double or a double[]</summary>
            public object ValueAsObject()
            {
                throw new NotImplementedException();
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

            Assert.AreEqual(tracker.Value(), 20);

            Utilities.CallEvent(tracker, "EndEvent", null);

        }



    }
}
