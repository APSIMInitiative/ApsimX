using Models.CLEM.Resources;
using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.CLEM
{
    [TestFixture]
    public class LandAllocationTest
    {
         

        Land land = new();
        LandType landType = new() { LandArea = 100 };

        [SetUp]
        public void SetUp()
        {
            land.Children.Add(landType);


        }

        [Test]
        public void TestLandAllocation_PlainAllocation()
        {
            Assert.That(true, Is.True);
        }

    }
}
