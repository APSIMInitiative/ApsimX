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
    public delegate void NewPotentialGrowthDelegate(NewPotentialGrowthType Data);
    public class KillLeafType
    {
        public Single KillFraction;
    }
    public delegate void NewCanopyDelegate(NewCanopyType Data);
    public delegate void FOMLayerDelegate(FOMLayerType Data);
    public delegate void NullTypeDelegate();
    public delegate void NewCropDelegate(NewCropType Data);
    public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);
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
    public class Plant: Model
    {
        public string CropType { get; set; }
        public Phenology Phenology { get; set; }
        public Arbitrator Arbitrator { get; set; }
        public Structure Structure { get; set; }
        public Summariser Summariser { get; set; }
        public SowPlant2Type SowingData;


        [XmlArrayItem(typeof(BelowGroundOrgan))]
        [XmlArrayItem(typeof(GenericAboveGroundOrgan))]
        [XmlArrayItem(typeof(GenericBelowGroundOrgan))]
        [XmlArrayItem(typeof(GenericOrgan))]
        [XmlArrayItem(typeof(HIReproductiveOrgan))]
        [XmlArrayItem(typeof(Leaf))]
        [XmlArrayItem(typeof(Nodule))]
        [XmlArrayItem(typeof(ReproductiveOrgan))]
        [XmlArrayItem(typeof(ReserveOrgan))]
        [XmlArrayItem(typeof(Root))]
        [XmlArrayItem(typeof(RootSWIM))]
        [XmlArrayItem(typeof(SimpleLeaf))]
        [XmlArrayItem(typeof(SimpleRoot))]
        public List<Organ> Organs { get; set; }

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
        #endregion

        #region Public functions
        /// <summary>
        /// Sow the crop with the specified parameters.
        /// </summary>
        public void Sow(string Cultivar, double Population, double Depth = 100, double RowSpacing = 150, double MaxCover = 1, double BudNumber = 1, string CropClass = "Plant")
        {
            SowingData = new SowPlant2Type();
            SowingData.Population = Population;
            SowingData.Depth = Depth;
            SowingData.Cultivar = Cultivar;
            SowingData.MaxCover = MaxCover;
            SowingData.BudNumber = BudNumber;
            SowingData.RowSpacing = RowSpacing;
            SowingData.CropClass = CropClass;

            // Invoke a sowing event.
            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            // tell all our children about sow
            foreach (Organ Child in Organs)
                Child.OnSow(SowingData);
            Structure.OnSow(SowingData);
            Phenology.OnSow();
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
            BiomassRemovedData.dm_type = new string[Organs.Count];
            BiomassRemovedData.dlt_crop_dm = new float[Organs.Count];
            BiomassRemovedData.dlt_dm_n = new float[Organs.Count];
            BiomassRemovedData.dlt_dm_p = new float[Organs.Count];
            BiomassRemovedData.fraction_to_residue = new float[Organs.Count];
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
            Structure.OnPlantEnding();
            Phenology.OnPlantEnding();
            Clear();
        }

        private void Clear()
        {
            SowingData = null;
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

        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            Clear();
            foreach (Organ o in Organs)
            {
                o.Clear();
            }
        }


        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
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
