using APSIM.Core;
using Models;
using Models.DCAPST;
using Models.DCAPST.Canopy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class DCaPSTHourlyReportingTests
    {
        [Test]
        public void PublishIntervalOutputsRaisesAnEventForEachInterval()
        {
            var model = new DCaPSTModelNG { Name = "DCaPST" };
            Node modelNode = Node.Create(model);
            var clock = new Clock();
            var today = new DateTime(2026, 7, 17);
            Utilities.SetProperty(clock, nameof(Clock.Today), today);
            Utilities.InjectLink(model, "clock", clock);

            model.DcapstModel.Intervals = new[]
            {
                CreateInterval(6, 20, 1),
                CreateInterval(7, 21, 101)
            };

            var outputs = new List<DCaPSTIntervalOutput>();
            var locatedHours = new List<object>();
            model.IntervalStep += (_, output) =>
            {
                Assert.That(model.CurrentInterval, Is.SameAs(output));
                outputs.Add(output);
                locatedHours.Add(modelNode.Get("[DCaPST].CurrentInterval.Hour", relativeTo: model));
            };

            Utilities.CallMethod(model, "PublishIntervalOutputs", Array.Empty<object>());

            Assert.That(outputs, Has.Count.EqualTo(2));
            Assert.That(locatedHours, Is.EqualTo(new object[] { 6.0, 7.0 }));
            Assert.That(model.CurrentInterval, Is.Null);
            Assert.That(modelNode.Get("[DCaPST].CurrentInterval.Hour", relativeTo: model), Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(outputs[0].IntervalDateTime, Is.EqualTo(today.AddHours(6)));
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
                Assert.That(outputs[1].IntervalDateTime, Is.EqualTo(today.AddHours(7)));
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

        [Test]
        public void PublishIntervalOutputsClearsCurrentIntervalWhenSubscriberThrows()
        {
            var model = new DCaPSTModelNG();
            var clock = new Clock();
            Utilities.SetProperty(clock, nameof(Clock.Today), new DateTime(2026, 7, 17));
            Utilities.InjectLink(model, "clock", clock);
            model.DcapstModel.Intervals = new[] { CreateInterval(6, 20, 1) };
            model.IntervalStep += (_, _) => throw new InvalidOperationException("Subscriber failed");

            var error = Assert.Throws<TargetInvocationException>(() =>
                Utilities.CallMethod(model, "PublishIntervalOutputs", Array.Empty<object>()));

            Assert.Multiple(() =>
            {
                Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(model.CurrentInterval, Is.Null);
            });
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

    }
}
