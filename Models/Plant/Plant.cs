using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Soils;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;

namespace Models.PMF
{
    public class WaterUptakesCalculatedUptakesType
    {
        public String Name = "";
        public Double[] Amount;
    }
    public class WaterUptakesCalculatedType
    {
        public WaterUptakesCalculatedUptakesType[] Uptakes;
    }
    public delegate void WaterUptakesCalculatedDelegate(WaterUptakesCalculatedType Data);
    public class WaterChangedType
    {
        public Double[] DeltaWater;
    }
    public delegate void WaterChangedDelegate(WaterChangedType Data);
    public class PruneType
    {
        public Double BudNumber;
    }
    public class KillLeafType
    {
        public Single KillFraction;
    }
    public delegate void NewCanopyDelegate(NewCanopyType NewCanopyData);
    public delegate void FOMLayerDelegate(FOMLayerType Data);
    public delegate void NullTypeDelegate();
    public delegate void NewCropDelegate(NewCropType Data);
    public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);
    [Serializable]
    public class SowPlant2Type
    {
        public String Cultivar = "";
        public Double Population = 100;
        public Double Depth = 100;
        public Double RowSpacing = 150;
        public Double MaxCover = 1;
        public Double BudNumber = 1;
        public String CropClass = "Plant";
        public Double SkipRow; //Not yet handled in Code
        public Double SkipPlant; //Not yet handled in Code
    }
    public class BiomassRemovedType
    {
        public String crop_type = "";
        public String[] dm_type;
        public Single[] dlt_crop_dm;
        public Single[] dlt_dm_n;
        public Single[] dlt_dm_p;
        public Single[] fraction_to_residue;
    }
    public class NewCropType
    {
        public String sender = "";
        public String crop_type = "";
    }


    [Serializable]
    public class Plant : ModelCollectionFromResource, ICrop
    {
        private Organ[] _Organs = null;
        public string CropType { get; set; }
        [Link(IsOptional = true)] public Phenology Phenology = null;
        [Link(IsOptional = true)] public OrganArbitrator Arbitrator = null;
        [Link(IsOptional = true)] private Models.PMF.Functions.SupplyFunctions.RUEModel RUEModel = null;
        [Link(IsOptional=true)] public Structure Structure = null;

        [XmlIgnore]
        public SowPlant2Type SowingData;

        [XmlIgnore]
        public Organ[] Organs 
        { 
            get 

            {
                if (_Organs == null)
                {
                    List<Organ> organs = new List<Organ>();
                    foreach (Organ organ in Children.MatchingMultiple(typeof(Organ)))
                        organs.Add(organ);
                    _Organs = organs.ToArray();
                }
                return _Organs;
            } 
        }

        [XmlIgnore]
        public NewCanopyType CanopyData { get { return LocalCanopyData; } }
        [XmlIgnore]
        public NewCanopyType LocalCanopyData;

        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
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

        /// <summary>
        /// A property to return all cultivar definitions.
        /// </summary>
        private List<Cultivar> Cultivars
        {
            get
            {
                List<Cultivar> cultivars = new List<Cultivar>();
                foreach (Model model in this.Children.MatchingMultiple(typeof(Cultivar)))
                {
                    cultivars.Add(model as Cultivar);
                }

                return cultivars;
            }
        }

        /// <summary>
        /// The current cultivar definition.
        /// </summary>
        private Cultivar cultivarDefinition;

        /// <summary>
        /// MicroClimate needs FRGR.
        /// </summary>
        public double FRGR 
        { 
            get 
            {
                double frgr = 1;
                foreach (Organ Child in Organs)
                {
                    if (Child.FRGR <= 1)
                      frgr = Child.FRGR;
                }
                return frgr; 
            } 
        }
            
        /// <summary>
        /// MicroClimate supplies light profile.
        /// </summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>
        /// MicroClimate supplies Potential EP
        /// </summary>
        [XmlIgnore]
        public double PotentialEP {get; set;}

        #region Links
        [Link]
        ISummary Summary = null;
        #endregion

        #region Outputs
        /// <summary>
        /// Is the plant in the ground?
        /// </summary>
        public bool InGround
        {
            get
            {
                return SowingData != null;
            }
        }

        [XmlIgnore]
        public double WaterSupplyDemandRatio { get; private set; }

        [XmlIgnore]
        [Description("Number of plants per meter2")]
        [Units("/m2")]
        public double Population { get; set; }
        
        #endregion

        #region Public functions
        /// <summary>
        /// Sow the crop with the specified parameters.
        /// </summary>
        public void Sow(string Cultivar, double Population = 1, double Depth = 100, double RowSpacing = 150, double MaxCover = 1, double BudNumber = 1, string CropClass = "Plant")
        {
            SowingData = new SowPlant2Type();
            SowingData.Population = Population;
            SowingData.Depth = Depth;
            SowingData.Cultivar = Cultivar;
            SowingData.MaxCover = MaxCover;
            SowingData.BudNumber = BudNumber;
            SowingData.RowSpacing = RowSpacing;
            SowingData.CropClass = CropClass;

            // Find cultivar and apply cultivar overrides.
            cultivarDefinition = PMF.Cultivar.Find(Cultivars, SowingData.Cultivar);
            cultivarDefinition.Apply(this);

            //Set up CanopyData type
            LocalCanopyData = new NewCanopyType();

            // Invoke a sowing event.
            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            this.Population = Population;

            // tell all our children about sow
            foreach (Organ Child in Organs)
                Child.OnSow(SowingData);
            if (Structure != null)
               Structure.OnSow(SowingData);
            if (Phenology != null)
            Phenology.OnSow();

            
       
            Summary.WriteMessage(FullPath, string.Format("A crop of " + CropType +" (cultivar = " + Cultivar + " Class = " + CropClass + ") was sown today at a population of " + Population + " plants/m2 with " + BudNumber + " buds per plant at a row spacing of " + RowSpacing + " and a depth of " + Depth + " mm"));
        }

        /// <summary>
        /// Harvest the crop.
        /// </summary>
        public void Harvest()
        {
            // Invoke a harvesting event.
            if (Harvesting != null)
                Harvesting.Invoke(this, new EventArgs());

            // tell all our children about sow
            foreach (Organ Child in Organs)
                Child.OnHarvest();
        }

        /// <summary>
        /// End the crop.
        /// </summary>
        public void EndCrop()
        {
            Summary.WriteMessage(FullPath, "Crop ending");

            if (PlantEnding != null)
                PlantEnding.Invoke(this, new EventArgs());

            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            BiomassRemovedData.crop_type = CropType;
            BiomassRemovedData.dm_type = new string[Organs.Length];
            BiomassRemovedData.dlt_crop_dm = new float[Organs.Length];
            BiomassRemovedData.dlt_dm_n = new float[Organs.Length];
            BiomassRemovedData.dlt_dm_p = new float[Organs.Length];
            BiomassRemovedData.fraction_to_residue = new float[Organs.Length];
            int i = 0;
            foreach (Organ O in Organs)
            {
                if (O is AboveGround)
                {
                    BiomassRemovedData.dm_type[i] = O.Name;
                    BiomassRemovedData.dlt_crop_dm[i] = (float)(O.Live.Wt + O.Dead.Wt) * 10f;
                    BiomassRemovedData.dlt_dm_n[i] = (float)(O.Live.N + O.Dead.N) * 10f;
                    BiomassRemovedData.dlt_dm_p[i] = 0f;
                    BiomassRemovedData.fraction_to_residue[i] = 1f;
                }
                else
                {
                    BiomassRemovedData.dm_type[i] = O.Name;
                    BiomassRemovedData.dlt_crop_dm[i] = 0f;
                    BiomassRemovedData.dlt_dm_n[i] = 0f;
                    BiomassRemovedData.dlt_dm_p[i] = 0f;
                    BiomassRemovedData.fraction_to_residue[i] = 0f;
                }
                i++;
            }
            BiomassRemoved.Invoke(BiomassRemovedData);

            // tell all our children about endcrop
            foreach (Organ Child in Organs)
                Child.OnEndCrop();
            Clear();

            cultivarDefinition.Unapply();
        }

        private void Clear()
        {
            SowingData = null;
            WaterSupplyDemandRatio = 0;
            Population = 0;
            if (Structure != null)
               Structure.Clear();
            if (Phenology != null)
               Phenology.Clear();
            if (Arbitrator != null)
               Arbitrator.Clear();
        }

        /// <summary>
        /// Cut the crop.
        /// </summary>
        public void Cut()
        {
            if (Cutting != null)
                Cutting.Invoke(this, new EventArgs());

            // tell all our children about endcrop
            foreach (Organ Child in Organs)
                Child.OnCut();
        }
        #endregion

        #region Private functions
        private void DoPhenology()
        {
            if (Phenology != null)
                Phenology.DoTimeStep();
        }
        private void DoDMSetUp()
        {
            if (Structure != null)
                Structure.DoPotentialDM();
            foreach (Organ o in Organs)
                o.DoPotentialDM();
        }
        private void DoNutrientSetUp()
        {
            foreach (Organ o in Organs)
                o.DoPotentialNutrient();
        }
        private void DoWater()
        {
            double Supply = 0;
            double Demand = 0;
            foreach (Organ o in Organs)
            {
                Supply += o.WaterSupply;
                Demand += o.WaterDemand;
            }

            if (Demand > 0)
                WaterSupplyDemandRatio = Supply / Demand;
            else
                WaterSupplyDemandRatio = 1;

            double fraction = 1;
            if (Demand > 0)
                fraction = Math.Min(1.0, Supply / Demand);

            foreach (Organ o in Organs)
                if (o.WaterDemand > 0)
                    o.WaterAllocation = fraction * o.WaterDemand;

            double FractionUsed = 0;
            if (Supply > 0)
                FractionUsed = Math.Min(1.0, Demand / Supply);

            foreach (Organ o in Organs)
                o.DoWaterUptake(FractionUsed * Supply);
        }
        private void DoActualGrowth()
        {
            if (Structure != null)
                Structure.DoActualGrowth();
            foreach (Organ o in Organs)
                o.DoActualGrowth();
        }
        #endregion

        #region Event handlers and publishers

        public event EventHandler Sowing;
        public event EventHandler Harvesting;
        public event EventHandler Cutting;
        public event EventHandler PlantEnding;
        public event BiomassRemovedDelegate BiomassRemoved;

        public override void OnSimulationCommencing()
        {
            Clear();
            foreach (Organ o in Organs)
            {
                o.Clear();
            }
        }


        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (InGround)
            {
                DoPhenology();
                DoDMSetUp();
                DoWater();  //Fixme Do water should go before do DMsetup
                if (Arbitrator != null)
                    Arbitrator.DoDMArbitration(Organs);
                DoNutrientSetUp();
                if (Arbitrator != null)
                    Arbitrator.DoNutrientArbitration(Organs);
                DoActualGrowth();
            }
        }
       
        #endregion

    }
}
