using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Struct;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF
{
    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    public class Tillering : Model
    {
        ///// <summary>The met data</summary>
        //[Link]
        //private IWeather metData = null;

        /// <summary>The parent plant</summary>
        [Link]
        private Plant parentPlant = null;

        ///// <summary>The parent plant</summary>
        //[Link]
        //private Clock clock = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public ICulmLeafArea AreaCalc = null;

        /// <summary>Logic for managing Culms</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private ITilleringMethod tilleringMethod = null;

        /// <summary>Expansion Stress Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction expansionStress = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senescenceRate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNumSeed = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafInitRate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction maxLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction ttEmergToFI = null;

        /// <summary> Subsequent tillers are slightly smaller - adjust that size using a percentage</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction VerticalTillerAdjustment = null;

        /// <summary> Maximum values that Subsequent tillers can be adjusted</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaxVerticalTillerAdjustment = null;

        /// <summary> LeafAppearance Rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public LinearInterpolationFunction LeafAppearanceRate = null;

        /// <summary>Leaf Area Index</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>LAI of Dead leaf</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double LAIDead { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Delta of Leaf Area Index</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double DltLAI { get; set; }

        /// <summary>Delta of Leaf Area Index before stress and limitations are applied</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double DltPotentialLAI { get; set; }

        /// <summary>Delta of Leaf Area Index</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double DltStressedLAI { get; set; }

        /// <summary>Delta of Leaf Area Index</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double DltRetranslocated { get; set; }

        /// <summary>Delta of Leaf Area Index</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double DltSenesced { get; set; } //total or separate retranslocation

        /// <summary>Current Leaf Number on the Main Stem</summary>
        [JsonIgnore]
        public double CurrentLeafNo { get; set; } //total or separate retranslocation

        /// <summary>Current Leaf Number on the Main Stem</summary>
        [JsonIgnore]
        public double FinalLeafNo { get; set; } //total or separate retranslocation

        /// <summary> List of Culms - first is the main stem</summary>
        public List<Culm> Culms;

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            Culms = new List<Culm> {new Culm(0,new CulmParams { }) };
            CurrentLeafNo = 0.0;
            LAI = 0.0;
            LAIDead = 0.0;
            DltPotentialLAI = 0.0;
            DltStressedLAI = 0.0;
            DltRetranslocated = 0.0;
            DltSenesced = 0.0;
        }

        /// <summary>
        /// Calculate final leaf number.
        /// </summary>
        private double calcFinalLeafNo()
        {
            double initRate = leafInitRate.Value();
            double noSeed = leafNumSeed.Value();
            double minLeaf = minLeafNo.Value();
            double maxLeaf = maxLeafNo.Value();
            double ttFi = ttEmergToFI.Value();

            return MathUtilities.Bound(MathUtilities.Divide(ttFi, initRate, 0) + noSeed, minLeaf, maxLeaf);
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (parentPlant.IsAlive)
            {
                FinalLeafNo = calcFinalLeafNo();
                Culms.ForEach(c => c.FinalLeafNo = FinalLeafNo);
                
                tilleringMethod.UpdateLeafNumber();
                CurrentLeafNo = Culms[0].CurrentLeafNo;
                tilleringMethod.UpdateTillerNumber();
                
                DltPotentialLAI = tilleringMethod.CalculatePotentialLeafArea();
                DltLAI = DltPotentialLAI * expansionStress.Value();
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (parentPlant.IsAlive)
            {
                //DltLAI = DltStressedLAI;
                DltSenesced = senescenceRate.Value();
                tilleringMethod.CalculateActualLeafArea();

                LAI += DltLAI - DltSenesced - DltRetranslocated;
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == parentPlant)
            {
                Clear();

                //clock.DoPotentialPlantGrowth += OnDoPotentialPlantGrowth;
                //clock.DoActualPlantGrowth += OnDoActualPlantGrowth;
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
            //clock.DoPotentialPlantGrowth -= OnDoPotentialPlantGrowth;
            //clock.DoActualPlantGrowth -= OnDoActualPlantGrowth;

        }

    }
}
