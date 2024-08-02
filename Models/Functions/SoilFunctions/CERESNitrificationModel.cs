using System;
using Models.Core;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>Fraction of NH4 which nitrifies today</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Soil NH4 Nitrification model from CERES-Maize")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class CERESNitrificationModel : Model, IFunction
    {

        [Link(ByName = true)]
        Solute NH4 = null;

        [Link(ByName = true)]
        CERESMineralisationTemperatureFactor TF = null;

        [Link(Type = LinkType.Child)]
        CERESNitrificationWaterFactor CERESWF = null;

        [Link(Type = LinkType.Child)]
        CERESNitrificationpHFactor CERESpHF = null;

        /// <summary>
        /// Potential Nitrification Rate at high NH4 concentration and optimal soil conditions.
        /// </summary>
        [Description("Potential Nitrification Rate")]
        [Units("kg/ha/d")]
        public double PotentialNitrificationRate { get; set; } = 40;

        /// <summary>
        /// NH4 concentration at which nitrification would be half the potential rate.
        /// </summary>
        [Description("Concentration at Half Max")]
        [Units("ppm")]
        public double ConcentrationAtHalfMax { get; set; } = 90;

        /// <summary>
        /// Nitirification inhibition function.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction NitrificationInhibition { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Nitrification Model");

            double PotentialRate = PotentialNitrificationRate / (NH4.ppm[arrayIndex] + ConcentrationAtHalfMax);

            double RateModifier = TF.Value(arrayIndex);
            RateModifier = Math.Min(RateModifier, CERESWF.Value(arrayIndex));
            RateModifier = Math.Min(RateModifier, CERESpHF.Value(arrayIndex));

            double inhibitor = 1;
            if (NitrificationInhibition != null)
                inhibitor = NitrificationInhibition.Value();

            return PotentialRate * RateModifier * inhibitor;
        }
    }
}