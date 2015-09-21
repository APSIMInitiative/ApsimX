using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Interfaces;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Nodule organ
    /// Has three options for N fixation methods set by the NFixation property
    /// 1. None means the nodule will fix no N 
    /// 2. Majic means the nodule will fix as much N as the plant demands at no DM cost
    /// 3. FullCost requires parameters additional parameters of FixationMetabolicCost, SpecificNitrogenaseActivity
    ///    FT, FW, FWlog and a DMDemandFunction.  This calculates N fixation supply from the wt of the nodules
    ///    and these parameters at a cost specified by the FixationMetabolicCost parameter
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Nodule : GenericOrgan, BelowGround
    {
        #region Paramater Input Classes
        /// <summary>The method used to determine N fixation</summary>
        [Description("NFixationOption can be FullCost, Majic or None")]
        public string NFixationOption { get; set; }
        
        /// <summary>The fixation metabolic cost</summary>
        [Link(IsOptional = true)]
        IFunction FixationMetabolicCost = null;
        /// <summary>The specific nitrogenase activity</summary>
        [Link(IsOptional = true)]
        IFunction SpecificNitrogenaseActivity = null;
        /// <summary>The ft</summary>
        [Link(IsOptional = true)]
        IFunction FT = null;
        /// <summary>The fw</summary>
        [Link(IsOptional = true)]
        IFunction FW = null;
        /// <summary>The f wlog</summary>
        [Link(IsOptional = true)]
        IFunction FWlog = null;
        #endregion

        #region Class Fields
        /// <summary>The respired wt</summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double RespiredWt { get; set; }
        /// <summary>The property fixation demand</summary>
        [Units("0-1")]
        [XmlIgnore]
        public double PropFixationDemand { get; set; }
        /// /// <summary>Gets the n fixed.</summary>
        /// <value>The n fixed.</value>
        [Units("g/m2")]
        [XmlIgnore]
        public double NFixed { get; set; }
        
        #endregion

        #region Arbitrator methods
        /// <summary>Gets or sets the n fixation cost.</summary>
        /// <value>The n fixation cost.</value>
        public override double NFixationCost
        {
            get
            {
                if ((NFixationOption == "Majic")||(NFixationOption == "None"))
                    return 0;
                else
                return FixationMetabolicCost.Value;
            }
        }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        public override BiomassAllocationType NAllocation
        {
            set
            {
                base.NAllocation = value;    // give N allocation to base first.
                NFixed = value.Fixation;    // now get our fixation value.
            }
        }

        /// <summary>Gets the respired wt fixation.</summary>
        /// <value>The respired wt fixation.</value>
        public double RespiredWtFixation
        {
            get
            {
                return RespiredWt;
            }
        }
        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        public override BiomassSupplyType NSupply
        {
            get
            {
                BiomassSupplyType Supply = base.NSupply;   // get our base GenericOrgan to fill a supply structure first.
                if (NFixationOption == "Majic")
                    Supply.Fixation = 10000; //the plant can fix all the N it will ever need
                else if (NFixationOption == "None")
                    Supply.Fixation = 0; //the plant will fix no N
                else if (Live != null)
                {
                    // Now add in our fixation calculated mechanisticaly
                    Supply.Fixation = Live.StructuralWt * SpecificNitrogenaseActivity.Value * Math.Min(FT.Value, Math.Min(FW.Value, FWlog.Value));
                }
                return Supply;
            }
        }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
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
