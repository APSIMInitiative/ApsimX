using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// A value is linearly interpolated between phenological growth stages
    /// </summary>
    [Serializable]
    [Description("A value is linearly interpolated between phenological growth stages")]
    public class StageBasedInterpolation : Model, IFunction
    {
        /// <summary>The _ proportional</summary>
        private bool _Proportional = true;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        // Parameters
        /// <summary>Gets or sets the stages.</summary>
        /// <value>The stages.</value>
        public string[] Stages { get; set; }
        /// <summary>Gets or sets the codes.</summary>
        /// <value>The codes.</value>
        public double[] Codes { get; set; }

        // States
        /// <summary>The stage codes</summary>
        private int[] StageCodes = null;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="StageBasedInterpolation"/> is proportional.
        /// </summary>
        /// <value><c>true</c> if proportional; otherwise, <c>false</c>.</value>
        public bool Proportional { get { return _Proportional; } set { _Proportional = value; } }

        /// <summary>Initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            StageCodes = null;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Something is a miss here.  Specifically, the number of values in your StageCode function don't match the number of stage names.  Sort it out numb nuts!!</exception>
        public double Value(int arrayIndex = -1)
        {
            if (StageCodes == null)
            {
                StageCodes = new int[Stages.Length];
                for (int i = 0; i < Stages.Length; i++)
                {
                    IPhase p = Phenology.PhaseStartingWith(Stages[i]);
                    StageCodes[i] = Phenology.IndexFromPhaseName(p.Name) + 1;
                }
            }

            //Fixme.  For some reason this error message won't cast properly??
            if (Codes.Length != StageCodes.Length)
            {
                throw new Exception("Something is a miss here.  Specifically, the number of values in your StageCode function don't match the number of stage names.  Sort it out numb nuts!!");
            }

            for (int i = 0; i < StageCodes.Length; i++)
            {
                if (Phenology.Stage <= StageCodes[i])
                {
                    if (i == 0)
                        return Codes[0];
                    if (Phenology.Stage == StageCodes[i])
                        return Codes[i];

                    if (Proportional)
                    {
                        double slope = MathUtilities.Divide(Codes[i] - Codes[i - 1],
                                                            StageCodes[i] - StageCodes[i - 1],
                                                            Codes[i]);
                        return Codes[i] + slope * (Phenology.Stage - StageCodes[i]);
                    }
                    else
                    {
                        // Simple lookup.
                        return Codes[i - 1];
                    }
                }
            }
            return Codes[StageCodes.Length - 1];
        }

    }
}
