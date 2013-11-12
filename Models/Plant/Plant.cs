using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.Plant.Organs;
using Models.Plant.Phen;
using Models.Soils;

namespace Models.Plant
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
        public Double Population;
        public Double Depth;
        public Double RowSpacing;
        public Double MaxCover;
        public Double BudNumber;
        public Double SkipRow;
        public Double SkipPlant;
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
    public class Plant2: Model
    {

        public Phenology Phenology { get; set; }

        public Arbitrator Arbitrator { get; set; }

        public Structure Structure { get; set; }

        public SowPlant2Type SowingData;

        private List<Organ> _Organs = new List<Organ>();
        
        public List<Organ> Organs
        {
            // Public property to return our organs to caller. Used primarily for unit testing.
            get { return _Organs; }
        }

        //Fixme, work out how to do swim
        //[Input(IsOptional = true)]
        Single swim3 = 0;

        #region Outputs
        public string CropType = "";
        
        private double WaterSupplyDemandRatio = 0;
        
        public string plant_status
        {
            get
            {
                // What should be returned here?
                // The old "plant" component returned either "out", "alive"
                // How to determine "dead"?
                return "alive";
            }
        }
        
        [Units("mm")]
        private double WaterDemand   // Needed for SWIM2
        {
            get
            {
                double Demand = 0;
                foreach (Organ o in Organs)
                    Demand += o.WaterDemand;
                return Demand;
            }
        }

        public string FullName
        {
            get { return this.FullName; }
        }
        #endregion

        #region Plant functions
        private void DoPhenology()
        {
            if (Phenology != null)
                Phenology.DoTimeStep();
        }
        public void DoDMSetUp()
        {
            if (Structure != null)
                Structure.DoPotentialDM();
            foreach (Organ o in Organs)
                o.DoPotentialDM();
        }
        public void DoNutrientSetUp()
        {
            foreach (Organ o in Organs)
                o.DoPotentialNutrient();
        }
        private void DoWater()
        {
            if (swim3 == 0)
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
            else
            {
                double Uptake = 0;
                double Demand = 0;
                double Supply = 0;
                foreach (Organ o in Organs)
                {
                    Supply += o.WaterSupply;
                    Uptake += o.WaterUptake;
                    Demand += o.WaterDemand;
                }
                // It is REALLY dodgy that we need to do this at all
                if (Demand > 0)
                    WaterSupplyDemandRatio = Supply / Demand;
                else
                    WaterSupplyDemandRatio = 1;

                double fraction = 1;
                if (Demand > 0)
                    fraction = Uptake / Demand;
                if (fraction > 1.001)
                    throw new Exception("Water uptake exceeds total crop demand.");

                foreach (Organ o in Organs)
                    if (o.WaterDemand > 0)
                        o.WaterAllocation = fraction * o.WaterDemand;

                //throw new Exception("Cannot talk to swim3 yet");
            }
        }
        public void DoActualGrowth()
        {
            if (Structure != null)
                Structure.DoActualGrowth();
            foreach (Organ o in Organs)
                o.DoActualGrowth();
        }
        #endregion



        #region Event handlers and publishers
       
        

        public event NewCropDelegate NewCrop;
        
        public event NullTypeDelegate Sowing;
        
        public event NullTypeDelegate Cutting;
        
        public event NewCropDelegate CropEnding;
        
        public event BiomassRemovedDelegate BiomassRemoved;

        public void Sow(double BudNumber, double Population, string Cultivar)
        {
            SowingData = new SowPlant2Type();
            SowingData.BudNumber = BudNumber;
            SowingData.Population = Population;
            SowingData.Cultivar = Cultivar;

            // Go through all our children and find all organs.
            Organs.Clear();
            foreach (object ChildObject in this.Models)
            {
                Organ Child = ChildObject as Organ;
                if (Child != null)
                    Organs.Add(Child);
            }

            if (NewCrop != null)
            {
                NewCropType Crop = new NewCropType();
                Crop.crop_type = CropType;
                Crop.sender = Name;
                NewCrop.Invoke(Crop);
            }

            if (Sowing != null)
                Sowing.Invoke();

            // tell all our children about sow
            foreach (Organ Child in Organs)
                Child.OnSow(SowingData);
        }
        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {
            if (SowingData != null)
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
        [EventSubscribe("Harvest")]
        private void OnHarvest()
        {
            // tell all our children about sow
            foreach (Organ Child in Organs)
                Child.OnHarvest();

            // I cannot end call end crop, however can call onharvest?
            // temp
            //     OnEndCrop();

        }
        [EventSubscribe("EndCrop")]
        private void OnEndCrop()
        {
            NewCropType Crop = new NewCropType();
            Crop.crop_type = CropType;
            Crop.sender = Name;
            if (CropEnding != null)
                CropEnding.Invoke(Crop);

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

            // tell all our children about sow
            foreach (Organ Child in Organs)
                Child.OnEndCrop();
        }
        [EventSubscribe("Cut")]
        public void OnCut()
        {
            Cutting.Invoke();
        }
        #endregion

    }
}
