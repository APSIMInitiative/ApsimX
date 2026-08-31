using NUnit.Framework;
using System;
using System.Collections.Generic;
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

        [Test]
        public void RawData_ReturnsRowsWithDatesAndValues()
        {
            string[] columns = ["date", "rain", "mint"];
            string[] units = ["", "mm", "C"];
            double[] values = 
            [
                0.0, 12.5, 4.2,
                0.0, 8.0, 3.1
            ];
            MetFile met = new MetFile();

            met.Load([], columns, units, values, "2020-01-01");

            string[][] rawData = met.GetData();

            Assert.That(rawData, Has.Length.EqualTo(2));
            Assert.That(rawData[0], Has.Length.EqualTo(3));
            Assert.That(DateTime.Parse(rawData[0][0]), Is.EqualTo(new DateTime(2020, 1, 1)));
            Assert.That(rawData[0][0], Is.EqualTo("2020-01-01"));
            Assert.That(rawData[0][1], Is.EqualTo("12.5"));
            Assert.That(rawData[0][2], Is.EqualTo("4.2"));
            Assert.That(rawData[1][0], Is.EqualTo("2020-01-02"));
            Assert.That(rawData[1][1], Is.EqualTo("8"));
            Assert.That(rawData[1][2], Is.EqualTo("3.1"));
        }

        [Test]
        public void ColumnsWithType_ReturnsTypesForValidMetFile()
        {
            string content = """
                [weather.met.weather]

                date rain mint maxt
                () (mm) (C) (C)
                2020-01-01 12.5 4.2 25.0
                """;

            MetFile met = MetFile.Create(content);

            Dictionary<string, string> columnsWithType = met.GetColumnDataTypes();

            Assert.That(columnsWithType, Is.EqualTo(new Dictionary<string, string>
            {
                ["date"] = "datetime",
                ["rain"] = "double",
                ["mint"] = "double",
                ["maxt"] = "double"
            }));
        }
    }
}
