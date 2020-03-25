namespace UnitTests
{
    using Models;
    using Models.Core;
    using Models.Soils;
    using System;

    [Serializable]
    class MockModelValuesChangeDaily : Model
    {
        private double[] aValues;
        private double[] bValues;
        private int dayIndex = 0;

        public MockModelValuesChangeDaily(double[] aDailyValues, double[] bDailyValues)
        {
            aValues = aDailyValues;
            bValues = bDailyValues;
        }

        public double A { get; private set; }
        public double B { get; private set; }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            A = aValues[dayIndex];
            B = bValues[dayIndex];
            dayIndex++;
        }
    }
}
