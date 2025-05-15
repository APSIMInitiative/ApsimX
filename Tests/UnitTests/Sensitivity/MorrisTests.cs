using Models;
using Models.Core.ApsimFile;
using Models.Core;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using APSIM.Shared.Utilities;
using APSIM.Core;

namespace UnitTests.Sensitivity
{
    /// <summary>
    /// Tests for Morris.
    /// </summary>
    class MorrisTests
    {
        /// <summary>
        /// Reproduces GitHub bug #3393.
        /// </summary>
        /// <remarks>
        /// The type previously known as Models.Morris.Parameter was moved to
        /// its own file and renamed to Models.Sensitivity.Parameter. No
        /// converter was written for this change, so any json apsimx file
        /// which contains a Morris model is now broken.
        /// </remarks>
        [Test]
        public void EnsureParameterRenameWorks()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Sensitivity.MorrisTestsBroken.apsimx");
            Simulations simulations = NodeTree.CreateFromString<Simulations>(json, e => throw e, false).Root.Model as Simulations;
        }
    }
}
