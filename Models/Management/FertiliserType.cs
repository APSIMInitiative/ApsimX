using System;
using System.Linq;
using Models.Core;
using Models.Functions;

namespace Models
{
    /// <summary>A class for holding a fertiliser type.</summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Fertiliser))]
    public class FertiliserType : Model
    {
        /// <summary>
        /// A function that defines the rate that this fertiliser type is released to the
        /// relevant solute pools. Rather than a separate lag phase, it can be incorporated
        /// into the release function. Too cumbersome?
        /// </summary>
        private IFunction releaseRate;

        /// <summary>A description of the fertiliser type.</summary>
        [Description("Description")]
        public string Description { get; set; }

        /// <summary>The fraction of fertiliser at which the remainder is released (e.g. when 0.995 released)</summary>
        [Description("The fraction of fertiliser at which the remainder is released (e.g. when 0.995 released)")]
        public double FractionWhenRemainderReleased { get; set; } = 0.995;

        /// <summary>The name of solute 1.</summary>
        [Description("Solute 1 name")]
        public string Solute1Name { get; set; }

        /// <summary>Solute 1 fraction.</summary>
        [Description("Fraction of solute 1 in fertiliser")]
        public double Solute1Fraction { get; set; }

        /// <summary>The name of solute 2.</summary>
        [Description("Solute 2 name")]
        public string Solute2Name { get; set; }

        /// <summary>Solute 2 fraction.</summary>
        [Description("Fraction of solute 2 in fertiliser")]
        public double Solute2Fraction { get; set; }

        /// <summary>The name of solute 3.</summary>
        [Description("Solute 3 name")]
        public string Solute3Name { get; set; }

        /// <summary>Solute 3 fraction.</summary>
        [Description("Fraction of solute 3 in fertiliser")]
        public double Solute3Fraction { get; set; }

        /// <summary>The name of solute 4.</summary>
        [Description("Solute 4 name")]
        public string Solute4Name { get; set; }

        /// <summary>Solute 4 fraction.</summary>
        [Description("Fraction of solute 4 in fertiliser")]
        public double Solute4Fraction { get; set; }

        /// <summary>The name of solute 5.</summary>
        [Description("Solute 5 name")]
        public string Solute5Name { get; set; }

        /// <summary>Solute 5 fraction.</summary>
        [Description("Fraction of solute 5 in fertiliser")]
        public double Solute5Fraction { get; set; }

        /// <summary>The name of solute 6.</summary>
        [Description("Solute 6 name")]
        public string Solute6Name { get; set; }

        /// <summary>Solute 6 fraction.</summary>
        [Description("Fraction of solute 6 in fertiliser")]
        public double Solute6Fraction { get; set; }

        /// <summary>Return the release rate for today.</summary>
        public double ReleaseRate
        {
            get
            {
                if (releaseRate == null) releaseRate = Children.First() as IFunction;
                return releaseRate.Value(-1);
            }
        }
    }
}