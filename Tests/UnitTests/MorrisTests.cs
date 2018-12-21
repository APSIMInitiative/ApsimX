using Models;
using Models.Core.ApsimFile;
using Models.Core;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using APSIM.Shared.Utilities;

namespace UnitTests
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.MorrisBroken.apsimx");
            List<Exception> exceptions;
            Simulations simulations = FileFormat.ReadFromString<Simulations>(json, out exceptions);
            if (exceptions != null && exceptions.Count > 0)
            {
                string message = string.Empty;
                foreach (Exception error in exceptions)
                    message += error.Message + Environment.NewLine;
                throw new Exception(message);
            }
        }
    }
}
