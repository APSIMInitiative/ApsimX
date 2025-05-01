using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// A value is linearly interpolated between phenological growth stages
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A value is linearly interpolated between phenological growth stages")]
    public class StageBasedInterpolation : Model, IFunction
    {
        /// <summary>The _ proportional</summary>
        private bool _Proportional = true;

        /// <summary>The phenology</summary>
        [Link]
        Phenology phenology = null;

        // Parameters
        /// <summary>Gets or sets the codes.</summary>
        /// <value>The codes.</value>
        [Description("Values at each stage")]
        public double[] Values { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="StageBasedInterpolation"/> is proportional.
        /// </summary>
        /// <value><c>true</c> if proportional; otherwise, <c>false</c>.</value>
        public bool Proportional { get { return _Proportional; } set { _Proportional = value; } }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Something is a miss here.  Specifically, the number of values in your StageCode function don't match the number of stage names.  Sort it out numb nuts!!</exception>
        public double Value(int arrayIndex = -1)
        {
            foreach (int s in phenology.StageCodes)
            {
                if ((int)phenology.Stage == s)
                {
                    if (Proportional)
                    {
                        double slope = MathUtilities.Divide(Values[s+1] - Values[s],1,s);
                        return Values[s] + slope * (phenology.Stage - (s));
                    }
                    else
                    {
                        // Simple lookup.
                        return Values[s];
                    }
                }
            }
            return Values[phenology.StageCodes.Count - 1];
        }

    }
}
