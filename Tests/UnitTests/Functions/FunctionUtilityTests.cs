using System;
using System.Collections.Generic;
using APSIM.Core;
using Models;
using Models.Core;
using Models.Functions;
using NUnit.Framework;

namespace UnitTests.Functions
{
    [TestFixture]
    class FunctionUtilityTests
    {
        /// <summary>Ensure the ValidateFunctionChildren throws when given a child that is not an IFunction</summary>
        [Test]
        public void ValidateFunctionChildrenThrows()
        {
            AddFunction addFunction = new AddFunction()
            {
                Children = new List<IModel>()
                {
                    new Constant() { Name = "A" },
                    new Constant() { Name = "B" },
                    new Summary() { }
                }
            };
            Node.Create(addFunction);
            Assert.Throws<Exception>(() => FunctionUtilities.ValidateFunctionChildren(addFunction.Node));
        }

        /// <summary>Ensure the ValidateFunctionChildren works when given a child that is a memo</summary>
        [Test]
        public void ValidateFunctionChildrenWorks()
        {
            AddFunction addFunction = new AddFunction()
            {
                Children = new List<IModel>()
                {
                    new Constant() { Name = "A" },
                    new Constant() { Name = "B" },
                    new Memo() { }
                }
            };
            Node.Create(addFunction);
            FunctionUtilities.ValidateFunctionChildren(addFunction.Node);
        }
    }
}
