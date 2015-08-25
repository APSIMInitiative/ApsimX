// -----------------------------------------------------------------------
// <copyright file="ITestable.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// An interface for something that is testable.
    /// </summary>
    public interface ITestable
    {
        /// <summary>Run tests. Should throw an exception if the test fails.</summary>
        void Test();
    }
}
