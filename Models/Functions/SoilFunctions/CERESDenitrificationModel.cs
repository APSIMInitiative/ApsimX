using System;
using Models.Core;
using Models.Soils.Nutrients;

namespace Models.Functions
{
    /// <summary>Fraction of NO3 which denitrifies today</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NO3 denitrified.
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [Serializable]
    [Description("Soil NO3 Denitrification model from CERES-Maize")]
    public class CERESDenitrificationModel : Model, IFunction
    {
        [Link]
        Soils.IPhysical soilPhysical = null;

        [Link(ByName = true)]
        IOrganicPool Humic = null;

        [Link(ByName = true)]
        IOrganicPool Inert = null;

        [Link(ByName = true)]
        IOrganicPool FOMCarbohydrate = null;

        [Link(ByName = true)]
        IOrganicPool FOMCellulose = null;

        [Link(ByName = true)]
        IOrganicPool FOMLignin = null;


        [Link(Type = LinkType.Child, ByName = true)]
        IFunction CERESDenitrificationTemperatureFactor = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction CERESDenitrificationWaterFactor = null;

        /// <summary>
        /// Rate modifier on the CERES denitrification model. Default = 0.0006.
        /// </summary>
        [Description("Denitrification rate modifier")]
        public double DenitrificationRateModifier { get; set; } = 0.0006;

        /// <summary>
        /// Kludge
        /// </summary>
        [Description("Is inert pool active?")]
        public bool IsInertActive { get; set; } = true;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Denitrification Model");
            double ActiveC;
            if (IsInertActive)
                ActiveC = Humic.C[arrayIndex] + Inert.C[arrayIndex] + FOMCarbohydrate.C[arrayIndex] + FOMCellulose.C[arrayIndex] + FOMLignin.C[arrayIndex];
            else
                ActiveC = Humic.C[arrayIndex] + 0.0 + FOMCarbohydrate.C[arrayIndex] + FOMCellulose.C[arrayIndex] + FOMLignin.C[arrayIndex];

            double ActiveCppm = ActiveC / (soilPhysical.BD[arrayIndex] * soilPhysical.Thickness[arrayIndex] / 100);
            double CarbonModifier = 0.0031 * ActiveCppm + 24.5;
            double PotentialRate = DenitrificationRateModifier * CarbonModifier;

            return PotentialRate * CERESDenitrificationTemperatureFactor.Value(arrayIndex) * CERESDenitrificationWaterFactor.Value(arrayIndex);
        }

        /// <summary>
        /// Get the values for all soil layers.
        /// </summary>
        public double[] Values
        {
            get
            {
                if (soilPhysical == null)
                    return null;
                double[] result = new double[soilPhysical.Thickness.Length];
                for (int i = 0; i < result.Length; i++)
                    result[i] = Value(i);
                return result;
            }
        }
    }
}