using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("A value is linearly interpolated between phenological growth stages")]
    public class StageBasedInterpolation : Function
    {
        [Link]
        Phenology Phenology = null;

        // Parameters
        public string[] Stages { get; set; }
        public double[] Codes { get; set; }

        // States
        private int[] StageCodes = null;

        public bool Proportional { get; set; }

        /// <summary>
        /// Initialise ourselves.
        /// </summary>
        public override void OnCommencing()
        {
            StageCodes = null;
            Proportional = true;
        }
        
        public override double Value
        {
            get
            {


                if (StageCodes == null)
                {
                    StageCodes = new int[Stages.Length];
                    for (int i = 0; i < Stages.Length; i++)
                    {
                        Phase p = Phenology.PhaseStartingWith(Stages[i]);
                        StageCodes[i] = Phenology.IndexOfPhase(p.Name) + 1;
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
                            double slope = Utility.Math.Divide(Codes[i] - Codes[i - 1],
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
}
