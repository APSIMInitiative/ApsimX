using System;
using System.Collections.Generic;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF
{
    ///<summary>    /// The Arbitrator class determines the allocation of dry matter (DM) and Nitrogen between each of the organs in the crop model. Each organ can have up to three differnt pools of biomass:    ///     /// * **Structural biomass** which remains within an organ once it is partitioned there    /// * **Metabolic biomass** which generally remains within an organ but is able to be re-allocated when the organ senesses and may be re-translocated when demand is high relative to supply.    /// * **Non-structural biomass** which is partitioned to organs when supply is high relative to demand and is available for re-translocation to other organs whenever supply from uptake, fixation and re-allocation is lower than demand .    ///     /// The process followed for biomass arbitration is shown in Figure 1. Arbitration responds to events broadcast daily by the central APSIM infrastructure:     ///     /// 1. **doPotentialPlantGrowth**.  When this event is broadcast each organ class executes code to determine their potential growth, biomass supplies and demands.  In addition to demands for structural, non-structural and metabolic biomass (DM and N) each organ may have the following biomass supplies:     /// 	* **Fixation supply**.  From photosynthesis (DM) or symbiotic fixation (N)    /// 	* **Uptake supply**.  Typically uptake of N from the soil by the roots but could also be uptake by other organs (eg foliage application of N).    /// 	* **Retranslocation supply**.  Non-structural biomass that may be moved from organs to meet demands of other organs.    /// 	* **Reallocation supply**. Biomass that can be moved from senescing organs to meet the demands of other organs.    /// 2. **doPotentialPlantPartitioning.** On this event the Arbitrator first executes the DoDMSetup() method to establish the DM supplies and demands from each organ.  It then executes the DoPotentialDMAllocation() method which works out how much biomass each organ would be allocated assuming N supply is not limiting and sends these allocations to the organs.  Each organ then uses their potential DM allocation to determine their N demand (how much N is needed to produce that much DM) and the arbitrator calls DoNSetup() establish N supplies and Demands and begin N arbitration.  Firstly DoNReallocation() is called to redistribute N that the plant has available from senescing organs.  After this step any unmet N demand is considered the plants demand for N uptake from the soil (N Uptake Demand).    /// 3. **doNutrientArbitration.** When this event is broadcast by the model framework the soil arbitrator gets the N uptake demands from each plant (where multiple plants are growing in competition) and their potential uptake from the soil and determines how nuch of their demand that the soil is able to provide.  This value is then passed back to each plant instance as their Nuptake and doNUptakeAllocation() is called to distribute this N between organs.      /// 4. **doActualPlantPartitioning.**  On this event the arbitrator call DoNRetranslocation() and DoNFixation() to satisify any unmet N demands from these sources.  Finally, DoActualDMAllocation is called where DM allocations to each organ are reduced if the N allocation is insufficient to achieve the organs minimum N conentration and final allocations are sent to organs.     /// 
    /// ![Alt Text](..\\..\\Documentation\\Images\\ArbitrationDiagram.PNG)    ///     /// **Figure 1.**  Schematic showing procedure for arbitration of biomass partitioning.  Pink boxes are events that are broadcast each day by the model infrastructure and their numbering shows the order of procedure. Blue boxes are methods that are called when these events are broadcast.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class OrganArbitrator : Model, IUptake
    {
        #region Links and Input parameters

        /// <summary>APSIMs clock model</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        public Plant Plant = null;

        /// <summary>The soil</summary>
        [Link]
        public Soils.Soil Soil = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link]
        private IArbitrationMethod NArbitrator = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link]
        private IArbitrationMethod DMArbitrator = null;

        /// <summary>The nutrient drivers</summary>
        [Description("List of nutrients that the arbitrator will consider")]
        public string[] NutrientDrivers = null;

        /// <summary>The kgha2gsm</summary>
        private const double kgha2gsm = 0.1;

        /// <summary>The list of organs</summary>
        private IArbitration[] Organs;

        /// <summary>The variables for DM</summary>
        [XmlIgnore]
        public BiomassArbitrationType DM { get; private set; }

        /// <summary>The variables for N</summary>
        [XmlIgnore]
        public BiomassArbitrationType N { get; private set; }

        #endregion

        #region Main outputs

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FDM { get { return MathUtilities.Divide(DM.TotalPlantSupply, DM.TotalPlantDemand, 0); } }

        /// <summary>Gets the delta wt.</summary>
        /// <value>The delta wt.</value>
        public double DeltaWt { get { return DM.End - DM.Start; } }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FN { get { return MathUtilities.Divide(N.TotalPlantSupply, N.TotalPlantDemand, 0); } }

        /// <summary>Gets the water supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WSupply { get; private set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public double WDemand { get; private set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        public double WAllocated { get; private set; }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FW { get { return MathUtilities.Divide(WSupply, WDemand, 0); } }

        #endregion

        #region IUptake interface

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
        {
            if (Plant.IsAlive)
            {
                // Get all water supplies.
                double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                List<double[]> supplies = new List<double[]>();
                List<Zone> zones = new List<Zone>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                    foreach (IOrgan o in Organs)
                        if (o is IWaterNitrogenUptake)
                        {
                            double[] organSupply = (o as IWaterNitrogenUptake).CalculateWaterSupply(zone);
                            if (organSupply != null)
                            {
                                supplies.Add(organSupply);
                                zones.Add(zone.Zone);
                                waterSupply += MathUtilities.Sum(organSupply) * zone.Zone.Area;
                            }
                        }
 
                // Calculate total water demand.
                double waterDemand = 0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
                foreach (IArbitration o in Organs)
                    if (o is IHasWaterDemand)
                        waterDemand += (o as IHasWaterDemand).CalculateWaterDemand() * Plant.Zone.Area;
 
                // Calculate demand / supply ratio.
                double fractionUsed = 0;
                if (waterSupply > 0)
                    fractionUsed = Math.Min(1.0, waterDemand / waterSupply);

                // Apply demand supply ratio to each zone and create a ZoneWaterAndN structure
                // to return to caller.
                List<ZoneWaterAndN> ZWNs = new List<ZoneWaterAndN>();
                for (int i = 0; i < supplies.Count; i++)
                {
                    // Just send uptake from my zone
                    ZoneWaterAndN uptake = new ZoneWaterAndN(zones[i]);
                    uptake.Water = MathUtilities.Multiply_Value(supplies[i], fractionUsed);
                    uptake.NO3N = new double[uptake.Water.Length];
                    uptake.NH4N = new double[uptake.Water.Length];
                    ZWNs.Add(uptake);
                }
                return ZWNs;
            }
            else
                return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetSWUptake(List<ZoneWaterAndN> zones)
        {
            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
                waterSupply += MathUtilities.Sum(Z.Water) * Z.Zone.Area;

            // Calculate total plant water demand.
            WDemand = 0.0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (IArbitration o in Organs)
                if (o is IHasWaterDemand)
                    WDemand += (o as IHasWaterDemand).CalculateWaterDemand() * Plant.Zone.Area;

            // Calculate the fraction of water demand that has been given to us.
            double fraction = 1;
            if (WDemand > 0)
                fraction = Math.Min(1.0, waterSupply / WDemand);

            // Proportionally allocate supply across organs.
            WAllocated = 0.0;
            foreach (IArbitration o in Organs)
                if (o is IHasWaterDemand)
                {
                    double demand = (o as IHasWaterDemand).CalculateWaterDemand();
                    if (demand > 0)
                    {
                        double allocation = fraction * demand;
                        (o as IHasWaterDemand).WaterAllocation = allocation;
                        WAllocated += allocation;
                    }
                }

            // Give the water uptake for each zone to Root so that it can perform the uptake
            // i.e. Root will do pass the uptake to the soil water balance.
            foreach (ZoneWaterAndN Z in zones)
                Plant.Root.DoWaterUptake(Z.Water, Z.Zone.Name);
        }

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            if (Plant.IsEmerged)
            {
                double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                for (int i = 0; i < Organs.Length; i++)
                    N.UptakeSupply[i] = 0;

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone.Zone);

                    UptakeDemands.NO3N = new double[zone.NO3N.Length];
                    UptakeDemands.NH4N = new double[zone.NH4N.Length];
                    UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                    //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                    for (int i = 0; i < Organs.Length; i++)
                        if (Organs[i] is IWaterNitrogenUptake)
                        {
                            double[] organNO3Supply = new double[zone.NO3N.Length];
                            double[] organNH4Supply = new double[zone.NH4N.Length];
                            (Organs[i] as IWaterNitrogenUptake).CalculateNitrogenSupply(zone, ref organNO3Supply, ref organNH4Supply);
                            UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                            UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);
                            N.UptakeSupply[i] += (MathUtilities.Sum(organNH4Supply) + MathUtilities.Sum(organNO3Supply)) * kgha2gsm * zone.Zone.Area / Plant.Zone.Area;
                            NSupply += (MathUtilities.Sum(organNH4Supply) + MathUtilities.Sum(organNO3Supply)) * zone.Zone.Area;
                        }
                    zones.Add(UptakeDemands);
                }

                double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * Plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                if (NSupply > NDemand)
                {
                    //Reduce the PotentialUptakes that we pass to the soil arbitrator
                    double ratio = Math.Min(1.0, NDemand / NSupply);
                    foreach (ZoneWaterAndN UptakeDemands in zones)
                    {
                        UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, ratio);
                        UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, ratio);
                    }
                }
                return zones;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetNUptake(List<ZoneWaterAndN> zones)
        {
            if (Plant.IsEmerged)
            {
                // Calculate the total no3 and nh4 across all zones.
                double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                foreach (ZoneWaterAndN Z in zones)
                    NSupply += (MathUtilities.Sum(Z.NO3N) + MathUtilities.Sum(Z.NH4N)) * Z.Zone.Area;

                //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
                for (int i = 0; i < Organs.Length; i++)
                    N.UptakeSupply[i] = NSupply / Plant.Zone.Area * N.UptakeSupply[i] / N.TotalUptakeSupply * kgha2gsm;

                //Allocate N that the SoilArbitrator has allocated the plant to each organ
                DoUptake(Organs, N, NArbitrator);
                Plant.Root.DoNitrogenUptake(zones);
            }
        }
        #endregion

        #region Plant interface methods

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { Clear(); }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                List<IArbitration> organsToArbitrate = new List<IArbitration>();
                foreach (IOrgan organ in Plant.Organs)
                    if (organ is IArbitration)
                        organsToArbitrate.Add(organ as IArbitration);

                Organs = organsToArbitrate.ToArray();
            }

        }

        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        private void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                //DM = BiomassArbitrationType.Create("DM", Organs);        //Get DM demands and supplies (with water stress effects included) from each organ
                DM = new BiomassArbitrationType();
                DM.DoSetup("DM", Organs);
                DoReAllocation(Organs, DM, DMArbitrator);         //Allocate supply of reallocated DM to organs
                DoFixation(Organs, DM, DMArbitrator);             //Allocate supply of fixed DM (photosynthesis) to organs
                DoRetranslocation(Organs, DM, DMArbitrator);      //Allocate supply of retranslocated DM to organs
                SendPotentialDMAllocations(Organs);                      //Tell each organ what their potential growth is so organs can calculate their N demands
                //N = BiomassArbitrationType.Create("N", Organs);
                N = new BiomassArbitrationType();
                N.DoSetup("N", Organs);
                DoReAllocation(Organs, N, NArbitrator);           //Allocate N available from reallocation to each organ
            }
        }


        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        private void OnDoActualPlantPartioning(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                DoFixation(Organs, N, NArbitrator);               //Allocate supply of fixable Nitrogen to each organ
                DoRetranslocation(Organs, N, NArbitrator);        //Allocate supply of retranslocatable N to each organ
                DoNutrientConstrainedDMAllocation(Organs);               //Work out how much DM can be assimilated by each organ based on allocated nutrients
                SendDMAllocations(Organs);                               //Tell each organ how DM they are getting folling allocation
                SendNutrientAllocations(Organs);                         //Tell each organ how much nutrient they are getting following allocaition
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            DM = new BiomassArbitrationType();
            N = new BiomassArbitrationType();
        }

        #endregion

        #region Arbitration step functions


        /// <summary>Sends the potential dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <exception cref="System.Exception">Mass Balance Error in Photosynthesis DM Allocation</exception>
        virtual public void SendPotentialDMAllocations(IArbitration[] Organs)
        {
            //  Allocate to meet Organs demands
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalNonStructuralAllocation;

            // Then check it all adds up
            if (Math.Round(DM.Allocated, 8) > Math.Round(DM.TotalPlantSupply, 8))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM supply.   Thats not really possible so something has gone a miss");
            if (Math.Round(DM.Allocated, 8) > Math.Round(DM.TotalPlantDemand, 8))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM Demand.   Thats not really possible so something has gone a miss");

            // Send potential DM allocation to organs to set this variable for calculating N demand
            for (int i = 0; i < Organs.Length; i++)
                Organs[i].DMPotentialAllocation = new BiomassPoolType
                {
                    Structural = DM.StructuralAllocation[i],  //Need to seperate metabolic and structural allocations
                    Metabolic = DM.MetabolicAllocation[i],  //This wont do anything currently
                    NonStructural = DM.NonStructuralAllocation[i], //Nor will this do anything
                };
        }

        /// <summary>Does the re allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The arbitrator.</param>
        virtual public void DoReAllocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassReallocated = 0;
            if (BAT.TotalReallocationSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);

                //Then calculate how much biomass is realloced from each supplying organ based on relative reallocation supply
                for (int i = 0; i < Organs.Length; i++)
                    if (BAT.ReallocationSupply[i] > 0)
                    {
                        double RelativeSupply = BAT.ReallocationSupply[i] / BAT.TotalReallocationSupply;
                        BAT.Reallocation[i] += BiomassReallocated * RelativeSupply;
                    }
                BAT.TotalReallocation = MathUtilities.Sum(BAT.Reallocation);
            }
        }

        /// <summary>Does the uptake.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        virtual public void DoUptake(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassTakenUp = 0;
            if (BAT.TotalUptakeSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                // Then calculate how much N is taken up by each supplying organ based on relative uptake supply
                for (int i = 0; i < Organs.Length; i++)
                    BAT.Uptake[i] += BiomassTakenUp * MathUtilities.Divide(BAT.UptakeSupply[i], BAT.TotalUptakeSupply, 0);
            }
        }

        /// <summary>Does the retranslocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        virtual public void DoRetranslocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassRetranslocated = 0;
            if (BAT.TotalRetranslocationSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                // Then calculate how much N (and associated biomass) is retranslocated from each supplying organ based on relative retranslocation supply
                for (int i = 0; i < Organs.Length; i++)
                    if (BAT.RetranslocationSupply[i] > 0.00000000001)
                    {
                        double RelativeSupply = BAT.RetranslocationSupply[i] / BAT.TotalRetranslocationSupply;
                        BAT.Retranslocation[i] += BiomassRetranslocated * RelativeSupply;
                    }
            }
        }

        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        /// <exception cref="System.Exception">Crop is trying to Fix excessive amounts of BAT.  Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic</exception>
        virtual public void DoFixation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassFixed = 0;
            if (BAT.TotalFixationSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);

                //Set the sink limitation variable.  BAT.NotAllocated changes after each allocation step so it must be caught here and assigned as sink limitation
                BAT.SinkLimitation = BAT.NotAllocated;

                // Then calculate how much resource is fixed from each supplying organ based on relative fixation supply
                if (BiomassFixed > 0)
                    for (int i = 0; i < Organs.Length; i++)
                    {
                        if (BAT.FixationSupply[i] > 0.00000000001)
                        {
                            double RelativeSupply = BAT.FixationSupply[i] / BAT.TotalFixationSupply;
                            BAT.Fixation[i] = BiomassFixed * RelativeSupply;
                            double Respiration = BiomassFixed * RelativeSupply * Organs[i].NFixationCost;  //Calculalte how much respirtion is associated with fixation
                            DM.Respiration[i] = Respiration; // allocate it to the organ
                        }
                        DM.TotalRespiration = MathUtilities.Sum(DM.Respiration);
                    }

                // Work out the amount of biomass (if any) lost due to the cost of N fixation
                if (DM.TotalRespiration <= DM.SinkLimitation)
                { } //Cost of N fixation can be met by DM supply that was not allocated
                else
                {//claw back todays NonStructuralDM allocation to cover the cost
                    double UnallocatedRespirationCost = DM.TotalRespiration - DM.SinkLimitation;
                    if (DM.TotalNonStructuralAllocation > 0)
                        for (int i = 0; i < Organs.Length; i++)
                        {
                            double proportion = DM.NonStructuralAllocation[i] / DM.TotalNonStructuralAllocation;
                            double Clawback = Math.Min(UnallocatedRespirationCost * proportion, DM.NonStructuralAllocation[i]);
                            DM.NonStructuralAllocation[i] -= Clawback;
                            UnallocatedRespirationCost -= Clawback;
                        }
                    if (UnallocatedRespirationCost == 0)
                    { }//All cost accounted for
                    else
                    {//Remobilise more Non-structural DM to cover the cost
                        if (DM.TotalRetranslocationSupply > 0)
                            for (int i = 0; i < Organs.Length; i++)
                            {
                                double proportion = DM.RetranslocationSupply[i] / DM.TotalRetranslocationSupply;
                                double DMRetranslocated = Math.Min(UnallocatedRespirationCost * proportion, DM.RetranslocationSupply[i]);
                                DM.Retranslocation[i] += DMRetranslocated;
                                UnallocatedRespirationCost -= DMRetranslocated;
                            }
                        if (UnallocatedRespirationCost == 0)
                        { }//All cost accounted for
                        else
                        {//Start cutting into Structural and Metabolic Allocations
                            if ((DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation) > 0)
                            {
                                double Costmet = 0;
                                for (int i = 0; i < Organs.Length; i++)
                                    if ((DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]) > 0)
                                    {
                                        double proportion = (DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]) / (DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation);
                                        double StructualFraction = DM.StructuralAllocation[i] / (DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]);
                                        double StructuralClawback = Math.Min(UnallocatedRespirationCost * proportion * StructualFraction, DM.StructuralAllocation[i]);
                                        double MetabolicClawback = Math.Min(UnallocatedRespirationCost * proportion * (1 - StructualFraction), DM.MetabolicAllocation[i]);
                                        DM.StructuralAllocation[i] -= StructuralClawback;
                                        DM.MetabolicAllocation[i] -= MetabolicClawback;
                                        Costmet += (StructuralClawback + MetabolicClawback);
                                    }
                                UnallocatedRespirationCost -= Costmet;
                            }
                        }
                        if (UnallocatedRespirationCost > 0.0000000001)
                            throw new Exception("Crop is trying to Fix excessive amounts of " + BAT.BiomassType + " Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic");
                    }
                }
            }
        }

        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void DoNutrientConstrainedDMAllocation(IArbitration[] Organs)
        {
            double PreNStressDMAllocation = DM.Allocated;
            for (int i = 0; i < Organs.Length; i++)
                N.TotalAllocation[i] = N.StructuralAllocation[i] + N.MetabolicAllocation[i] + N.NonStructuralAllocation[i];

            N.Allocated = MathUtilities.Sum(N.TotalAllocation);

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            for (int i = 0; i < Organs.Length; i++)
            {
                double TotalNDemand = N.StructuralDemand[i] + N.MetabolicDemand[i] + N.NonStructuralDemand[i];
                if (N.TotalAllocation[i] >= TotalNDemand)
                    N.ConstrainedGrowth[i] = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.TotalAllocation[i] == 0)
                    N.ConstrainedGrowth[i] = 0;
                else
                    N.ConstrainedGrowth[i] = N.TotalAllocation[i] / Organs[i].MinNconc;
            }

            // Reduce DM allocation below potential if insufficient N to reach Min n Conc or if DM was allocated to fixation
            for (int i = 0; i < Organs.Length; i++)
                if ((DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]) != 0)
                {
                    double MetabolicProportion = DM.MetabolicAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.NonStructuralAllocation[i]);
                    double StructuralProportion = DM.StructuralAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.NonStructuralAllocation[i]);
                    double NonStructuralProportion = DM.NonStructuralAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.NonStructuralAllocation[i]);
                    DM.MetabolicAllocation[i] = Math.Min(DM.MetabolicAllocation[i], N.ConstrainedGrowth[i] * MetabolicProportion);
                    DM.StructuralAllocation[i] = Math.Min(DM.StructuralAllocation[i], N.ConstrainedGrowth[i] * StructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    DM.NonStructuralAllocation[i] = Math.Min(DM.NonStructuralAllocation[i], N.ConstrainedGrowth[i] * NonStructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function

                    //Question.  Why do I not restrain non-structural DM allocations.  I think this may be wrong and require further thought HEB 15-1-2015
                }
            //Recalculated DM Allocation totals
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalNonStructuralAllocation;
            DM.NutrientLimitation = (PreNStressDMAllocation - DM.Allocated);
        }

        /// <summary>Sends the dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void SendDMAllocations(IArbitration[] Organs)
        {
            // Send DM allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
                Organs[i].DMAllocation = new BiomassAllocationType
                {
                    Respired = DM.Respiration[i],
                    Reallocation = DM.Reallocation[i],
                    Retranslocation = DM.Retranslocation[i],
                    Structural = DM.StructuralAllocation[i],
                    NonStructural = DM.NonStructuralAllocation[i],
                    Metabolic = DM.MetabolicAllocation[i],
                };
        }

        /// <summary>Sends the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void SendNutrientAllocations(IArbitration[] Organs)
        {
            // Send N allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
            {
                if ((N.StructuralAllocation[i] < -0.00000001) || (N.MetabolicAllocation[i] < -0.00000001) || (N.NonStructuralAllocation[i] < -0.00000001))
                    throw new Exception("-ve N Allocation");
                if (N.StructuralAllocation[i] < 0.0)
                    N.StructuralAllocation[i] = 0.0;
                if (N.MetabolicAllocation[i] < 0.0)
                    N.MetabolicAllocation[i] = 0.0;
                if (N.NonStructuralAllocation[i] < 0.0)
                    N.NonStructuralAllocation[i] = 0.0;
                Organs[i].NAllocation = new BiomassAllocationType
                {
                    Structural = N.StructuralAllocation[i], //This needs to be seperated into components
                    Metabolic = N.MetabolicAllocation[i],
                    NonStructural = N.NonStructuralAllocation[i],
                    Fixation = N.Fixation[i],
                    Reallocation = N.Reallocation[i],
                    Retranslocation = N.Retranslocation[i],
                    Uptake = N.Uptake[i]
                };
            }

            //Finally Check Mass balance adds up
            N.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                N.End += Organs[i].N;
            N.BalanceError = (N.End - (N.Start + N.TotalUptakeSupply + N.TotalFixationSupply));
            if (N.BalanceError > 0.000000001)
                throw new Exception("N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply");
            N.BalanceError = (N.End - (N.Start + N.TotalPlantDemand));
            if (N.BalanceError > 0.000000001)
                throw new Exception("N Mass balance violated!!!!  Daily Plant N increment is greater than N demand");
            DM.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                DM.End += Organs[i].Wt;
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalFixationSupply));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than Photosynthetic DM supply");
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalNonStructuralDemand));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and NonStructural DM capacity");
        }

        #endregion


        /// <summary>Partitions biomass between organs based on their relative demand in a single pass so non-structural always gets some if there is a non-structural demand</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        private void RelativeAllocationSinglePass(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////allocate to all pools based on their relative demands
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                double NonStructuralRequirement = Math.Max(0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement + NonStructuralRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.TotalStructuralDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand);
                    double MetabolicFraction = BAT.TotalMetabolicDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand);
                    double NonStructuralFraction = BAT.TotalNonStructuralDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand);

                    double StructuralAllocation = Math.Min(StructuralRequirement, TotalSupply * StructuralFraction * BAT.StructuralDemand[i] / BAT.TotalStructuralDemand);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TotalSupply * MetabolicFraction * MathUtilities.Divide(BAT.MetabolicDemand[i], BAT.TotalMetabolicDemand, 0));
                    double NonStructuralAllocation = Math.Min(NonStructuralRequirement, TotalSupply * NonStructuralFraction * MathUtilities.Divide(BAT.NonStructuralDemand[i], BAT.TotalNonStructuralDemand,0));

                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    BAT.NonStructuralAllocation[i] += Math.Max(0, NonStructuralAllocation);
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation + NonStructuralAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation + NonStructuralAllocation);
                }
            }
        }
    }
}