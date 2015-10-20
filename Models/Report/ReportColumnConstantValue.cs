// -----------------------------------------------------------------------
// <copyright file="ReportColumnForFactorValue.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using PMF.Functions;

    /// <summary>A class for outputting a constant value in a report column.</summary>
    [Serializable]
    public class ReportColumnConstantValue : ReportColumn
    {
        /// <summary>The constant value</summary>
        private object constantValue;

        /// <summary>
        /// Constructor for a plain report variable.
        /// </summary>
        /// <param name="variableName">The name of the APSIM variable to retrieve</param>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="frequenciesFromReport">Reporting frequencies</param>
        /// <param name="parentModel">The parent model</param>
        /// <param name="value">The constant value</param>
        public ReportColumnConstantValue(string variableName, string columnName, string[] frequenciesFromReport, IModel parentModel, object value)
            : base(variableName, columnName, frequenciesFromReport, parentModel)
        {
            constantValue = value;
        }


        /// <summary>
        /// Retrieve the current value and store it in our array of values.
        /// </summary>
        public override void StoreValue()
        {
            values.Add(constantValue);
        }

    }
}
