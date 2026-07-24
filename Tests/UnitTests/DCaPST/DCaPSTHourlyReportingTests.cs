using Models;
using Models.DCAPST;
using Models.DCAPST.Canopy;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class DCaPSTHourlyReportingTests
    {
        [Test]
        public void PublishIntervalOutputsRaisesAnEventForEachInterval()
        {
            var model = new DCaPSTModelNG();
            var clock = new Clock();
            var today = new DateTime(2026, 7, 17);
            Utilities.SetProperty(clock, nameof(Clock.Today), today);
            Utilities.InjectLink(model, "clock", clock);

            model.DcapstModel.Intervals = new[]
            {
                CreateInterval(6, 20, 1),
                CreateInterval(7, 21, 101)
            };

            var outputs = new List<IntervalOutput>();
            model.IntervalStep += (_, _) => outputs.Add(new IntervalOutput
            {
                DateTime = model.IntervalDateTime,
                Hour = model.Hour,
                AirTemperature = model.AirTemperature,
                SunlitLAI = model.SunlitLAI,
                ShadedLAI = model.ShadedLAI,
                CanopyTemperature = model.CanopyTemperature,
                CanopyVPD = model.CanopyVPD,
                SunlitAssimilation = model.SunlitAssimilation,
                SunlitWater = model.SunlitWater,
                SunlitTemperature = model.SunlitTemperature,
                SunlitVPD = model.SunlitVPD,
                SunlitAc1 = model.SunlitAc1,
                SunlitAc2 = model.SunlitAc2,
                SunlitAj = model.SunlitAj,
                ShadedAssimilation = model.ShadedAssimilation,
                ShadedWater = model.ShadedWater,
                ShadedTemperature = model.ShadedTemperature,
                ShadedVPD = model.ShadedVPD,
                ShadedAc1 = model.ShadedAc1,
                ShadedAc2 = model.ShadedAc2,
                ShadedAj = model.ShadedAj
            });

            Utilities.CallMethod(model, "PublishIntervalOutputs", Array.Empty<object>());

            Assert.That(outputs, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(outputs[0].DateTime, Is.EqualTo(today.AddHours(6)));
                Assert.That(outputs[0].Hour, Is.EqualTo(6));
                Assert.That(outputs[0].AirTemperature, Is.EqualTo(20));
                Assert.That(outputs[0].SunlitLAI, Is.EqualTo(1));
                Assert.That(outputs[0].ShadedLAI, Is.EqualTo(3));
                Assert.That(outputs[0].CanopyTemperature, Is.EqualTo(10.5));
                Assert.That(outputs[0].CanopyVPD, Is.EqualTo(14.5));
                Assert.That(outputs[0].SunlitAssimilation, Is.EqualTo(1));
                Assert.That(outputs[0].SunlitWater, Is.EqualTo(2));
                Assert.That(outputs[0].SunlitTemperature, Is.EqualTo(3));
                Assert.That(outputs[0].SunlitVPD, Is.EqualTo(7));
                Assert.That(outputs[0].SunlitAc1, Is.EqualTo(4));
                Assert.That(outputs[0].SunlitAc2, Is.EqualTo(5));
                Assert.That(outputs[0].SunlitAj, Is.EqualTo(6));
                Assert.That(outputs[0].ShadedAssimilation, Is.EqualTo(11));
                Assert.That(outputs[0].ShadedWater, Is.EqualTo(12));
                Assert.That(outputs[0].ShadedTemperature, Is.EqualTo(13));
                Assert.That(outputs[0].ShadedVPD, Is.EqualTo(17));
                Assert.That(outputs[0].ShadedAc1, Is.EqualTo(14));
                Assert.That(outputs[0].ShadedAc2, Is.EqualTo(15));
                Assert.That(outputs[0].ShadedAj, Is.EqualTo(16));
                Assert.That(outputs[1].DateTime, Is.EqualTo(today.AddHours(7)));
                Assert.That(outputs[1].Hour, Is.EqualTo(7));
                Assert.That(outputs[1].SunlitAssimilation, Is.EqualTo(101));
            });
        }

        [Test]
        public void PublishIntervalOutputsDoesNothingWhenNoIntervalsWereCalculated()
        {
            var model = new DCaPSTModelNG();
            int eventCount = 0;
            model.IntervalStep += (_, _) => eventCount++;

            Utilities.CallMethod(model, "PublishIntervalOutputs", Array.Empty<object>());

            Assert.That(eventCount, Is.Zero);
        }

        private static IntervalValues CreateInterval(double hour, double airTemperature, double value)
        {
            return new IntervalValues
            {
                Time = hour,
                AirTemperature = airTemperature,
                SunlitLAI = 1,
                ShadedLAI = 3,
                Sunlit = CreateArea(value),
                Shaded = CreateArea(value + 10)
            };
        }

        private static AreaValues CreateArea(double value)
        {
            return new AreaValues
            {
                A = value,
                Water = value + 1,
                Temperature = value + 2,
                VPD = value + 6,
                Ac1 = new PathValues { Assimilation = value + 3 },
                Ac2 = new PathValues { Assimilation = value + 4 },
                Aj = new PathValues { Assimilation = value + 5 }
            };
        }

        private class IntervalOutput
        {
            public DateTime DateTime { get; set; }
            public double Hour { get; set; }
            public double AirTemperature { get; set; }
            public double SunlitLAI { get; set; }
            public double ShadedLAI { get; set; }
            public double CanopyTemperature { get; set; }
            public double CanopyVPD { get; set; }
            public double SunlitAssimilation { get; set; }
            public double SunlitWater { get; set; }
            public double SunlitTemperature { get; set; }
            public double SunlitVPD { get; set; }
            public double SunlitAc1 { get; set; }
            public double SunlitAc2 { get; set; }
            public double SunlitAj { get; set; }
            public double ShadedAssimilation { get; set; }
            public double ShadedWater { get; set; }
            public double ShadedTemperature { get; set; }
            public double ShadedVPD { get; set; }
            public double ShadedAc1 { get; set; }
            public double ShadedAc2 { get; set; }
            public double ShadedAj { get; set; }
        }
    }
}
