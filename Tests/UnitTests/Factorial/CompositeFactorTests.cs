using System;
using System.Collections.Generic;
using Models;
using Models.Core;
using Models.Factorial;
using NUnit.Framework;

namespace UnitTests.Factorial
{
    [TestFixture]
    public class CompositeFactorTests
    {
        [Test]
        public void RetrieveActiveSpecificationsSkipsEmptyCommentsAndDisabledChildModelSpecifications()
        {
            CompositeFactor compositeFactor = new CompositeFactor();
            List<IModel> children = new List<IModel>
            {
                new MockClock { Name = "Clock", Enabled = true },
                new MockClock { Name = "DisabledClock", Enabled = false }
            };
            string[] specifications =
            {
                "",
                "//[Clock]",
                "[Clock]",
                "[DisabledClock]"
            };

            List<string> activeSpecifications = compositeFactor.RetrieveActiveSpecifications(children, specifications);

            Assert.That(activeSpecifications.Count, Is.EqualTo(1));
            Assert.That(activeSpecifications[0], Is.EqualTo("[Clock]"));
        }

        [Test]
        public void RetrieveActiveSpecificationsTreatsDisabledChildNameComparisonAsCaseInsensitive()
        {
            CompositeFactor compositeFactor = new CompositeFactor();
            List<IModel> children = new List<IModel>
            {
                new MockClock { Name = "DisabledClock", Enabled = false }
            };
            string[] specifications =
            {
                "[DISABLEDCLOCK]"
            };

            List<string> activeSpecifications = compositeFactor.RetrieveActiveSpecifications(children, specifications);

            Assert.That(activeSpecifications.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnsureAModelExistsForEachSpecificationThrowsWhenSpecificationForChildModelIsMissing()
        {
            CompositeFactor compositeFactor = new CompositeFactor
            {
                Name = "Composite",
                Specifications = ["[Clock]"]
            };
            List<IModel> children = new List<IModel>
            {
                new MockClock { Name = "Clock" },
                new MockClock { Name = "Clock2" }
            };

            Exception error = Assert.Throws<Exception>(() => compositeFactor.EnsureAModelExistsForEachSpecification(children, compositeFactor.Specifications));

            Assert.That(error.Message, Does.Contain("Clock2"));
        }

        [Test]
        public void EnsureAModelExistsForEachSpecificationDoesNotThrowWhenAllChildModelSpecificationsExist()
        {
            CompositeFactor compositeFactor = new CompositeFactor
            {
                Name = "Composite",
                Specifications = ["[Clock]", "[Clock2]"]
            };
            List<IModel> children = new List<IModel>
            {
                new MockClock { Name = "Clock" },
                new MockClock { Name = "Clock2" }
            };

            Assert.DoesNotThrow(() => compositeFactor.EnsureAModelExistsForEachSpecification(children, compositeFactor.Specifications));
        }

        [Test]
        public void EnsureAModelExistsForEachSpecificationIgnoresITextChildren()
        {
            CompositeFactor compositeFactor = new CompositeFactor
            {
                Name = "Composite",
                Specifications = ["[Clock]"]
            };
            List<IModel> children = new List<IModel>
            {
                new MockClock { Name = "Clock" },
                new Memo { Name = "Note" }
            };

            Assert.DoesNotThrow(() => compositeFactor.EnsureAModelExistsForEachSpecification(children, compositeFactor.Specifications));
        }
    }
}
