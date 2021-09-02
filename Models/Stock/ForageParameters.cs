using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using System;
using System.Linq;

namespace Models.GrazPlan
{
    /// <summary>Encapsulates parameters for a forage (e.g. wheat).</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Forages))]
    public class ForageParameters : Model
    {

        /// <summary>Parameters for a forage.</summary>
        [Link]
        public ForageMaterialParameter[] Parameters { get; set; }

        /// <summary>Return true if forage has grazable material.</summary>
        public bool HasGrazableMaterial => Parameters.FirstOrDefault(m => m.FractionConsumableLive > 0 || m.FractionConsumableDead > 0) != null;
    }

    /// <summary>Encapsulates parameters for a forage material (e.g. leaf, stem etc).</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(ForageParameters))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ForageMaterialParameter : Model
    {
        private IFunction digestibilityLiveFunction;
        private IFunction digestibilityDeadFunction;


        /// <summary>Digestibility of live material (0-1). Can be value or expresison.</summary>
        [Description("LIVE Digestibility")]
        public string DigestibilityLiveString { get; set; }

        /// <summary>Digestibility of dead material (0-1). Can be value or expresison.</summary>
        [Description("DEAD Digestibility")]
        public string DigestibilityDeadString { get; set; }

        /// <summary>Fraction of live material that is consumable.</summary>
        [Description("Fraction of LIVE material that is grazable.")]
        public double FractionConsumableLive { get; set; }

        /// <summary>Fraction of dead material that is consumable.</summary>
        [Description("Fraction of DEAD material that is grazable.")]
        public double FractionConsumableDead { get; set; }

        /// <summary>Digestibility of live material (0-1).</summary>
        public double DigestibilityLive => digestibilityLiveFunction.Value();

        /// <summary>Digestibility of dead material (0-1).</summary>
        public double DigestibilityDead => digestibilityDeadFunction.Value();

        /// <summary>Start of simulation.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (MathUtilities.IsNumerical(DigestibilityLiveString))
                digestibilityLiveFunction = new Constant() { FixedValue = Convert.ToDouble(DigestibilityLiveString) };
            else
                digestibilityLiveFunction = new ExpressionFunction() { Expression = DigestibilityLiveString };

            if (MathUtilities.IsNumerical(DigestibilityDeadString))
                digestibilityDeadFunction = new Constant() { FixedValue = Convert.ToDouble(DigestibilityDeadString) };
            else
                digestibilityDeadFunction = new ExpressionFunction() { Expression = DigestibilityDeadString };
        }
    }
}