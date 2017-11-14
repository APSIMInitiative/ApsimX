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
        public override double NFixationCost { get { return FixationMetabolicCost.Value(); } }

        /// <summary>Sets the n allocation.</summary>
        public override void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            base.SetNitrogenAllocation(nitrogen);    // give N allocation to base first.
            NFixed = nitrogen.Fixation;    // now get our fixation value.
        }

        /// <summary>Gets the respired wt fixation.</summary>
        public double RespiredWtFixation { get { return RespiredWt; } }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        public override BiomassSupplyType CalculateNitrogenSupply()
        {
            base.CalculateNitrogenSupply();   // get our base GenericOrgan to fill a supply structure first.
            NSupply.Fixation = FixationRate.Value();
            return NSupply; 
        }

        /// <summary>Sets the dry matter allocation.</summary>
        public override void SetDryMatterAllocation(BiomassAllocationType value)
        {
            //This is the DM that is consumed to fix N.  this is calculated by the arbitrator and passed to the nodule to report
            base.SetDryMatterAllocation(value);      // Give the allocation to our base GenericOrgan first
            RespiredWt = value.Respired;    // Now get the respired value for ourselves.
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
