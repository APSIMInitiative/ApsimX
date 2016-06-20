using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using Models.PMF.Functions;


namespace Models.PMF.Organs
{
    /// <summary>
    /// This class represents a base organ
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class BaseOrgan : Model, IOrgan, IArbitration
    {
        #region Links to other models or compontnets
        /// <summary>The live</summary>
        [Link] [DoNotDocument] public Biomass Live = null;
        
        /// <summary>The dead</summary>
        [Link] [DoNotDocument] public Biomass Dead = null;
        
        /// <summary>The clock</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;

        /// <summary>The plant</summary>
        [Link]
        protected Plant Plant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        protected ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The summary</summary>
        [Link]
        protected ISummary Summary = null;
        #endregion

        #region Arbitration methods
        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        [XmlIgnore]
        virtual public BiomassSupplyType DMSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        [XmlIgnore]
        virtual public BiomassPoolType DMPotentialAllocation { set { } }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        [XmlIgnore]
        virtual public BiomassAllocationType DMAllocation { set { } }
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        [XmlIgnore]
        virtual public BiomassPoolType DMDemand { get { return new BiomassPoolType(); } set { } }

        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        virtual public BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        [XmlIgnore]
        virtual public BiomassAllocationType NAllocation { set { } }
        /// <summary>Gets or sets the n fixation cost.</summary>
        /// <value>The n fixation cost.</value>
        [XmlIgnore]
        virtual public double NFixationCost { get { return 0; } set { } }
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        [XmlIgnore]
        virtual public BiomassPoolType NDemand { get { return new BiomassPoolType(); } set { } }
        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        [XmlIgnore]
        virtual public double MaxNconc { get { return 0; } set { } }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        [XmlIgnore]
        virtual public double MinNconc { get { return 0; } set { } }
         #endregion

        #region Soil Arbitrator interface
        /// <summary>Gets the NO3 supply for the given N state.</summary>
        virtual public double[] NO3NSupply(List<ZoneWaterAndN> zones) { return null; }

        /// <summary>Gets the NH4 supply for the given N state.</summary>
        virtual public double[] NH4NSupply(List<ZoneWaterAndN> zones) { return null; }

        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        virtual public double WaterDemand { get { return 0; } set { } }

        /// <summary>Gets the water supply for the given water state.</summary>
        virtual public double[] WaterSupply(List<ZoneWaterAndN> zones) { return null; }

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        /// <exception cref="System.Exception">Cannot set water uptake for  + Name</exception>
        [XmlIgnore]
        virtual public double WaterUptake
        {
            get { return 0; }
            set { throw new Exception("Cannot set water uptake for " + Name); }
        }

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        /// <exception cref="System.Exception">Cannot set water uptake for  + Name</exception>
        [XmlIgnore]
        virtual public double NUptake
        {
            get { return 0; }
            set { throw new Exception("Cannot set water uptake for " + Name); }
        }
        
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        /// <exception cref="System.Exception">Cannot set water allocation for  + Name</exception>
        [XmlIgnore]
        virtual public double WaterAllocation
        {
            get { return 0; }
            set { throw new Exception("Cannot set water allocation for " + Name); }
        }
        /// <summary>Does the water uptake.</summary>
        /// <param name="uptake">The uptake.</param>
        virtual public void DoWaterUptake(double[] uptake) { }
        
        /// <summary>Does the N uptake.</summary>
        /// <param name="NO3NUptake">The NO3NUptake.</param>
        /// <param name="NH4Uptake">The NH4Uptake.</param>
        virtual public void DoNitrogenUptake(double[] NO3NUptake, double[] NH4Uptake) { }
                        
        /// <summary>Gets the n supply uptake.</summary>
        /// <value>The n supply uptake.</value>
        [Units("g/m^2")]
        virtual public double NSupplyUptake { get { return NSupply.Uptake; } }
        #endregion

        #region Organ properties

        /// <summary>Gets the total (live + dead) dm (g/m2)</summary>
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) n (g/m2)</summary>
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the dm amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public double DetachedWt { get; set; }

        /// <summary>Gets the N amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public double DetachedN { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public double RemovedWt { get; set; }

        /// <summary>Gets the N amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public double RemovedN { get; set; }

        /// <summary>Gets the dm supply photosynthesis.</summary>
        /// <value>The dm supply photosynthesis.</value>
        [Units("g/m^2")]
        virtual public double DMSupplyPhotosynthesis { get { return DMSupply.Fixation; } }

        #endregion

        #region Biomass removal
        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="value">The fractions of biomass to remove</param>
        virtual public void DoRemoveBiomass(OrganBiomassRemovalType value)
        {
            double totalFractionToRemove = value.FractionLiveToRemove + value.FractionDeadToRemove
                                           + value.FractionLiveToResidue + value.FractionDeadToResidue;
            if (totalFractionToRemove > 1.0)
            {
                throw new Exception("The sum of FractionToResidue and FractionToRemove sent with your "
                                    + "!!!!PLACE HOLDER FOR EVENT SENDER!!!!"
                                    + " is greater than 1.  Had this execption not triggered you would be removing more biomass from "
                                    + Name + " than there is to remove");
            }
            else  if (totalFractionToRemove > 0.0)
            {
                double detachingWt = Live.Wt * value.FractionLiveToResidue + Dead.Wt * value.FractionDeadToResidue;
                double detachingN = Live.N * value.FractionLiveToResidue + Dead.N * value.FractionDeadToResidue;
                double removingWt = Live.Wt * value.FractionLiveToRemove + Dead.Wt * value.FractionDeadToRemove;
                double removingN = Live.N * value.FractionLiveToRemove + Dead.N * value.FractionDeadToRemove;

                Live.StructuralWt *= (1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove));
                Dead.StructuralWt *= (1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove));
                Live.NonStructuralWt *= (1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove));
                Dead.NonStructuralWt *= (1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove));
                Live.MetabolicWt *= (1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove));
                Dead.MetabolicWt *= (1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove));

                DetachedN += Live.N * value.FractionLiveToResidue + Dead.N * value.FractionDeadToResidue;
                RemovedN += Live.N * value.FractionLiveToRemove + Dead.N * value.FractionDeadToRemove;
                Live.StructuralN *= (1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove));
                Dead.StructuralN *= (1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove));
                Live.NonStructuralN *= (1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove));
                Dead.NonStructuralN *= (1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove));
                Live.MetabolicN *= (1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove));
                Dead.MetabolicN *= (1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove));

                SurfaceOrganicMatter.Add(detachingWt * 10, detachingN * 10, 0.0, Plant.CropType, Name);
                //TODO: theoretically the dead material is different from the live, so it should be added as a separate pool to SurfaceOM

                DetachedWt += detachingWt;
                DetachedN += detachingN;
                RemovedWt += removingWt;
                RemovedN += removingN;

                double toResidue = (value.FractionLiveToResidue + value.FractionDeadToResidue) / totalFractionToRemove * 100;
                double removedOff = (value.FractionLiveToRemove + value.FractionDeadToRemove) / totalFractionToRemove * 100;
                Summary.WriteMessage(this, "Removing " + (totalFractionToRemove * 100).ToString("0.0")
                                         + "% of " + Name + " Biomass from " + Plant.Name
                                         + ".  Of this " + removedOff.ToString("0.0") + "% is removed from the system and "
                                         + toResidue.ToString("0.0") + "% is returned to the surface organic matter");
            }
        }

        #endregion

        #region Management event methods

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
                DoDailyCleanup();
        }

        /// <summary>Called when crop is ending</summary>
        ///[EventSubscribe("PlantEnding")]
        virtual public void DoPlantEnding()
        {
            if (Wt > 0.0)
            {
                DetachedWt += Wt;
                DetachedN += N;
                SurfaceOrganicMatter.Add(Wt * 10, N * 10, 0, Plant.CropType, Name);
            }

            Clear();
        }

        /// <summary>
        /// Do harvest logic for this organ
        /// </summary>
        virtual public void DoHarvest() { }

        /// <summary>
        /// Do Cutting logic for this organ
        /// </summary>
        virtual public void DoCut() { }

        /// <summary>
        /// Do Graze logic for this organ
        /// </summary>
        virtual public void DoGraze() { }

        /// <summary>
        /// Do prune logic for this organ
        /// </summary>
        virtual public void DoPrune() { }
        #endregion
        
        #region Organ functions
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            // write a list of constant functions.
            foreach (IModel child in Apsim.Children(this, typeof(Constant)))
                child.Document(tags, -1, indent);

            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (child is Constant || child is Biomass || child is CompositeBiomass || child is ArrayBiomass)
                {
                    // don't document.
                }
                else
                    child.Document(tags, headingLevel + 1, indent);
            }
        }
        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            Live.Clear();
            Dead.Clear();
        }

        /// <summary>Does the zeroing of some varibles.</summary>
        virtual protected void DoDailyCleanup()
        {
            DetachedWt = 0.0;
            DetachedN = 0.0;
            RemovedWt = 0.0;
            RemovedN = 0.0;
        }

        /// <summary>Does the potential nutrient.</summary>
        virtual public void DoPotentialNutrient() { }
        #endregion
    }
}