using System;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{

    /// <summary>
    /// This class holds the functions for calculating the Nutrient concentration thresholds
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Respiration))]
    public class NutrientFunctions : Model
    {
        /// <summary>Parameter relevent to Carbon</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction C = null;
        /// <summary>Parameter relevent to Nitrogen</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction N = null;
        /// <summary>Parameter relevent to Phosphorus</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction P = null;
        /// <summary>Parameter relevent to Potassium</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction K = null;

        /// <summary> Interface member that is got by other methods </summary>
        public NutrientsStates NutrientValues
        {
            get
            {
                NutrientsStates states = new NutrientsStates(
                    C.Value(),
                    N.Value(),
                    P.Value(),
                    K.Value());
                return states;
            }
        }
    }
}
