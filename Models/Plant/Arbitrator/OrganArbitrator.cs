using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Arbitrator;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Models.PMF
{
    ///<summary>
    /// The Arbitrator class determines the allocation of dry matter (DM) and Nitrogen between each of the organs in the crop model. Each organ can have up to three different pools of biomass:
    /// 
    /// * **Structural biomass** which is essential for growth and remains within the organ once it is allocated there.
    /// * **Metabolic biomass** which generally remains within an organ but is able to be re-allocated when the organ senesces and may be retranslocated when demand is high relative to supply.
    /// * **Storage biomass** which is partitioned to organs when supply is high relative to demand and is available for retranslocation to other organs whenever supply from uptake, fixation, or re-allocation is lower than demand.
    /// 
    /// The process followed for biomass arbitration is shown in Figure [FigureNumber]. Arbitration calculations are triggered by a series of events (shown below) that are raised every day.  For these calculations, at each step the Arbitrator exchange information with each organ, so the basic computations of demand and supply are done at the organ level, using their specific parameters. 
    /// 
    /// 1. **doPotentialPlantGrowth**.  When this event occurs, each organ class executes code to determine their potential growth, biomass supplies and demands.  In addition to demands for structural, non-structural and metabolic biomass (DM and N) each organ may have the following biomass supplies: 
    /// 	* **Fixation supply**.  From photosynthesis (DM) or symbiotic fixation (N)
    /// 	* **Uptake supply**.  Typically uptake of N from the soil by the roots but could also be uptake by other organs (eg foliage application of N).
    /// 	* **Retranslocation supply**.  Storage biomass that may be moved from organs to meet demands of other organs.
    /// 	* **Reallocation supply**. Biomass that can be moved from senescing organs to meet the demands of other organs.
    /// 2. **doPotentialPlantPartitioning.** On this event the Arbitrator first executes the DoDMSetup() method to gather the DM supplies and demands from each organ, these values are computed at the organ level.  It then executes the DoPotentialDMAllocation() method which works out how much biomass each organ would be allocated assuming N supply is not limiting and sends these allocations to the organs.  Each organ then uses their potential DM allocation to determine their N demand (how much N is needed to produce that much DM) and the arbitrator calls DoNSetup() to gather the N supplies and demands from each organ and begin N arbitration.  Firstly DoNReallocation() is called to redistribute N that the plant has available from senescing organs.  After this step any unmet N demand is considered as plant demand for N uptake from the soil (N Uptake Demand).
    /// 3. **doNutrientArbitration.** When this event occurs, the soil arbitrator gets the N uptake demands from each plant (where multiple plants are growing in competition) and their potential uptake from the soil and determines how much of their demand that the soil is able to provide.  This value is then passed back to each plant instance as their Nuptake and doNUptakeAllocation() is called to distribute this N between organs.  
    /// 4. **doActualPlantPartitioning.**  On this event the arbitrator call DoNRetranslocation() and DoNFixation() to satisfy any unmet N demands from these sources.  Finally, DoActualDMAllocation is called where DM allocations to each organ are reduced if the N allocation is insufficient to achieve the organs minimum N concentration and final allocations are sent to organs. 
    /// 
    /// ![Alt Text](ArbitratorSequenceDiagram.png)
    /// 
    /// **Figure [FigureNumber]:**  Schematic showing the procedure for arbitration of biomass partitioning.  Pink boxes represent events that occur every day and their numbering shows the order of calculations. Blue boxes represent the methods that are called when these events occur.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    public class OrganArbitrator : Model, IUptake, IArbitrator, ICustomDocumentation
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        ///// <summary>The method used to arbitrate N allocations</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //protected IArbitrationMethod nArbitrator = null;

        ///// <summary>The method used to arbitrate N allocations</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //protected IArbitrationMethod dmArbitrator = null;

        /// <summary>The method used to call N arbitrations methods</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        protected BiomassTypeArbitrator nArbitration = null;

        /// <summary>The method used to call DM arbitrations methods</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        protected BiomassTypeArbitrator dmArbitration = null;

        /// <summary>The method used to call water uptakes</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        protected IUptakeMethod waterUptakeMethod = null;

        /// <summary>The method used to call water uptakes</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        protected IUptakeMethod nitrogenUptakeMethod = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The kgha2gsm</summary>
        protected const double kgha2gsm = 0.1;

        /// <summary>The list of organs</summary>
        protected List<IArbitration> Organs = new List<IArbitration>();

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetDMSupply;
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetNSupply;
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetDMDemand;
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetNDemand;

        
        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>The variables for DM</summary>
        [XmlIgnore]
        public BiomassArbitrationType DM { get; private set; }

        /// <summary>The variables for N</summary>
        [XmlIgnore]
        public BiomassArbitrationType N { get; private set; }

        //// <summary>Gets the dry mass supply relative to dry mass demand.</summary>
        /// <value>The dry mass supply.</value>
        [XmlIgnore]
        public double FDM { get { return DM == null ? 0 : MathUtilities.Divide(DM.TotalPlantSupply, DM.TotalPlantDemand, 0); } }

        /// <summary>Gets the dry mass supply relative to dry structural demand plus metabolic demand.</summary>
        /// <value>The dry mass supply.</value>
        [XmlIgnore]
        public double StructuralCarbonSupplyDemand { get { return DM == null ? 0 : MathUtilities.Divide(DM.TotalPlantSupply, (DM.TotalStructuralDemand + DM.TotalMetabolicDemand), 0); } }

        /// <summary>Gets the delta wt.</summary>
        /// <value>The delta wt.</value>
        public double DeltaWt { get { return DM == null ? 0 : (DM.End - DM.Start); } }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FN { get { return N == null ? 0 : MathUtilities.Divide(N.TotalPlantSupply, N.TotalPlantDemand, 0); } }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        public double WAllocated { get; protected set; }


        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e) { Clear(); }
        
        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            List<IArbitration> organsToArbitrate = new List<IArbitration>();

            foreach (IOrgan organ in plant.Organs)
                if (organ is IArbitration)
                    organsToArbitrate.Add(organ as IArbitration);

            Organs = organsToArbitrate;
            DM = new BiomassArbitrationType("DM", Organs);
            N = new BiomassArbitrationType("N", Organs);
        }

        
        /// First get all demands and supplies, send potential DM allocations and do N reallocation so N uptake demand can be calculated
        
        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        virtual protected void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                DM.Clear();
                N.Clear();

                DMSupplies();
                DMDemands();

                dmArbitration.DoPotentialPartitioning(Organs.ToArray(), DM);

                NSupplies();
                NDemands();

                nArbitration.DoPotentialPartitioning(Organs.ToArray(), N);
            }
        }
        
        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsAlive)
            {
                return waterUptakeMethod.GetUptakeEstimates(soilstate, Organs.ToArray());
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {
            waterUptakeMethod.SetActualUptakes(zones, Organs.ToArray());
            //WDemand = waterUptakeMethod.WDemand;
            //WAllocated = waterUptakeMethod.WAllocated;

            // Give the water uptake for each zone to Root so that it can perform the uptake
            // i.e. Root will do pass the uptake to the soil water balance.
            foreach (ZoneWaterAndN Z in zones)
                plant.Root.DoWaterUptake(Z.Water, Z.Zone.Name);

        }

        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public virtual List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsEmerged)
            {
                return nitrogenUptakeMethod.GetUptakeEstimates(soilstate, Organs.ToArray());
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (plant.IsEmerged)
            {
                nitrogenUptakeMethod.SetActualUptakes(zones, Organs.ToArray());
                //Allocate N that the SoilArbitrator has allocated the plant to each organ
                nArbitration.DoUptakes(Organs.ToArray(), N);
                plant.Root.DoNitrogenUptake(zones);
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        virtual protected void OnDoActualPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                //ordering within the arbitration items is important - uses the order in the tree
                //Do the rest of the N partitioning, revise DM allocations if N is limited and do DM and N allocations
                nArbitration.DoActualPartitioning(Organs.ToArray(), N);

                dmArbitration.DoAllocations(Organs.ToArray(), DM);
                nArbitration.DoAllocations(Organs.ToArray(), N);
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        virtual protected void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }

        /// Local methods for setting up supplies and demands
        /// <summary>Accumulate all of the Organ DM Supplies </summary>
        public virtual void DMSupplies()
        {
            // Setup DM supplies from each organ
            SetDMSupply?.Invoke(this, new EventArgs());
            BiomassSupplyType[] supplies = Organs.Select(organ => organ.DMSupply).ToArray();

            double totalWt = Organs.Sum(o => o.Total.Wt);
            DM.GetSupplies(supplies, totalWt);

        }

        /// <summary>Calculate all of the Organ DM Demands </summary>
        public virtual void DMDemands()
        {
            // Setup DM demands for each organ  
            SetDMDemand?.Invoke(this, new EventArgs());
            BiomassPoolType[] demands = Organs.Select(organ => organ.DMDemand).ToArray();
            BiomassPoolType[] Qpriorities = Organs.Select(organ => organ.DMDemandPriorityFactor).ToArray();
            DM.GetDemands(demands,Qpriorities);
        }

        /// <summary>Calculate all of the Organ N Supplies </summary>
        public virtual void NSupplies()
        {
            // Setup N supplies from each organ
            SetNSupply?.Invoke(this, new EventArgs());
            BiomassSupplyType[] supplies = Organs.Select(organ => organ.NSupply).ToArray();
            double totalN = Organs.Sum(o => o.Total.N);
            N.GetSupplies(supplies, totalN);

        }

        /// <summary>Calculate all of the Organ N Demands </summary>
        public virtual void NDemands()
        {
            // Setup N demands
            SetNDemand?.Invoke(this, new EventArgs());
            BiomassPoolType[] demands = Organs.Select(organ => organ.NDemand).ToArray();
            N.GetDemands(demands);

        }

        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            DM = new BiomassArbitrationType("DM", Organs);
            N = new BiomassArbitrationType("N", Organs);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
