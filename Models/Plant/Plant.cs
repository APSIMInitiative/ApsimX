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
    public class Plant: Model, IXmlSerializable
    {
        public string CropType { get; set; }
        public Phenology Phenology { get; set; }
        public Arbitrator Arbitrator { get; set; }
        public Structure Structure { get; set; }
        public Summariser Summariser { get; set; }
        public SowPlant2Type SowingData;
        [XmlIgnore]
        public List<Organ> Organs { get; set; }
        
        #region XmlSerializable methods
        /// <summary>
        /// Return our schema - needed for IXmlSerializable.
        /// </summary>
        public XmlSchema GetSchema() { return null; }

        /// <summary>
        /// Read XML from specified reader. Called during Deserialisation.
        /// </summary>
        public virtual void ReadXml(XmlReader reader)
        {
            Organs = new List<Organ>();
            reader.Read();
            while (reader.IsStartElement())
            {
                string Type = reader.Name;

                if (Type == "Name")
                {
                    Name = reader.ReadString();
                    reader.Read();
                }
                else if (Type == "CropType")
                {
                    CropType = reader.ReadString();
                    reader.Read();
                } 
                else
                {
                    Model NewChild = Utility.Xml.Deserialise(reader) as Model;
                    if (NewChild is Organ)
                        Organs.Add(NewChild as Organ);
                    else
                        AddModel(NewChild, false);
                    NewChild.Parent = this;
                }
            }
            reader.ReadEndElement();
        }

        /// <summary>
        /// Write this point to the specified XmlWriter
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Name");
            writer.WriteString(Name);
            writer.WriteEndElement();
            writer.WriteStartElement("CropType");
            writer.WriteString(CropType);
            writer.WriteEndElement();

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            XmlSerializer serial = new XmlSerializer(typeof(Arbitrator));
            serial.Serialize(writer, Arbitrator, ns);

            XmlSerializer serial2 = new XmlSerializer(typeof(Phenology));
            serial2.Serialize(writer, Phenology, ns);

            XmlSerializer serial3 = new XmlSerializer(typeof(Structure));
            serial3.Serialize(writer, Structure, ns);

            XmlSerializer serial4 = new XmlSerializer(typeof(Summariser));
            serial4.Serialize(writer, Summariser, ns);

            foreach (object Model in Organs)
            {
                Type[] type = Utility.Reflection.GetTypeWithoutNameSpace(Model.GetType().Name);
                if (type.Length == 0)
                    throw new Exception("Cannot find a model with class name: " + Model.GetType().Name);
                if (type.Length > 1)
                    throw new Exception("Found two models with class name: " + Model.GetType().Name);


                serial = new XmlSerializer(type[0]);
                serial.Serialize(writer, Model, ns);
            }
        }

        #endregion

        //Fixme, work out how to do swim
        //[Input(IsOptional = true)]
        Single swim3 = 0;

        #region Outputs
         
        public double WaterSupplyDemandRatio = 0;
        
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
        public void Sow(SowPlant2Type ManagerSowingData)
        {
            SowingData = new SowPlant2Type();
            if(ManagerSowingData.Population != null)
                SowingData.Population = ManagerSowingData.Population;
            if (ManagerSowingData.Depth != null)
                SowingData.Depth = ManagerSowingData.Depth;
            SowingData.Cultivar = ManagerSowingData.Cultivar;
            if (ManagerSowingData.MaxCover != null)
                SowingData.MaxCover = ManagerSowingData.MaxCover;
            if (ManagerSowingData.BudNumber != null)
                SowingData.BudNumber = ManagerSowingData.BudNumber;
            if (ManagerSowingData.RowSpacing != null)
                SowingData.RowSpacing = ManagerSowingData.RowSpacing;
            if (ManagerSowingData.CropClass != null)
                SowingData.CropClass = ManagerSowingData.CropClass;
            

            // Go through all our children and find all organs.
            
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
            Structure.OnSow(SowingData);
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
