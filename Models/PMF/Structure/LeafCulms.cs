using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Newtonsoft.Json;

namespace Models.PMF.Struct
{
    /// <summary>
    /// The LeafCulms model manages the canopy resources produced by tillering. Two main tillering strategies are provided by default, and are managed via 
    /// the TilleringMethod switch defined in SorghumLeaf, which can be manipulated via script methods. 
    /// FixedTillering will use the FTN property provided as part of the sowing method to determine the total number of fertile tillers.
    /// Setting FTN to a negative value will calculate the number of fixed tillers using latitude and sowing density to provide a rule of thumb value. 
    /// These values have been derived using data from the Australian sorghum growing area, and may not be suitable for other locations.
    /// DynamicTillering will calculate the potential number of tillers - usually determined by the time the 6th leaf has appeared. 
    /// The number of fertile tillers is then maintained by the addition or removal of active tillers. Further information provided below for each method.
    /// 
    /// </summary>	
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LeafCulms : Model
    {
        /// <summary>The parent Plant</summary>
        [Link]
        Plant plant = null;

        /// <summary> Tillering Method that uses a fixed number of tillers</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private ITilleringMethod fixedTillering = null;

        /// <summary> Tillering Method that manages number of tillers dynamically</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private ITilleringMethod dynamicTillering = null;

        /// <summary> Expansion stress. </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction expansionStress = null;

        /// <summary> Appearance rate changes when this many leaves are remaining</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction noRateChange1 = null;

        /// <summary> Appearance rate can change again when this many leaves are remaining</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction noRateChange2 = null;

        /// <summary> The Initial Appearance rate for phyllocron.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction appearanceRate1 = null;

        /// <summary>The Appearance rate for phyllocron after noRateChange 1 .</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction appearanceRate2 = null;

        /// <summary>The Appearance rate for phyllocron after noRateChange 2 .</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction appearanceRate3 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoAtEmergence = null;

        /// <summary> Set through Sowing Event</summary>
        [JsonIgnore]
        public int TilleringMethod { get; set; }

        private ITilleringMethod tillering => TilleringMethod == 0 ? fixedTillering : dynamicTillering;

        /// <summary> FertileTillerNumber is determined by the tillering method chosen</summary>
		[JsonIgnore]
        public double FertileTillerNumber
        {
            get => tillering.FertileTillerNumber;
            set
            {
                //the preferred method for setting FertileTillerNumber is during the sowing event
                //this is here to enable access by external processes immediately following sowing
                fixedTillering.FertileTillerNumber = value;
            }
        }

        /// <summary> CurrentTillerNumber is determined by the tillering method chosen</summary>
		[JsonIgnore]
        public double CurrentTillerNumber { get => tillering.CurrentTillerNumber; }

        /// <summary> Subsequent tillers are slightly smaller - adjust that size using a percentage</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction VerticalTillerAdjustment = null;

        /// <summary> Maximum values that Subsequent tillers can be adjusted</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaxVerticalTillerAdjustment = null;

        /// <summary>Final leaf number.</summary>
        [JsonIgnore]
        public double FinalLeafNo { get; set; }

        /// <summary>Leaf number.</summary>
        [JsonIgnore]
        public double LeafNo { get { return Culms[0].CurrentLeafNo; } }

        /// <summary> Amount of Leaf that appears today</summary>
        [JsonIgnore]
        public double dltLeafNo;

        /// <summary> Potential leaf growth for today for all culms</summary>
        [JsonIgnore]
        public double dltPotentialLAI { get; set; }

        /// <summary> Potential leaf growth after stress for today for all culms</summary>
        [JsonIgnore]
        public double dltStressedLAI { get; set; }

        /// <summary> Collection of Culms </summary>
        [JsonIgnore]
        public List<Culm> Culms;

        /// <summary>Total TT required to get from emergence to floral init.</summary>
        [JsonIgnore]
        public double TTTargetFI { get; private set; }

        /// <summary> Constructor. </summary>
        public LeafCulms()
        {
            Culms = new List<Culm>();
        }

        /// <summary> Array of Individual leaf sizeson the first culm</summary>
        [JsonIgnore]
        public double[] LeafSizes
        {
            get
            {
                return Culms[0].LeafSizes.ToArray();
            }
        }

        /// <summary>
        /// Remove all then add the first culm (which is the main culm).
        /// Shouldn't be called once sown.
        /// </summary>
        public void Initialize()
        {
            Culms.Clear();
            Culms.Add(new Culm(0));
            Culms[0].CurrentLeafNo = leafNoAtEmergence.Value();

            TTTargetFI = 0;
            FinalLeafNo = 0;
            dltLeafNo = 0;
            dltPotentialLAI = 0.0;
            dltStressedLAI = 0.0;
        }

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
            Initialize();
        }

        /// <summary>Calculate Potential Leaf Area</summary>
        public void CalculatePotentialArea()
        {
            //FinalLeafNo = numberOfLeaves.Value();
            Culms.ForEach(c => c.FinalLeafNo = FinalLeafNo);

            dltLeafNo = tillering.CalcLeafNumber();

            dltPotentialLAI = tillering.CalcPotentialLeafArea();
            double expStress = expansionStress.Value();
            dltStressedLAI = dltPotentialLAI * expStress;
            Culms.ForEach(c => c.DltStressedLAI = c.DltLAI * expStress);
        }

        /// <summary>Calculate Actual Area - adjusts potential growth </summary>
        public double CalculateActualArea()
        {
            double actualLAI = tillering.CalcActualLeafArea(dltStressedLAI);

            Culms.ForEach(c => c.TotalLAI = c.TotalLAI + c.DltStressedLAI);
            return actualLAI;
        }

        /// <summary>Calculate Actual Area - adjusts potential growth </summary>
        public double getLeafAppearanceRate(double remainingLeaves)
        {
            //allowing for 2 rate changes although current crops only utilise 1
            if (remainingLeaves <= noRateChange2.Value())
                return appearanceRate3.Value();
            if (remainingLeaves <= noRateChange1.Value())
                return appearanceRate2.Value();
            return appearanceRate1.Value();
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == plant)
            {
                //sets which tillering method to reference via tillering
                TilleringMethod = data.TilleringMethod;
            }
        }

    }
}
