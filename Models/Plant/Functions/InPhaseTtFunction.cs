// -----------------------------------------------------------------------
// <copyright file="InPhaseTtFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using Models.PMF.Phen;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Returns the thermal time accumulation from the current phase in phenology
    /// </summary>
    [Description("Returns the thermal time accumulation from the current phase in phenology")]
    [Serializable]
    public class InPhaseTtFunction : BaseFunction
    {
        /// <summary>The phenology</summary>
        [Link]
        private Phenology phenologyModel = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + phenologyModel.CurrentPhase.TTinPhase);
            return new double[] { phenologyModel.CurrentPhase.TTinPhase };
        }
    }
}
