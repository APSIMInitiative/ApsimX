// ----------------------------------------------------------------------
// <copyright file="BaseFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Struct
{
    using Models.Core;
    using Models.PMF.Functions;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// # [Name]
    /// Height is used by the MicroClimate model to calculate the aerodynamic resistance used for calculation of potential transpiration.
    /// Calculates the potential height increment and then multiplies it by the smallest of any childern functions (Child functions represent stress).
    /// </summary>
    [Serializable]
    public class HeightFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>All child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        [Link]
        private Plant plantModel = null;

        /// <summary>The potential height</summary>
        [Link]
        private IFunction PotentialHeight = null;

        /// <summary>The potential height yesterday</summary>
        private double PotentialHeightYesterday = 0;

        /// <summary>The height</summary>
        private double Height = 0;
        
        /// <summary>Gets or sets the height of the delta.</summary>
        [XmlIgnore]
        public double DeltaHeight { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double PotentialHeightIncrement = PotentialHeight.Value() - PotentialHeightYesterday;
            double StressValue = 1.0;
            //This function is counting potential height as a stress.
            foreach (IFunction F in childFunctions)
                StressValue = Math.Min(StressValue, F.Value());

            DeltaHeight = PotentialHeightIncrement * StressValue;
            PotentialHeightYesterday = PotentialHeight.Value();
            Height += DeltaHeight;
            returnValue[0] = Height;
            return returnValue;
        }

        /// <summary>Clear all variables</summary>
        private void Clear()
        {
            PotentialHeightYesterday = 0;
            Height = 0;
            DeltaHeight = 0;
        }

        /// <summary>Called when crop is sowing</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (sender == plantModel)
                Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == plantModel)
                Clear();
        }

    }
}
