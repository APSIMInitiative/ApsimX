using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// This function is calcualted using a sigmoid function of the form y = Xmax * 1 / 1 + e^-(Xvalue - Xo) / b^.
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Ymax * 1/1+exp(-(XValue-Xo)/b)")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SigmoidFunction : Model, IFunction, IIndexedFunction
    {
        /// <summary>The ymax</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Ymax = null;
        /// <summary>The x value</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        IFunction XValue = null;
        /// <summary>The Xo</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Xo = null;
        /// <summary>The b</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction b = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Error with values to Sigmoid function</exception>
        public double Value(int arrayIndex = -1)
        {
            return ValueIndexed(XValue.Value(arrayIndex));
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            try
            {
                return Ymax.Value(-1) * Function(dX, Xo.Value(-1), b.Value(-1));
            }
            catch (Exception)
            {
                throw new Exception("Error with values to Sigmoid function");
            }
        }


        /// <summary>
        /// General sigmoid function
        /// </summary>
        /// <param name="dX"></param>
        /// <param name="Xo"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public double Function(double dX, double Xo, double b)
        {
            return 1 / (1 + Math.Exp(-(dX - Xo) / b));
        }
        
            /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"{Name} is calcualted using a sigmoid function of the form y = Ymax * 1 / 1 + e^-(Xvalue - Xo) / b^.");

            // Document Ymax.
            yield return new Paragraph($"{nameof(Ymax)} is calculated as:");
            foreach (ITag tag in Ymax.Document())
                yield return tag;

            // Document x0.
            yield return new Paragraph($"{nameof(Xo)} is calculated as:");
            foreach (ITag tag in Xo.Document())
                yield return tag;

            // Document b.
            yield return new Paragraph($"{nameof(b)} is calculated as:");
            foreach (ITag tag in b.Document())
                yield return tag;

            // Document x value.
            yield return new Paragraph($"{nameof(XValue)} is calculated as:");
            foreach (ITag tag in XValue.Document())
                yield return tag;
        }
    }
}
