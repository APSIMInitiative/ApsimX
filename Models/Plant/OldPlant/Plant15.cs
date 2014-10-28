using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Functions;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System.Xml.Serialization;
using System.IO;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class HarvestType
    {
        /// <summary>The plants</summary>
        public Double Plants;
        /// <summary>The remove</summary>
        public Double Remove;
        /// <summary>The height</summary>
        public Double Height;
        /// <summary>The report</summary>
        public String Report = "";
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class RemovedByAnimalType
    {
        /// <summary>The element</summary>
        public RemovedByAnimalelementType[] element;
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class RemovedByAnimalelementType
    {
        /// <summary>The cohort identifier</summary>
        public String CohortID = "";
        /// <summary>The organ</summary>
        public String Organ = "";
        /// <summary>The age identifier</summary>
        public String AgeID = "";
        /// <summary>The bottom</summary>
        public Double Bottom;
        /// <summary>The top</summary>
        public Double Top;
        /// <summary>The chem</summary>
        public String Chem = "";
        /// <summary>The weight removed</summary>
        public Double WeightRemoved;
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class AvailableToAnimalelementType
    {
        /// <summary>The cohort identifier</summary>
        public String CohortID = "";
        /// <summary>The organ</summary>
        public String Organ = "";
        /// <summary>The age identifier</summary>
        public String AgeID = "";
        /// <summary>The bottom</summary>
        public Double Bottom;
        /// <summary>The top</summary>
        public Double Top;
        /// <summary>The chem</summary>
        public String Chem = "";
        /// <summary>The weight</summary>
        public Double Weight;
        /// <summary>The n</summary>
        public Double N;
        /// <summary>The p</summary>
        public Double P;
        /// <summary>The s</summary>
        public Double S;
        /// <summary>The ash alk</summary>
        public Double AshAlk;
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class AvailableToAnimalType
    {
        /// <summary>The element</summary>
        public AvailableToAnimalelementType[] element;
    }


    /// <summary>
    /// The generic plant15 crop model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Plant15 : ModelCollectionFromResource, ICrop
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Gets or sets a value indicating whether [automatic harvest].
        /// </summary>
        /// <value><c>true</c> if [automatic harvest]; otherwise, <c>false</c>.</value>
        public bool AutoHarvest { get; set; }

        /// <summary>The sowing data</summary>
        [XmlIgnore]
        public SowPlant2Type SowingData;

        /// <summary>
        /// MicroClimate will get 'CropType' and use it to look up
        /// canopy properties for this crop.
        /// </summary>
        public string CropType { get; set; }


        /// <summary>The cultivar definition</summary>
        private Cultivar cultivarDefinition;

        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames
        {
            get
            {
                SortedSet<string> cultivarNames = new SortedSet<string>();
                foreach (Cultivar cultivar in this.Cultivars)
                {
                    cultivarNames.Add(cultivar.Name);
                    if (cultivar.Aliases != null)
                    {
                        foreach (string alias in cultivar.Aliases)
                            cultivarNames.Add(alias);
                    }
                }

                return new List<string>(cultivarNames).ToArray();
            }
        }

        #region Event handlers and publishers

        /// <summary>Occurs when [new crop].</summary>
        public event NewCropDelegate NewCrop;

        /// <summary>Occurs when [sowing].</summary>
        public event EventHandler Sowing;

        /// <summary>Occurs when [biomass removed].</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>Called when [loaded].</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded()
        {
            // Find organs
            Organ1s = new List<Organ1>();
            foreach (Model model in this.Children)
            {
                if (model is Organ1)
                    Organ1s.Add(model as Organ1);
            }

            EOCropFactor = 1.5;  // wheat
            NSupplyPreference = "active";
            DoRetranslocationBeforeNDemand = false;

        }

        /// <summary>Sows the specified population.</summary>
        /// <param name="population">The population.</param>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="budNumber">Bud number not used</param>
        /// <param name="maxCover">Maximum cover not used</param>
        /// <exception cref="System.Exception">Cultivar not specified on sow line.</exception>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
        {
            SowingData = new SowPlant2Type();
            SowingData.Population = population;
            SowingData.Cultivar = cultivar;
            SowingData.Depth = depth;
            SowingData.RowSpacing = rowSpacing;

            if (SowingData.Cultivar == "")
                throw new Exception("Cultivar not specified on sow line.");

            // Find cultivar and apply cultivar overrides.
            cultivarDefinition = Cultivar.Find(Cultivars, SowingData.Cultivar);
            cultivarDefinition.Apply(this);

            if (NewCrop != null)
            {
                NewCropType Crop = new NewCropType();
                Crop.crop_type = CropType;
                Crop.sender = Name;
                NewCrop.Invoke(Crop);
            }

            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            Population.OnSow(SowingData);
            Phenology.OnSow();
            WriteSowReport(SowingData);
            OnPrepare(null, null); // Call this because otherwise it won't get called on the sow date.
        }


        #endregion


        #region Plant1 functionality

        /// <summary>The n stress</summary>
        [Link]
        NStress NStress = null;

        /// <summary>The radiation partitioning</summary>
        [Link]
        RadiationPartitioning RadiationPartitioning = null;

        /// <summary>The n fix rate</summary>
        [Link]
        Function NFixRate = null;

        /// <summary>The above ground live</summary>
        [Link]
        CompositeBiomass AboveGroundLive = null;

        /// <summary>The above ground dead</summary>
        [Link]
        CompositeBiomass AboveGroundDead = null;

        /// <summary>The above ground</summary>
        [Link]
        CompositeBiomass AboveGround = null;

        /// <summary>The vegetative live</summary>
        [Link]
        CompositeBiomass VegetativeLive = null;
        
        /// <summary>The vegetative dead</summary>
        [Link]
        CompositeBiomass VegetativeDead = null;

        /// <summary>The vegetative biomass pool</summary>
        [Link]
        CompositeBiomass Vegetative = null;

        /// <summary>The below ground</summary>
        [Link]
        CompositeBiomass BelowGround = null;

        /// <summary>The total live</summary>
        [Link]
        public CompositeBiomass TotalLive = null;

        /// <summary>The sw stress</summary>
        [Link]
        SWStress SWStress = null;

        /// <summary>The temporary stress</summary>
        [Link]
        Function TempStress = null;

        /// <summary>The root</summary>
        [Link]
        Root1 Root = null;

        /// <summary>The stem</summary>
        [Link]
        Stem1 Stem = null;

        /// <summary>The leaf</summary>
        [Link]
        Leaf1 Leaf = null;

        /// <summary>The pod</summary>
        [Link]
        Pod Pod = null;

        /// <summary>The grain</summary>
        [Link]
        Grain Grain = null;

        /// <summary>The population</summary>
        [Link]
        Population1 Population = null;

        /// <summary>The plant spatial</summary>
        [Link]
        PlantSpatial1 PlantSpatial = null;

        /// <summary>The arbitrator1</summary>
        [Link]
        GenericArbitratorXY Arbitrator1 = null;

        /// <summary>Gets or sets the eo crop factor.</summary>
        /// <value>The eo crop factor.</value>
        [XmlIgnore]
        public double EOCropFactor { get; set; }

        /// <summary>Gets or sets the n supply preference.</summary>
        /// <value>The n supply preference.</value>
        [XmlIgnore]
        public string NSupplyPreference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [do retranslocation before n demand].
        /// </summary>
        /// <value>
        /// <c>true</c> if [do retranslocation before n demand]; otherwise, <c>false</c>.
        /// </value>
        [XmlIgnore]
        public bool DoRetranslocationBeforeNDemand { get; set; }

        //[Input]
        //double EO = 0;
        /// <summary>The soil</summary>
        [Link]
        Soils.Soil Soil = null;

        //[Input]
        //public DateTime Today; // for debugging.
        /// <summary>The clock</summary>
        [Link]
        Clock Clock = null;

        /// <summary>Occurs when [new canopy].</summary>
        public event NewCanopyDelegate NewCanopy;


        /// <summary>Occurs when [crop ending].</summary>
        public event NewCropDelegate CropEnding;

        /// <summary>Occurs when [harvesting].</summary>
        public event EventHandler Harvesting;

        /// <summary>Gets the cover_green.</summary>
        /// <value>The cover_green.</value>
        public double cover_green 
        { 
            get 
            {
                double green_cover = 0;
                foreach (Organ1 Organ in Organ1s)
                    green_cover += Organ.CoverGreen; //Fix me maybe.  Neil thinks this looks wrong 
                return green_cover;
            }
        }

        /// <summary>Gets the cover_tot.</summary>
        /// <value>The cover_tot.</value>
        public double cover_tot
        {
            get
            {
                double cover_sen = 0;
                foreach (Organ1 Organ in Organ1s)
                    cover_sen += Organ.CoverSen;
                return cover_green + cover_sen;
            }
        }

        /// <summary>Gets the height.</summary>
        /// <value>The height.</value>
        public double height
        { get { return Stem.Height; } }

        /// <summary>Gets the plant_status.</summary>
        /// <value>The plant_status.</value>
        public string plant_status
        {
            get
            {
                // What should be returned here?
                // The old "plant" component returned either "out", "alive"
                // How to determine "dead"?
                if (SowingData == null)
                    return "out";
                else
                    return "alive";
            }
        }

        // Used by SWIM

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [Units("mm")]
        public double WaterDemand
        {
            get
            {
                double Demand = 0;
                foreach (Organ1 Organ in Organ1s)
                    Demand += Organ.SWDemand;
                return Demand;
            }
        }


        /// <summary>Gets the ep.</summary>
        /// <value>The ep.</value>
        [Units("mm")]
        public double EP
        {
            get
            {
                return Root.SWUptake;
            }
        }

        /// <summary>MicroClimate supplies PotentialEP</summary>
        [XmlIgnore]
        public double PotentialEP { get; set; }

        /// <summary>MicroClimate supplies LightProfile</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>Gets the biomass.</summary>
        /// <value>The biomass.</value>
        [Units("kg/ha")]
        public double Biomass { get { return AboveGround.Wt * 10; } } // convert to kg/ha

        /// <summary>Gets or sets the organ1s.</summary>
        /// <value>The organ1s.</value>
        [XmlIgnore]
        public List<Organ1> Organ1s { get; set; }

        /// <summary>Gets the tops.</summary>
        /// <value>The tops.</value>
        [XmlIgnore]
        public List<Organ1> Tops
        {
            get
            {
                List<Organ1> tops = new List<Organ1>(); 
                foreach (Organ1 organ in this.Organ1s)
                {
                    if (organ is AboveGround)
                        tops.Add(organ);
                }
                return tops;
            }
        }
        /// <summary>The ext_n_demand</summary>
        private double ext_n_demand;
        /// <summary>The average stress message</summary>
        private string AverageStressMessage;
        /// <summary>The flowering date</summary>
        private DateTime FloweringDate;
        /// <summary>The maturity date</summary>
        private DateTime MaturityDate;
        /// <summary>The lai maximum</summary>
        private double LAIMax = 0;
        /// <summary>The phenology event today</summary>
        private bool PhenologyEventToday = false;

        /// <summary>Gets the tops sw demand.</summary>
        /// <value>The tops sw demand.</value>
        public double TopsSWDemand
        {
            get
            {
                double SWDemand = 0.0;
                foreach (Organ1 Organ in Tops)
                    SWDemand += Organ.SWDemand;
                return SWDemand;
            }
        }
        /// <summary>Gets the tops live wt.</summary>
        /// <value>The tops live wt.</value>
        private double TopsLiveWt
        {
            get
            {
                double SWDemand = 0.0;
                foreach (Organ1 Organ in Tops)
                    SWDemand += Organ.Live.Wt;
                return SWDemand;
            }
        }
        /// <summary>Gets the sw supply demand ratio.</summary>
        /// <value>The sw supply demand ratio.</value>
        public double SWSupplyDemandRatio
        {
            get
            {
                double Supply = 0;
                foreach (Organ1 Organ in Organ1s)
                    Supply += Organ.SWSupply;

                double Demand = TopsSWDemand;
                if (Demand > 0)
                    return Supply / Demand;
                else
                    return 1.0;
            }
        }

        /// <summary>
        /// Crop specific relative growth stress factor (0-1). MicroClimate
        /// uses this to calculate the crop canopy conductance
        /// </summary>
        public double FRGR
        {
            get
            {
                return Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                Math.Min(SWStress.OxygenDeficitPhoto, 1.0 /*PStress.Photo*/));  // FIXME
            }
        }

        /// <summary>Old PLANT1 compat. eventhandler. Not used in Plant2</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnPrepare(object sender, EventArgs e)
        {
            if (SowingData != null)
            {
                Util.Debug("\r\nPREPARE=%s", Clock.Today.ToString("d/M/yyyy"));
                Util.Debug("       =%i", Clock.Today.DayOfYear);

                foreach (Organ1 Organ in Organ1s)
                    Organ.OnPrepare(null, null);

                NStress.DoPlantNStress();
                RadiationPartitioning.DoRadiationPartition();
                foreach (Organ1 Organ in Organ1s)
                    Organ.DoPotentialRUE();

                // Calculate Plant Water Demand
                double SWDemandMaxFactor = EOCropFactor * Soil.SoilWater.Eo;
                foreach (Organ1 Organ in Organ1s)
                    Organ.DoSWDemand(SWDemandMaxFactor);

                DoNDemandEstimate();

                Util.Debug("NewPotentialGrowth.frgr=%f", FRGR);
                //Prepare_p();   // FIXME
            }
        }

        /// <summary>Old PLANT1 compat. process eventhandler.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            // Dean: This call to OnPrepare used to be called at start of day. 
            // No need to separate the call into separate event (I think)
            OnPrepare(null, null);

            if (SowingData != null)
            {
                Root.DoRootDepth();
                Util.Debug("\r\nPROCESS=%s", Clock.Today.ToString("d/M/yyyy"));
                Util.Debug("       =%i", Clock.Today.DayOfYear);
                foreach (Organ1 Organ in Organ1s)
                    Organ.DoSWUptake(TopsSWDemand);

                SWStress.DoPlantWaterStress(TopsSWDemand);
                foreach (Organ1 Organ in Organ1s)
                    Organ.DoNSupply();
                Phenology.DoTimeStep();
                Stem.Morphology();
                Leaf.DoCanopyExpansion();

                foreach (Organ1 Organ in Organ1s)   // NIH - WHY IS THIS HERE!!!!?????  Not needed I hope.
                    Organ.DoPotentialRUE();         // DPH - It does make a small difference!

                Arbitrator1.PartitionDM(Organ1s);
                Arbitrator1.RetranslocateDM(Organ1s);
                Leaf.Actual();
                Pod.CalcDltPodArea();
                Root.RootLengthGrowth();
                Leaf.LeafDeath();
                Leaf.LeafAreaSenescence();

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoSenescence();

                Root.DoSenescenceLength();
                Grain.DoNDemandGrain();

                //  g.n_fix_pot = _fixation->Potential(biomass, swStress->swDef.fixation);
                double n_fix_pot = 0;

                if (DoRetranslocationBeforeNDemand)
                    Arbitrator1.DoNRetranslocate(Grain.NDemand, Organ1s);

                bool IncludeRetranslocationInNDemand = !DoRetranslocationBeforeNDemand;
                foreach (Organ1 Organ in Organ1s)
                    Organ.DoNDemand(IncludeRetranslocationInNDemand);

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoNSenescence();
                Arbitrator1.doNSenescedRetranslocation(Organ1s);

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoSoilNDemand();
                // PotNFix = _fixation->NFixPot();
                double PotNFix = 0;
                Root.DoNUptake(PotNFix);

                double n_fix_uptake = Arbitrator1.DoNPartition(n_fix_pot, Organ1s);

                // DoPPartition();
                if (!DoRetranslocationBeforeNDemand)
                    Arbitrator1.DoNRetranslocate(Grain.NDemand2, Tops);

                // DoPRetranslocate();
                bool PlantIsDead = Population.PlantDeath();

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoDetachment();

                Update();

                CheckBounds();
                SWStress.DoPlantWaterStress(TopsSWDemand);
                NStress.DoPlantNStress();
                if (PhenologyEventToday)
                {
                    string message = Phenology.CurrentPhase.Start;
                    if (Phenology.CurrentPhase.Start != "Germination")
                    {
                        double biomass = 0;

                        double StoverWt = 0;
                        double StoverN = 0;
                        foreach (Organ1 Organ in Tops)
                        {
                            biomass += Organ.Live.Wt + Organ.Dead.Wt;
                            if (!(Organ is Reproductive))
                            {
                                StoverWt += Organ.Live.Wt + Organ.Dead.Wt;
                                StoverN += Organ.Live.N + Organ.Dead.N;
                            }
                        }
                        double StoverNConc = Utility.Math.Divide(StoverN, StoverWt, 0) * Conversions.fract2pcnt;
                        message += "\r\n";
                        message += string.Format("  biomass        = {0,8:F2} (g/m^2)\r\n" +
                                                 "  lai            = {1,8:F2} (m^2/m^2)\r\n" +
                                                 "  stover N conc  = {2,8:F2} (%)\r\n" +
                                                 "  extractable sw = {3,8:F2} (mm)",
                                           biomass, Leaf.LAI, StoverNConc, Root.ESWInRootZone);
                    }
                    
                    Summary.WriteMessage(this, message);
                    PhenologyEventToday = false;
                }
                //Root.UpdateWaterBalance();

                if (Phenology.InPhase("ReadyForHarvesting"))
                {
                    OnHarvest(new HarvestType());
                    OnEndCrop();
                    SowingData = null;
                }

                LAIMax = Math.Max(LAIMax, Leaf.LAI);
            }
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="Data">The data.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType Data)
        {
            if (SWStress != null && NStress != null)
            {
                PhenologyEventToday = true;
                AverageStressMessage += String.Format("{0,36}{1,13:F3}{2,13:F3}{3,13:F3}{4,13:F3}\r\n",
                                                      Data.OldPhaseName,
                                                      1 - SWStress.PhotoAverage, 1 - SWStress.ExpansionAverage,
                                                      1 - NStress.PhotoAverage, 1 - NStress.GrainAverage);
                SWStress.ResetAverage();
                NStress.ResetAverage();
            }

            if (Data.NewPhaseName.Contains("FloweringTo"))
                FloweringDate = Clock.Today;
            else if (Data.NewPhaseName.Contains("MaturityTo"))
                MaturityDate = Clock.Today;
        }

        /// <summary>Checks the bounds.</summary>
        private void CheckBounds()
        {
            //throw new NotImplementedException();
        }

        /// <summary>Updates this instance.</summary>
        private void Update()
        {
            foreach (Organ1 Organ in Organ1s)
                Organ.Update();

            // now update new canopy covers
            PlantSpatial.Density = Population.Density;
            PlantSpatial.CanopyWidth = Stem.Width;

            foreach (Organ1 Organ in Organ1s)
                Organ.DoCover();

            // Update the plant stress observers
            SWStress.Update();
            NStress.Update();
            Population.Update();

            UpdateCanopy();

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNConccentrationLimits();

            // PUBLISH BiomassRemoved event
            DoBiomassRemoved();
        }

        /// <summary>Provides canopy data to SoilWater.</summary>
        public NewCanopyType CanopyData { get { return LocalCanopyData; } }
        /// <summary>The local canopy data</summary>
        NewCanopyType LocalCanopyData = new NewCanopyType();

        /// <summary>Updates the canopy.</summary>
        private void UpdateCanopy()
        {
            // PUBLISH New_Canopy event
            double cover_green = 0;
            double cover_sen = 0;
            foreach (Organ1 Organ in Organ1s)
            {
                cover_green += Organ.CoverGreen; //Fix me maybe.  Neil thinks this looks wrong 
                cover_sen += Organ.CoverSen;
            }
            double cover_tot = (1.0 - (1.0 - cover_green) * (1.0 - cover_sen));
            LocalCanopyData.height = (float)Stem.Height;
            LocalCanopyData.depth = (float)Stem.Height;
            LocalCanopyData.lai = (float)Leaf.LAI;
            LocalCanopyData.lai_tot = (float)(Leaf.LAI + Leaf.SLAI);
            LocalCanopyData.cover = (float)cover_green;
            LocalCanopyData.cover_tot = (float)cover_tot;
            LocalCanopyData.sender = Name;

            NewCanopy.Invoke(LocalCanopyData);
            Util.Debug("NewCanopy.height=%f", LocalCanopyData.height);
            Util.Debug("NewCanopy.depth=%f", LocalCanopyData.depth);
            Util.Debug("NewCanopy.lai=%f", LocalCanopyData.lai);
            Util.Debug("NewCanopy.lai_tot=%f", LocalCanopyData.lai_tot);
            Util.Debug("NewCanopy.cover=%f", LocalCanopyData.cover);
            Util.Debug("NewCanopy.cover_tot=%f", LocalCanopyData.cover_tot);
        }

        /// <summary>Does the biomass removed.</summary>
        private void DoBiomassRemoved()
        {
            List<Biomass> Detaching = new List<Biomass>();
            List<string> OrganNames = new List<string>();
            foreach (Organ1 Organ in Organ1s)
            {
                if (Organ is AboveGround && !Organ.Detaching.IsEmpty)
                {
                    Detaching.Add(Organ.Detaching);
                    OrganNames.Add(Organ.Name);
                }
            }
            BiomassRemovedType chopped = new BiomassRemovedType();
            chopped.crop_type = CropType;
            chopped.dm_type = new string[Detaching.Count];
            chopped.dlt_crop_dm = new float[Detaching.Count];
            chopped.dlt_dm_n = new float[Detaching.Count];
            chopped.dlt_dm_p = new float[Detaching.Count];
            chopped.fraction_to_residue = new float[Detaching.Count];

            for (int i = 0; i < Detaching.Count; i++)
            {
                chopped.dm_type[i] = OrganNames[i];
                chopped.dlt_crop_dm[i] = (float)Detaching[i].Wt;
                chopped.dlt_dm_n[i] = (float)Detaching[i].N;
                //chopped.dlt_dm_p[i] = (float) Detaching[i].P;
                chopped.fraction_to_residue[i] = 1.0f;
            }
            BiomassRemoved.Invoke(chopped);
        }


        /// <summary>
        /// Calculate an approximate nitrogen demand for today's growth.
        /// The estimate basically = n to fill the plant up to maximum
        /// nitrogen concentration.
        /// </summary>
        /// <exception cref="System.Exception">bad n supply preference</exception>
        void DoNDemandEstimate()
        {
            // Assume that the distribution of plant
            // C will be similar after today and so N demand is that
            // required to raise all plant parts to max N conc.

            double dltDmPotRue = 0;
            foreach (Organ1 Organ in Organ1s)
                dltDmPotRue += Organ.dltDmPotRue;

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNDemand1Pot(dltDmPotRue);

            ext_n_demand = 0;
            foreach (Organ1 Organ in Organ1s)
                ext_n_demand += Organ.NDemand;

            //nh  use zero growth value here so that estimated n fix is always <= actual;
            double n_fix_pot = NFixRate.Value * AboveGroundLive.Wt * SWStress.Fixation;

            if (NSupplyPreference == "active")
            {
                // Nothing extra to do here
            }
            else if (NSupplyPreference == "fixation")
            {
                // Remove potential fixation from demand term
                ext_n_demand = ext_n_demand - n_fix_pot;
                ext_n_demand = Utility.Math.Constrain(ext_n_demand, 0.0, Double.MaxValue);
            }
            else
            {
                throw new Exception("bad n supply preference");
            }
            Util.Debug("Plant.ext_n_demand=%f", ext_n_demand);
        }


        /// <summary>Event handler for the Harvest event - normally comes from Manager.</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <exception cref="System.Exception">
        /// Harvest remove fraction needs to be between 0 and 1
        /// or
        /// Harvest height needs to be between 0 and 1000
        /// </exception>
        public void OnHarvest(HarvestType Harvest)
        {
            WriteHarvestReport();

            // Tell the rest of the system we are about to harvest
            if (Harvesting != null)
                Harvesting.Invoke(this, new EventArgs());

            // Check some bounds
            if (Harvest.Remove < 0 || Harvest.Remove > 1.0)
                throw new Exception("Harvest remove fraction needs to be between 0 and 1");
            if (Harvest.Height < 0 || Harvest.Height > 1000.0)
                throw new Exception("Harvest height needs to be between 0 and 1000");

            // Set the population denisty if one was provided by user.
            if (Harvest.Plants != 0)
                Population.Density = Harvest.Plants;

            // Call each organ's OnHarvest. They fill a BiomassRemoved structure. We then publish a
            // BiomassRemoved event.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            foreach (Organ1 Organ in Organ1s)
                Organ.OnHarvest(Harvest, BiomassRemovedData);
            BiomassRemovedData.crop_type = CropType;
            BiomassRemoved.Invoke(BiomassRemovedData);
            WriteBiomassRemovedReport(BiomassRemovedData);

            // now update new canopy covers
            PlantSpatial.Density = Population.Density;
            PlantSpatial.CanopyWidth = Leaf.width;
            foreach (Organ1 Organ in Organ1s)
                Organ.DoCover();
            UpdateCanopy();

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNConccentrationLimits();

            foreach (Organ1 Organ in Organ1s)
                Organ.OnHarvest(Harvest, BiomassRemovedData);
        }

        /// <summary>Called when [end crop].</summary>
        public void OnEndCrop()
        {
            NewCropType Crop = new NewCropType();
            Crop.crop_type = CropType;
            Crop.sender = Name;
            if (CropEnding != null)
                CropEnding.Invoke(Crop);

            // Keep track of some variables for reporting.
            Biomass AboveGroundBiomass = new Biomass(AboveGround);
            Biomass BelowGroundBiomass = new Biomass(BelowGround);

            // Call each organ's OnHarvest. They fill a BiomassRemoved structure. We then publish a
            // BiomassRemoved event.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            foreach (Organ1 Organ in Organ1s)
                Organ.OnEndCrop(BiomassRemovedData);
            BiomassRemovedData.crop_type = CropType;
            BiomassRemoved.Invoke(BiomassRemovedData);

            string message = string.Format("Organic matter from crop to surface organic matter:-\r\n" +
                                           "  DM tops  (kg/ha) = {0,10:F1}\r\n" +
                                           "  DM roots (kg/ha) = {1,10:F1}\r\n" +
                                           "  N  tops  (kg/ha) = {0,10:F2}\r\n" +
                                           "  N  roots (lh/ha) = {1,10:F2}",
                     AboveGroundBiomass.Wt, BelowGroundBiomass.Wt, 
                     AboveGroundBiomass.N, BelowGroundBiomass.N);
            Summary.WriteMessage(this, message);

            cultivarDefinition.Unapply();
            SowingData = null;
        }

        /// <summary>Write a sowing report to summary file.</summary>
        /// <param name="Sow">The sow.</param>
        void WriteSowReport(SowPlant2Type Sow)
        {
            StringWriter summary = new StringWriter();
            summary.WriteLine("Crop sow");
            summary.WriteLine("   cultivar = " + Sow.Cultivar);
            Phenology.WriteSummary(summary);
            Grain.WriteCultivarInfo(summary);

            Root.WriteSummary(summary);

            summary.WriteLine(string.Format("Crop factor for bounding water use is set to {0,5:F1} times eo.", EOCropFactor));

            summary.WriteLine("");
            summary.WriteLine("             Crop Sowing Data");
            summary.WriteLine("------------------------------------------------");
            summary.WriteLine("Sowing  Depth Plants Spacing Skip  Skip  Cultivar");
            summary.WriteLine("Day no   mm     m^2     mm   row   plant name");
            summary.WriteLine("------------------------------------------------");

            summary.WriteLine(string.Format("{0,7:D}{1,7:F1}{2,7:F1}{3,7:F1}{4,6:F1}{5,6:F1} {6}", new object[] 
                            {Clock.Today.DayOfYear, 
                             Sow.Depth,
                             Sow.Population, 
                             Sow.RowSpacing,
                             Sow.SkipRow,
                             Sow.SkipPlant,
                             Sow.Cultivar}));
            Summary.WriteMessage(this, summary.ToString());
        }

        /// <summary>Write a biomass removed report.</summary>
        /// <param name="Data">The data.</param>
        void WriteBiomassRemovedReport(BiomassRemovedType Data)
        {
            double dm_residue = 0.0;
            double dm_root_residue = 0.0;
            double n_residue = 0.0;
            double n_root_residue = 0.0;
            double p_residue = 0.0;
            double p_root_residue = 0.0;
            double dm_chopped = 0.0;
            double dm_root_chopped = 0.0;
            double n_chopped = 0.0;
            double n_root_chopped = 0.0;
            double p_chopped = 0.0;
            double p_root_chopped = 0.0;

            for (int part = 0; part < Data.dm_type.Length; part++)
            {
                dm_chopped += Data.dlt_crop_dm[part];
                n_chopped += Data.dlt_dm_n[part];
                p_chopped += Data.dlt_dm_p[part];
                dm_residue += Data.dlt_crop_dm[part] * Data.fraction_to_residue[part];
                n_residue += Data.dlt_dm_n[part] * Data.fraction_to_residue[part];
                p_residue += Data.dlt_dm_p[part] * Data.fraction_to_residue[part];
                if (Data.dm_type[part] == "root")
                {
                    dm_root_residue += Data.dlt_crop_dm[part] * Data.fraction_to_residue[part];
                    n_root_residue += Data.dlt_dm_n[part] * Data.fraction_to_residue[part];
                    p_root_residue += Data.dlt_dm_p[part] * Data.fraction_to_residue[part];
                    dm_root_chopped += Data.dlt_crop_dm[part];
                    n_root_chopped += Data.dlt_dm_n[part];
                    p_root_chopped += Data.dlt_dm_p[part];
                }
            }

            double dm_tops_chopped = dm_chopped - dm_root_chopped;
            double n_tops_chopped = n_chopped - n_root_chopped;
            double p_tops_chopped = p_chopped - p_root_chopped;

            double dm_tops_residue = dm_residue - dm_root_residue;
            double n_tops_residue = n_residue - n_root_residue;
            double p_tops_residue = p_residue - p_root_residue;

            Summary.WriteMessage(this, "Crop harvested.");

            Summary.WriteMessage(this, "Organic matter from crop:-  Tops to surface residue      Roots to soil FOM");

            Summary.WriteMessage(this, string.Format("                  DM (kg/ha) = {0,21:F1}{1,24:F1}",
                                            dm_tops_residue, dm_root_residue));
            Summary.WriteMessage(this, string.Format("                  N  (kg/ha) = {0,22:F2}{1,24:F2}",
                                            n_tops_residue, n_root_residue));

            double dm_removed_tops = dm_tops_chopped - dm_tops_residue;
            double dm_removed_root = dm_root_chopped - dm_root_residue;
            double n_removed_tops = n_tops_chopped - n_tops_residue;
            double n_removed_root = n_root_chopped - n_root_residue;
            double p_removed_tops = p_tops_chopped - p_tops_residue;
            double p_removed_root = p_root_chopped - p_root_residue;

            Summary.WriteMessage(this, "    Organic matter removed from system:-      From Tops               From Roots");

            Summary.WriteMessage(this, string.Format("                  DM (kg/ha) = {0,21:F1}{1,24:F1}",
                              dm_removed_tops, dm_removed_root));
            Summary.WriteMessage(this, string.Format("                  N  (kg/ha) = {0,22:F2}{1,24:F2}",
                              n_removed_tops, n_removed_root));
        }

        /// <summary>Report the state of the crop at harvest time</summary>
        private void WriteHarvestReport()
        {
            const double  plant_c_frac = 0.4f;    // fraction of c in resiudes
            double grain_wt;                      // grain dry weight (g/kernel)
            double plant_grain_no;                // final grains /head
            double n_grain;                       // total grain N uptake (kg/ha)
            double n_green;                       // above ground green plant N (kg/ha)
            double n_stover;                      // nitrogen content of stover (kg\ha)
            double n_total;                       // total gross nitrogen content (kg/ha)
            double n_grain_conc_percent;          // grain nitrogen %
            double yield;                         // grain yield dry wt (kg/ha)

            // crop harvested. Report status
            yield = Grain.Yield;
            grain_wt = Grain.Wt;
            plant_grain_no = Utility.Math.Divide(Grain.GrainNo, Population.Density, 0.0);
            n_grain = (Grain.Live.N + Grain.Dead.N) * 10;  // g/m2 to kg/ha

            double dmRoot = (Root.Live.Wt + Root.Dead.Wt) * 10;
            double nRoot = (Root.Live.N + Root.Dead.N) * 10;

            n_grain_conc_percent = (Grain.Live.NConc + Grain.Dead.NConc);

            n_stover = Vegetative.N * 10;
            n_green = VegetativeLive.N * 10;
            n_total = n_grain + n_stover;

            double stoverTot = Vegetative.Wt;
            double DMRrootShootRatio = Utility.Math.Divide(dmRoot, AboveGround.Wt * 10, 0.0);
            double HarvestIndex = Utility.Math.Divide(yield, AboveGround.Wt * 10, 0.0);
            double StoverCNRatio     = Utility.Math.Divide(stoverTot * 10 *plant_c_frac, n_stover, 0.0);
            double RootCNRatio       = Utility.Math.Divide(dmRoot*plant_c_frac, nRoot, 0.0);

            Summary.WriteMessage(this, string.Empty);
            Summary.WriteMessage(this, string.Empty);
            Summary.WriteMessage(this, 
                                 string.Format("                    stover (kg/ha) = {0,7:F1}",
                                               stoverTot * 10));
            Summary.WriteMessage(this,
                                 string.Format("               grain yield (kg/ha) = {0,7:F1}",
                                               yield));
            Summary.WriteMessage(this,
                                 string.Format("                      grain wt (g) =  {0,9:F3}",
                                               grain_wt));
            Summary.WriteMessage(this,
                                 string.Format("                        grains/m^2 =  {0,7:F1}",
                                               Grain.GrainNo));
            Summary.WriteMessage(this,
                                 string.Format("                      grains/plant =  {0,7:F1}",
                                               plant_grain_no));
            Summary.WriteMessage(this,
                                 string.Format("                       maximum lai =  {0,9:F3}",
                                               Leaf.MaximumLAI));
            Summary.WriteMessage(this,
                                 string.Format("Total above ground biomass (kg/ha) =  {0,7:F1}",
                                               AboveGround.Wt * 10));
            Summary.WriteMessage(this,
                                 string.Format(" Live above ground biomass (kg/ha) =  {0,7:F1}",
                                               AboveGroundLive.Wt * 10));
            Summary.WriteMessage(this,
                                 string.Format(" Dead above ground biomass (kg/ha) =  {0,7:F1}",
                                               AboveGroundDead.Wt * 10));
            Summary.WriteMessage(this,
                                 string.Format("                  Number of leaves =  {0,7:F1}",
                                               Leaf.LeafNumber));
            Summary.WriteMessage(this,
                                 string.Format("               DM Root:Shoot ratio =  {0,8:F2}",
                                               DMRrootShootRatio));
            Summary.WriteMessage(this,
                                 string.Format("                     Harvest Index =  {0,8:F2}",
                                               HarvestIndex));
            Summary.WriteMessage(this,
                                 string.Format("                  Stover C:N ratio =  {0,8:F2}",
                                               StoverCNRatio));
            Summary.WriteMessage(this,
                                 string.Format("                    Root C:N ratio =  {0,8:F2}",
                                               RootCNRatio));
            Summary.WriteMessage(this,
                                 string.Format("                   Grain N percent =  {0,8:F2}",
                                               n_grain_conc_percent));
            Summary.WriteMessage(this,
                                 string.Format("           Total N content (kg/ha) =  {0,8:F2}",
                                               n_total));
            Summary.WriteMessage(this,
                                 string.Format("            Grain N uptake (kg/ha) =  {0,8:F2}",
                                               n_grain));
            Summary.WriteMessage(this,
                                 string.Format("            Dead N content (kg/ha) =  {0,8:F2}",
                                               VegetativeDead.N * 10));
            Summary.WriteMessage(this,
                                 string.Format("           Green N content (kg/ha) =  {0,8:F2}",
                                               n_green));

            Summary.WriteMessage(this, string.Empty);
            Summary.WriteMessage(this, string.Empty);
            Summary.WriteMessage(this, "              Average Stress Indices    Water Photo  Water Expan  N Photo      N grain conc");

            Summary.WriteMessage(this, AverageStressMessage);
            AverageStressMessage = string.Empty;
    }
        #endregion

        #region Grazing


        /// <summary>Gets the available to animal.</summary>
        /// <value>The available to animal.</value>
        public AvailableToAnimalType AvailableToAnimal
        {
            get
            {
                List<AvailableToAnimalelementType> All = new List<AvailableToAnimalelementType>();
                foreach (Organ1 Organ in Organ1s)
                {
                    AvailableToAnimalelementType[] Available = Organ.AvailableToAnimal;
                    if (Available != null)
                        All.AddRange(Available);
                }
                AvailableToAnimalType AvailableToAnimal = new AvailableToAnimalType();
                AvailableToAnimal.element = All.ToArray();
                return AvailableToAnimal;
            }
        }


        /// <summary>Gets or sets the removed by animal.</summary>
        /// <value>The removed by animal.</value>
        [XmlIgnore]
        public RemovedByAnimalType RemovedByAnimal
        {
            get
            {
                return new RemovedByAnimalType();
            }
            set
            {
                if (value == null || value.element == null)
                    return;

                double dmRemovedGreenTops = 0.0;
                foreach (Organ1 Organ in Organ1s)
                {
                    Organ.RemovedByAnimal = value;

                    // BUG? The old plant model always had dmRemovedGreenTops = 0.0;
                    //dmRemovedGreenTops += Organ.GreenRemoved.Wt;  
                }

                // Update biomass and N pools. Different types of plant pools are affected in different ways.
                // Calculate Root Die Back
                double chop_fr_green_leaf = Utility.Math.Divide(Leaf.GreenRemoved.Wt, Leaf.Live.Wt, 0.0);

                Root.RemoveBiomassFraction(chop_fr_green_leaf);
                double biomassGreenTops = AboveGroundLive.Wt;

                foreach (Organ1 Organ in Organ1s)
                    Organ.RemoveBiomass();

                Stem.AdjustMorphologyAfterARemoveBiomass(); // the values calculated here are overwritten in plantPart::morphology(void)

                // now update new canopy covers
                PlantSpatial.Density = Population.Density;
                PlantSpatial.CanopyWidth = 0.0; //Leaf.Width;

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoCover();

                UpdateCanopy();

                double remove_biom_pheno = Utility.Math.Divide(dmRemovedGreenTops, biomassGreenTops, 0.0);
                Phenology.OnRemoveBiomass(remove_biom_pheno);

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoNConccentrationLimits();

                //protocol::ExternalMassFlowType EMF;
                //EMF.PoolClass = "crop";
                //EMF.FlowType = "loss";
                //EMF.DM = (Tops().GreenRemoved.DM() + Tops().SenescedRemoved.DM()) * gm2kg / sm2ha;
                //EMF.N = (Tops().GreenRemoved.N() + Tops().SenescedRemoved.N()) * gm2kg / sm2ha;
                //EMF.P = (Tops().GreenRemoved.P() + Tops().SenescedRemoved.P()) * gm2kg / sm2ha;
                //EMF.C = 0.0; // ?????
                //EMF.SW = 0.0;
                //scienceAPI.publish("ExternalMassFlow", EMF);

            }
        }

        #endregion

        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public List<Soils.UptakeInfo> GetSWUptake(List<Soils.UptakeInfo> info)
        {
            return info;
        }
        /// <summary>
        /// Set the potential sw uptake for today
        /// </summary>
        public void SetSWUptake(List<Soils.UptakeInfo> info)
        {
            //Root.AribtratorSWUptake = info[0].Amount;
        }

        /// <summary>A property to return all cultivar definitions.</summary>
        /// <value>The cultivars.</value>
        private List<Cultivar> Cultivars
        {
            get
            {
                List<Cultivar> cultivars = new List<Cultivar>();
                foreach (Model model in Apsim.Children(this, typeof(Cultivar)))
                {
                    cultivars.Add(model as Cultivar);
                }

                return cultivars;
            }
        }
    }
}
