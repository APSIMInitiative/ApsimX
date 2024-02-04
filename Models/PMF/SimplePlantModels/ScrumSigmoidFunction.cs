using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// This function is calcualted using a sigmoid function of the form y = Xmax * 1 / 1 + e^-(Xvalue - Xo) / b^.
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Ymax * 1/1+exp(-(XValue-Xo)/b)")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumSigmoidFunction : Model, IFunction, IIndexedFunction
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

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        public Plant scrum = null;

        [Link] private SigmoidFunction sigmoid = null;

        private double tt_EmertToMat { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Error with values to Sigmoid function</exception>
        public double Value(int arrayIndex = -1)
        {
            return ValueIndexed(XValue.Value(arrayIndex));
        }

        /// <summary>Called when scrum is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("SCRUMTotalNDemand")]
        private void OnSCRUMTotalNDemand(object sender, ScrumFertDemandData data)
        {
            tt_EmertToMat = data.Tt_EmergtoMat;
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        public double ValueIndexed(double dX)
        {
            double ymax = Ymax.Value(-1) * 1/1.0096; //This corrects for the sigmoid already being @ 2% at emergence and only getting to 99.95% at matruity.  Sort of.
            double ttAdjust = tt_EmertToMat * 0.012170216462554536; //TtEmerge is zero but the sigmoid is already ~ 1% of the way along its x axis so we add exta tt to account for that 
            try
            {
                return ymax * sigmoid.Function(dX+ttAdjust, Xo.Value(-1), b.Value(-1));
            }
            catch (Exception)
            {
                throw new Exception("Error with values to Sigmoid function");
            }
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
