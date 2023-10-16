using System;
using Models.Core;
using Models.Functions;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Upregulation of Vrn1 from cold.  Is additional to base vrn1.
    /// BaseDVrn1 in seperate calculation otherwise te same as Brown etal 2013
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class ColdVrnResponse : Model, IFunction, IIndexedFunction
    {

        [Link(ByName = true, Type = LinkType.Ancestor)]
        CAMP camp = null;

        /// <summary> The k factor controls the shape of the exponential decline of vernalisation with temperature </summary>
        [Description("The exponential shape factor")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction k = null;

        /// <summary> The temperature above which Vrn1 is down regulated </summary>
        [Description("The temperature above which Vrn1 is down regulated")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction deVernalisationTemp = null;

        /// <summary> The rate (/d) that Vrn1 is down regulated when temp is over DVernalisationTemp </summary>
        [Description("The rate (/d) that Vrn1 is down regulated when temp is over DVernalisationTemp")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction deVernalisationRate = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value(int arrayIndex = -1)
        {
            throw new Exception("Cannot call Value onColdUpRegVrn1 function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            if (camp.Params != null)
            {
                double UdVrn1 = Math.Exp(k.Value() * dX);
                if (dX < deVernalisationTemp.Value())
                    return UdVrn1/24;
                else
                {
                    return deVernalisationRate.Value()/24;
                }
            }
            else return 0.0;
        }
    }
}
