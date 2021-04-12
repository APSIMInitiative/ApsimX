using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Models;
using Models.Core;
using NUnit.Framework;

namespace UnitTests.Core
{
    /// <summary>
    /// Unit tests for the auto documentation.
    /// </summary>
    class DocumentationTests
    {
        /// <summary>
        /// Documents all models which implement ICustomDocumentation.
        /// </summary>
        [Test]
        public void DocumentAllModels()
        {
            Assembly models = typeof(IModel).Assembly;
            int successes = 0;
            int failures = 0;
            foreach (Type t in models.GetTypes())
            {
                // Skip this type if it's not an implementation of ICustomDocumentation,
                // or if it's an interface.
                if (!typeof(IModel).IsAssignableFrom(t) || t.GetMethod(nameof(IModel.Document)) == null)
                    continue;
                IModel instance = models.CreateInstance(t.FullName) as IModel;
                try
                {
                    var tags = instance.Document();
                    successes++;
                }
                catch (Exception err)
                {
                    TestContext.Error.WriteLine("Error documenting {0}: {1}", t.Name, err.ToString());
                    failures++;
                }
            }
            if (failures > 0)
            {
                int total = successes + failures;
                double successRate = Math.Round((double)successes / total, 2);
                TestContext.WriteLine();
                TestContext.WriteLine("Success ratio: {0} ({1}/{2}).", successRate, successes, total);
                Assert.Fail();
            }
        }
    }
}
