The Arbitrator class determines the allocation of total dry matter (DM) and Nitrogen components of Biomass between each of the organs in the crop model. Eacn organ potentially has three pools of biomass:
* Structural biomass which is fixed within an organ once it is partitioned to an organ
* Non-structural biomass which is available for re-translocation to other organs with high demand and is re-allocated to other organs when this organ senesces.
* Metabolic biomass which is generally fixed in an organ but is able to be reallocated and may be re-translocated in some cases.

The process followed for biomass arbitration are shown in Figure 1. Arbitration responds to events broadcast daily by the central APSIM infrastructure: 
1. **doPotentialPlantGrowth**.  When this event is broadcast the attached method executes code to determine the potential growth of each organ, the extent of moisture stress that a crop encounters and the potential biomass supplies and demands of each organ based on these.  In addition to demands for structural, non-structural and metabolic biomass (DM and N) each organ may have the following biomass supplies: 
	* Fixation supply.  From photosynthesis (DM) or symbiotic fixation (N)
	* Uptake supply.  Typically uptake of N from the soil by the roots but could be uptake by other organs.
	* Retranslocation supply.  Non-structural biomass that may be moved from one organ to meet demands of other organs
	* Reallocation supply. Biomass that can be moved from senescing organs to meet the demands of other organs.
	  
2. **doPotentialPlantPartitioning.** On this event the Arbitrator first executes the DoDMSetup() to establish the DM supplies and demands from each organ.  Then it executes the DoPotentialDMAllocation() method which works out how much biomass each organ would be allocated assuming N supply is not limiting and sends these allocations to the organs.  Each organ then uses their potential DM allocation to determine their N demand (how much N is needed to produce that much DM) and the arbitrator calls DoNSetup() establish N supplies and Demands and begin N arbitration.  Firstly DoNReallocation() is called to redistribute N that the plant has available from senescing organs.  After this step any unmet N demand is considered the plants demand for N uptake from the soil (N Uptake Demand).
3. **doNutrientArbitration** When this event is broadcast by the model framework the soil arbitrator gets the N uptake demands from each plant (where multiple plants are growing in competition) and their potential uptake from the soil and determines how nuch of their demand that the soil is able to provide.  This value is then passed back to each plant instance as their Nuptake and doNUptakeAllocation() is called to distribute this N between organs.  
4. **doActualPlantPartitioning**  On this event the arbitrator call DoNRetranslocation() and DoNFixation() to satisify any unmet N demands from these sources.  Finally, DoActualDMAllocation is called where DM allocations to each organ are reduced if the N allocation is insufficient to achieve the organs minimum N conentration and final allocations are sent to organs. 
* 
![Alt Text](C:\ApsimX\Documentation\Images\ArbitrationDiagram.PNG)

**Figure 1.**  Schematic showing procedure for biomass partitioning arbitration.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties, pink boxes are events that are broadcast each day by the model infrastructure and blue boxes are methods that are triggered by these events.

For both DM and then N the arbitrator steps through allocation of biomass from ReAllocaiton, Uptake, Retranslocation and then fixation.  Biomass is only allocated from subsequent sources if demands have not already been meet from the preceeding source.  

For each of the biomass supply sources arbitration is done in two passes.  On the first pass structural and metabilis biomass is allocated to each organ based on their demnad relative to the demand from all organs.  On the second pass any remaining biomass is allocated to non-structural demands based on the organs relative demand.

For each of the biomass supply sources arbitration is done in two passes.  On the first pass structural and metabilis biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand before the next organ is partitioned anything.   On the second pass any remaining biomass is allocated to non-structural demands based on the organs demand relative to the demand from all organs.

For each of the biomass supply sources arbitration is done in two passes.  On the first pass structural and metabilis biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand before the next organ is partitioned anything.  On the second pass any remaining biomass is allocated to non-structural demands based on the same order of priority.
