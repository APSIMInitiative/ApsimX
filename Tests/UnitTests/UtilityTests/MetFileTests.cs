using NUnit.Framework;
using System;
using APSIM.Shared.Utilities;

namespace UnitTests.UtilityTests
{
    [TestFixture]
    public class MetFileTests
    {
        [Test]
        public void Load_FromFlatValues_PreservesColumnIndexing()
        {
            // Arrange
            string[] constants = new[] { "latitude = 0" };
            string[] columns = new[] { "date", "col1", "col2", "col3" };
            string[] units = new[] { "", "u1", "u2", "u3" };
            int numColumns = columns.Length;
            int numDays = 5;

            // Create a flat values array where each element is unique so any
            // repetition due to incorrect indexing will be detected.
            double[] values = new double[numColumns * numDays];
            for (int day = 0; day < numDays; day++)
            {
                for (int col = 0; col < numColumns; col++)
                    values[day * numColumns + col] = day * 100.0 + col; // unique per (day,col)
            }

            string startDate = "2020-01-01";
            MetFile met = new MetFile();

            // Act
            met.Load(constants, columns, units, values, startDate);

            // Assert
            Assert.That(met.NumberOfDays, Is.EqualTo(numDays));
            DateTime start = DateTime.ParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            for (int day = 0; day < numDays; day++)
            {
                double[] row = met.GetDay(start.AddDays(day));
                for (int col = 0; col < numColumns; col++)
                {
                    double expected = values[day * numColumns + col];
                    Assert.That(row[col], Is.EqualTo(expected).Within(1e-9),
                        $"Mismatch at day {day} column {col}. Expected {expected} got {row[col]}");
                }
            }
        }
    }
}
