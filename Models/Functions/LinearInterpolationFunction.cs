using System;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// A linear interpolation model, where an
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A Y value is returned for the current vaule of the XValue child via linear interpolation of the XY pairs specified")]
    public class LinearInterpolationFunction : Model, IFunction
    {
        /// <summary>Gets the xy pairs.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;

        /// <summary>The x value to use for interpolation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction XValue = null;

        /// <summary>Constructor</summary>
        public LinearInterpolationFunction() { }

        /// <summary>Constructor</summary>
        public LinearInterpolationFunction(double[] x, double[] y)
        {
            XYPairs = new XYPairs() { X = x, Y = y };
        }

        /// <summary>Return the name of the x variable. Used as graph x axis title.</summary>
        public string XVariableName
        {
            get
            {
                if (XValue == null)
                    return string.Empty;
                else if (XValue is VariableReference)
                    return (XValue as VariableReference).VariableName;
                else
                    return "XValue";
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="xProperty">name of the x property</param>
        /// <param name="x">x values.</param>
        /// <param name="y">y values.</param>
        public LinearInterpolationFunction(string xProperty, double[] x, double[] y)
        {
            XYPairs = new XYPairs();
            XYPairs.X = x;
            XYPairs.Y = y;
            VariableReference XValue = new VariableReference();
            XValue.VariableName = xProperty;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot find value for  + Name +  XProperty:  + XProperty</exception>
        public double Value(int arrayIndex = -1)
        {
            if (XYPairs == null)
                throw new Exception($"LinearInterp Function at {this.FullPath} does not have an XYPairs linked to it");
                
            return XYPairs.ValueIndexed(XValue.Value(arrayIndex));
        }

        /// <summary>Values for x.</summary>
        /// <param name="XValue">The x value.</param>
        /// <returns></returns>
        public double ValueForX(double XValue)
        {
            if (XYPairs == null)
                throw new Exception($"LinearInterp Function at {this.FullPath} does not have an XYPairs linked to it");

            return XYPairs.ValueIndexed(XValue);
        }
    }
}
