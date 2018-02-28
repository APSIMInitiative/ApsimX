// ----------------------------------------------------------------------
// <copyright file="StageBasedInterpolation.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF.Phen;
    using System;

    /// <summary>
    /// # [Name]
    /// A value is linearly interpolated between phenological growth stages
    /// </summary>
    [Serializable]
    [Description("A value is linearly interpolated between phenological growth stages")]
    public class StageBasedInterpolation : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];
        
        /// <summary>The phenology</summary>
        [Link]
        Phenology phenologyModel = null;

        /// <summary>The stage codes</summary>
        private int[] stageCodes = null;

        /// <summary>Gets or sets the stages.</summary>
        public string[] Stages { get; set; }

        /// <summary>Gets or sets the codes.</summary>
        public double[] Codes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="StageBasedInterpolation"/> is proportional.
        /// </summary>
        public bool Proportional { get; set; }

        /// <summary>Initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            stageCodes = null;
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (stageCodes == null)
            {
                stageCodes = new int[Stages.Length];
                for (int i = 0; i < Stages.Length; i++)
                {
                    Phase p = phenologyModel.PhaseStartingWith(Stages[i]);
                    stageCodes[i] = phenologyModel.IndexOfPhase(p.Name) + 1;
                }
            }

            if (Codes.Length != stageCodes.Length)
                throw new Exception("The number of codes and stage codes does not match");

            for (int i = 0; i < stageCodes.Length; i++)
            {
                if (phenologyModel.Stage <= stageCodes[i])
                {
                    if (i == 0)
                        returnValue[0] = Codes[0];
                    else if (phenologyModel.Stage == stageCodes[i])
                        returnValue[0] = Codes[i];

                    else if (Proportional)
                    {
                        double slope = MathUtilities.Divide(Codes[i] - Codes[i - 1],
                                                            stageCodes[i] - stageCodes[i - 1],
                                                            Codes[i]);
                        returnValue[0] = Codes[i] + slope * (phenologyModel.Stage - stageCodes[i]);
                    }
                    else
                    {
                        // Simple lookup.
                        returnValue[0] = Codes[i - 1];
                    }

                    return returnValue;
                }
            }
            returnValue[0] = Codes[stageCodes.Length - 1];
            return returnValue;
        }
        
    }
}
