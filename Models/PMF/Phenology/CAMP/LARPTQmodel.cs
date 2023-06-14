using System;
using Models.Core;

namespace Models.PMF.Phen
{

    /// <summary>
    /// Calculates the leaf appearance rate from photo thermal quotient
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class LARPTQmodel : Model
    {
        /// <summary>
        /// alculate the Leaf Appearance Rate at a given PTQ for a genotype of given minLAR and MaxLAR as formualted by Baumont etal 2019 Journal Expt Botany, Equation 3.
        /// </summary>
        /// <param name="PTQ">PhotoThermal Quatent (mmol PAR m-2 oCd-1)</param>
        /// <param name="maxLAR">Leaf Appearance Rate at PTQ = infinity (oCd-1)</param>
        /// <param name="minLAR">Leaf Appearance rate at PTQ = 0 (oCd-1)</param>
        /// <param name="PTQhf">PTQ half, controls the curvature of response</param>
        /// <returns></returns>
        public double CalculateLAR(double PTQ, double maxLAR, double minLAR, double PTQhf)
        {
            return minLAR + ((maxLAR - minLAR) * PTQ) / (PTQhf + PTQ);
        }
    }
}
