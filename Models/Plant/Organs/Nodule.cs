using System;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Interfaces;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// This organ simulates the N fixation supply, and respiration cost, of N fixing nodules.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Nodule : GenericOrgan
    {
        #region Paramater Input Classes
       
        /// <summary>The fixation metabolic cost</summary>
        [Link]
        IFunction FixationMetabolicCost = null;
        /// <summary>The specific nitrogenase activity</summary>
        [Link]
        IFunction FixationRate = null;
        #endregion

        #region Class Fields
        /// <summary>The respired wt</summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double RespiredWt { get; set; }
        /// <summary>Gets the n fixed.</summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double NFixed { get; set; }
        
        #endregion

        #region Arbitrator methods
        /// <summary>Gets or sets the n fixation cost.</summary>
        public override double NFixationCost { get { return FixationMetabolicCost.Value; } }

        /// <summary>Sets the n allocation.</summary>
        [XmlIgnore]
        public override BiomassAllocationType NAllocation
        {
            set
            {
                base.NAllocation = value;    // give N allocation to base first.
                NFixed = value.Fixation;    // now get our fixation value.
            }
        }

        /// <summary>Gets the respired wt fixation.</summary>
        public double RespiredWtFixation { get { return RespiredWt; } }

        /// <summary>Gets or sets the n supply.</summary>
        public override BiomassSupplyType NSupply
        {
            get
            {
                BiomassSupplyType Supply = base.NSupply;   // get our base GenericOrgan to fill a supply structure first.
                Supply.Fixation = FixationRate.Value;
                return Supply;
            }
        }
        /// <summary>Sets the dm allocation.</summary>
        public override BiomassAllocationType DMAllocation
        {
            set
            //This is the DM that is consumed to fix N.  this is calculated by the arbitrator and passed to the nodule to report
            {
                base.DMAllocation = value;      // Give the allocation to our base GenericOrgan first
                RespiredWt = value.Respired;    // Now get the respired value for ourselves.
            }
        }

        #endregion

        /// <summary>Event from sequencer telling us to do phenology events.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        protected void OnStartOfDay(object sender, EventArgs e)
        {
            NFixed = 0;
            RespiredWt = 0;
        }
    }
}
