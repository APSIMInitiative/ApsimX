using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Adds the value of all childern functions to the previous days accumulation between start and end phases")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateFunction : Model, IFunction
    {
        //Class members
        /// <summary>The accumulated value</summary>
        private double AccumulatedValue = 0;
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The start stage name</summary>
        [Description("StartStageName")]
        public string StartStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("EndStageName")]
        public string EndStageName { get; set; }
    
        /// <summary>The fraction removed on cut</summary>
        private double FractionRemovedOnCut = 0; //FIXME: This should be passed from teh manager when "cut event" is called. Must be made general to other events.

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));

            if (Phenology.Between(StartStageName, EndStageName))
            {
                double DailyIncrement = 0.0;
                foreach (IFunction F in ChildFunctions)
                {
                    DailyIncrement = DailyIncrement + F.Value;
                }
                AccumulatedValue += DailyIncrement;
            }

        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                return AccumulatedValue;
            }
        }

        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCut(object sender, EventArgs e)
        {
            AccumulatedValue -= FractionRemovedOnCut * AccumulatedValue;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
        }

    }
}
