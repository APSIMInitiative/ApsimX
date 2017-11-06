using System;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Phen;
using Models.PMF.Interfaces;
using System.Xml;
using System.Xml.Serialization;
using Models.PMF.Library;

namespace Models.PMF.Organs
{
    /// <summary>
    /// This organ uses a generic model for plant reproductive components.  Yield is calculated from its components in terms of organ number and size (for example, grain number and grain size).  
    /// </summary>
    [Serializable]
    public class ReproductiveOrgan : BaseOrgan, IArbitration
    {
        #region Parameter Input Classes
        /// <summary>The phenology</summary>
        [Link]
        protected Phenology Phenology = null;
        /// <summary>The water content</summary>
        [Link]
        [Units("g/g")]
        [Description("Water content used to calculate a fresh weight.")]
        IFunction WaterContent = null;
        
        /// <summary>The Maximum potential size of individual grains</summary>
        [Link]
        [Units("g/grain")]
        IFunction MaximumPotentialGrainSize = null;
        
        /// <summary>The number function</summary>
        [Link]
        [Units("/m2")]
        IFunction NumberFunction = null;
        /// <summary>The n filling rate</summary>
        [Link]
        [Units("g/m2/d")]
        IFunction NFillingRate = null;
        /// <summary>The maximum n conc</summary>
        [Link]
        [Units("g/g")]
        IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Link]
        [Units("g/g")]
        IFunction MinimumNConc = null;

        /// <summary>The dm demand function</summary>
        [Link]
        [Units("g/m2/d")]
        IFunction DMDemandFunction = null;

        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>Dry matter conversion efficiency</summary>
        [Link]
        public IFunction DMConversionEfficiency = null;

        /// <summary>The proportion of biomass repired each day</summary>
        [Link(IsOptional = true)]
        public IFunction MaintenanceRespirationFunction = null;

        #endregion

        #region Class Fields
        /// <summary>The ripe stage</summary>
        [Description("Stage at which this organ becomes ripe")]
        public string RipeStage { get; set; }
        /// <summary>The _ ready for harvest</summary>
        protected bool _ReadyForHarvest = false;
        /// <summary>The potential dm allocation</summary>
        private double PotentialDMAllocation = 0;
        #endregion

        #region Class Properties

        /// <summary>The live biomass</summary>
        public Biomass Live { get; set; }

        /// <summary>The dead biomass</summary>
        public Biomass Dead { get; set; }

        /// <summary>The number</summary>
        [XmlIgnore]
        [Units("/m^2")]
        public double Number { get; set; }

        /// <summary>The Maximum potential size of grains</summary>
        [XmlIgnore]
        [Units("/m^2")]
        public double MaximumSize { get; set; }

        /// <summary>Gets the live f wt.</summary>
        [Units("g/m^2")]
        public double LiveFWt
        {
            get
            {
                if (WaterContent != null)
                    return Live.Wt / (1 - WaterContent.Value());
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the size.</summary>
        [Units("g")]
        public double Size
        {
            get
            {
                if (Number > 0)
                    return Live.Wt / Number;
                else
                    return 0;
            }
        }

        /// <summary>Gets the size of the f.</summary>
        [Units("g")]
        private double FSize
        {
            get
            {
                if (Number > 0)
                {
                    if (WaterContent != null)
                        return (Live.Wt / Number) / (1 - WaterContent.Value());
                    else
                        return 0.0;
                }
                else
                    return 0;
            }
        }

        /// <summary>Gets the ready for harvest.</summary>
        public int ReadyForHarvest
        {
            get
            {
                if (_ReadyForHarvest)
                    return 1;
                else
                    return 0;
            }
        }
        #endregion

        #region Functions

        /// <summary>Initializes a new instance of the <see cref="ReproductiveOrgan"/> class.</summary>
        public ReproductiveOrgan()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
            {
                Number = NumberFunction.Value();
                MaximumSize = MaximumPotentialGrainSize.Value();
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
                Clear();
        }

        /// <summary>
        /// Execute harvest logic for reproductive organ
        /// </summary>
        public override void DoHarvest()
        {
                double YieldDW = (Live.Wt + Dead.Wt);

                Summary.WriteMessage(this, "Harvesting " + Name + " from " + Plant.Name);
                Summary.WriteMessage(this, " Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)");
                Summary.WriteMessage(this, " Size: " + Size.ToString("f2") + " (g)");
                Summary.WriteMessage(this, " Number: " + Number.ToString("f2") + " (/m^2)");

                Live.Clear();
                Dead.Clear();
                Number = 0;
                _ReadyForHarvest = false;
        }
        #endregion

        #region Event handlers

        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                Summary.WriteMessage(this, "Cutting " + Name + " from " + Plant.Name);

                Live.Clear();
                Dead.Clear();
                Number = 0;
                _ReadyForHarvest = false;
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            Biomass total = Live + Dead;
            if (total.Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                SurfaceOrganicMatter.Add(total.Wt * 10, total.N * 10, 0, Plant.CropType, Name);
            }

            Clear();
        }
        #endregion

        #region Arbitrator methods
        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public override BiomassPoolType CalculateDryMatterDemand()
        {
            dryMatterDemand.Structural = DMDemandFunction.Value() / DMConversionEfficiency.Value();
            return dryMatterDemand;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        public override BiomassPoolType CalculateNitrogenDemand()
        {
            double demand = NFillingRate.Value();
            demand = Math.Min(demand, MaximumNConc.Value() * PotentialDMAllocation);
            nitrogenDemand.Structural = demand;

            return nitrogenDemand;
        }


        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Phenology.OnDayOf(RipeStage))
                _ReadyForHarvest = true;


            MaintenanceRespiration = 0;
            //Do Maintenance respiration
            if (MaintenanceRespirationFunction != null)

            {
                MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value();
                Live.MetabolicWt *= (1 - MaintenanceRespirationFunction.Value());
                MaintenanceRespiration += Live.StorageWt * MaintenanceRespirationFunction.Value();
                Live.StorageWt *= (1 - MaintenanceRespirationFunction.Value());
            }

        }
        /// <summary>Sets the dry matter potential allocation.</summary>
        public override void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            if (DMDemand.Structural == 0)
                if (dryMatter.Structural < 0.000000000001) { }//All OK
                else
                    throw new Exception("Invalid allocation of potential DM in" + Name);
            PotentialDMAllocation = dryMatter.Structural;
            // PotentialDailyGrowth = value.Structural;
        }
        /// <summary>Sets the dry matter allocation.</summary>
        public override void SetDryMatterAllocation(BiomassAllocationType value)
        {
            GrowthRespiration = value.Structural * (1 - DMConversionEfficiency.Value());
            Live.StructuralWt += value.Structural * DMConversionEfficiency.Value();
            Allocated.StructuralWt = value.Structural;
        }
        /// <summary>Sets the n allocation.</summary>
        public override void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
            Allocated.StructuralN = nitrogen.Structural;
        }
        /// <summary>Gets or sets the maximum nconc.</summary>
        public double MaxNconc
        {
            get
            {
                return MaximumNConc.Value();
            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        public override double MinNconc
        {
            get
            {
                return MinimumNConc.Value();
            }
        }

        /// <summary>Gets the total biomass</summary>
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the total grain weight</summary>
        [Units("g/m2")]
        public double Wt { get { return Total.Wt; } }

        /// <summary>Gets the total grain N</summary>
        [Units("g/m2")]
        public double N { get { return Total.N; } }


        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [Units("g/g")]
        public double Nconc
        {
            get
            {
                if (Total.Wt > 0.0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }

        #endregion

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="value">The fractions of biomass to remove</param>
        public override void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType value)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, value, Live, Dead, Removed, Detached);
        }

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }
    }
}
