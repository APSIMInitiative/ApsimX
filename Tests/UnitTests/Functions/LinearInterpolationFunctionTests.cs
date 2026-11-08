using APSIM.Core;
using Models.Core;
using Models.Functions;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.Functions
{
    [TestFixture]
    class LinearInterpolationFunctionTests
    {
        /// <summary>
        /// Y values that are all the same when the model is created, and are changed afterwards
        /// (e.g. by ModelOverrides or a cultivar command), must be interpolated, not returned as Y[0].
        /// </summary>
        [Test]
        public void ChangedYValuesAreUsedAfterCreation()
        {
            var xy = new XYPairs() { Name = "XYPairs", X = new[] { 0.0, 4.0 }, Y = new[] { 0.03, 0.03 } };
            var f = new LinearInterpolationFunction()
            {
                Children = new List<IModel>()
                {
                    xy,
                    new Constant() { Name = "XValue", FixedValue = 2 }
                }
            };
            Node.Create(f);
            new Links().Resolve(f, true);

            // As for a model created from an instance whose links are already resolved.
            f.OnCreated();
            Assert.That(f.Value(), Is.EqualTo(0.03));

            xy.Y = new[] { 0.02975, 0.035 };
            Assert.That(f.Value(), Is.EqualTo(0.032375).Within(1e-12));

            xy.Y = new[] { 0.04, 0.04 };
            Assert.That(f.Value(), Is.EqualTo(0.04));
        }
    }
}
