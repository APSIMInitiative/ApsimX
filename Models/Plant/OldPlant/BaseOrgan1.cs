using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// This class represents a base organ for a Plant15 model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant15))]
    abstract public class BaseOrgan1 : Model, Organ1
    {
        /// <summary>Gets or sets the live.</summary>
        /// <value>The live.</value>
        public Biomass Live 
        { 
            get 
            { 
                return Apsim.Child(this, "Live") as Biomass;
            }
            set
            {
                Live.SetTo(value);
            }
        }
        /// <summary>Gets or sets the dead.</summary>
        /// <value>The dead.</value>
        public Biomass Dead
        {
            get
            {
                return Apsim.Child(this, "Dead") as Biomass;
            }
            set
            {
                Dead.SetTo(value);
            }
        }

        

       // public abstract string Name { get; }
        /// <summary>Gets or sets the senescing.</summary>
        /// <value>The senescing.</value>
        [XmlIgnore]
        public abstract Biomass Senescing { get; protected set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        [XmlIgnore]
        public abstract Biomass Retranslocation { get; protected set; }
        /// <summary>Gets or sets the growth.</summary>
        /// <value>The growth.</value>
        [XmlIgnore]
        public abstract Biomass Growth { get; protected set; }
        /// <summary>Gets or sets the detaching.</summary>
        /// <value>The detaching.</value>
        [XmlIgnore]
        public abstract Biomass Detaching { get; protected set; }
        /// <summary>Gets or sets the green removed.</summary>
        /// <value>The green removed.</value>
        [XmlIgnore]
        public abstract Biomass GreenRemoved { get; protected set; }
        /// <summary>Gets or sets the senesced removed.</summary>
        /// <value>The senesced removed.</value>
        [XmlIgnore]
        public abstract Biomass SenescedRemoved { get; protected set; }

        // Soil water
        /// <summary>Gets the sw supply.</summary>
        /// <value>The sw supply.</value>
        public abstract double SWSupply { get; }
        /// <summary>Gets the sw demand.</summary>
        /// <value>The sw demand.</value>
        public abstract double SWDemand { get; }
        /// <summary>Gets the sw uptake.</summary>
        /// <value>The sw uptake.</value>
        public abstract double SWUptake { get; }
        /// <summary>Does the sw demand.</summary>
        /// <param name="Supply">The supply.</param>
        public abstract void DoSWDemand(double Supply);
        /// <summary>Does the sw uptake.</summary>
        /// <param name="SWDemand">The sw demand.</param>
        public abstract void DoSWUptake(double SWDemand);


        // dry matter
        /// <summary>Gets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public abstract double DMSupply { get; }
        /// <summary>Gets the dm retrans supply.</summary>
        /// <value>The dm retrans supply.</value>
        public abstract double DMRetransSupply { get; }
        /// <summary>Gets the DLT dm pot rue.</summary>
        /// <value>The DLT dm pot rue.</value>
        public abstract double dltDmPotRue { get; }
        /// <summary>Gets the dm green demand.</summary>
        /// <value>The dm green demand.</value>
        public abstract double DMGreenDemand { get; }
        /// <summary>Gets the dm demand differential.</summary>
        /// <value>The dm demand differential.</value>
        public abstract double DMDemandDifferential { get; }
        /// <summary>Does the dm demand.</summary>
        /// <param name="DMSupply">The dm supply.</param>
        public abstract void DoDMDemand(double DMSupply);
        /// <summary>Does the dm retranslocate.</summary>
        /// <param name="dlt_dm_retrans_to_fruit">The dlt_dm_retrans_to_fruit.</param>
        /// <param name="demand_differential_begin">The demand_differential_begin.</param>
        public abstract void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin);
        /// <summary>Gives the dm green.</summary>
        /// <param name="Delta">The delta.</param>
        public abstract void GiveDmGreen(double Delta);
        /// <summary>Does the senescence.</summary>
        public abstract void DoSenescence();
        /// <summary>Does the detachment.</summary>
        public abstract void DoDetachment();
        /// <summary>Removes the biomass.</summary>
        public abstract void RemoveBiomass();

        // nitrogen
        /// <summary>Gets the n supply.</summary>
        /// <value>The n supply.</value>
        public abstract double NSupply { get; }
        /// <summary>Gets the n demand.</summary>
        /// <value>The n demand.</value>
        public abstract double NDemand { get; }
        /// <summary>Gets the n uptake.</summary>
        /// <value>The n uptake.</value>
        public abstract double NUptake { get; }
        /// <summary>Gets the soil n demand.</summary>
        /// <value>The soil n demand.</value>
        public abstract double SoilNDemand { get; }
        /// <summary>Gets the n capacity.</summary>
        /// <value>The n capacity.</value>
        public abstract double NCapacity { get; }
        /// <summary>Gets the n demand differential.</summary>
        /// <value>The n demand differential.</value>
        public abstract double NDemandDifferential { get; }
        /// <summary>Gets the available retranslocate n.</summary>
        /// <value>The available retranslocate n.</value>
        public abstract double AvailableRetranslocateN { get; }
        /// <summary>Gets the DLT n senesced retrans.</summary>
        /// <value>The DLT n senesced retrans.</value>
        public abstract double DltNSenescedRetrans { get; }
        /// <summary>Does the n demand.</summary>
        /// <param name="IncludeRetranslocation">if set to <c>true</c> [include retranslocation].</param>
        public abstract void DoNDemand(bool IncludeRetranslocation);
        /// <summary>Does the n demand1 pot.</summary>
        /// <param name="dltDmPotRue">The DLT dm pot rue.</param>
        public abstract void DoNDemand1Pot(double dltDmPotRue);
        /// <summary>Does the soil n demand.</summary>
        public abstract void DoSoilNDemand();
        /// <summary>Does the n supply.</summary>
        public abstract void DoNSupply();
        /// <summary>Does the n retranslocate.</summary>
        /// <param name="availableRetranslocateN">The available retranslocate n.</param>
        /// <param name="GrainNDemand">The grain n demand.</param>
        public abstract void DoNRetranslocate(double availableRetranslocateN, double GrainNDemand);
        /// <summary>Does the n senescence.</summary>
        public abstract void DoNSenescence();
        /// <summary>Does the n senesced retranslocation.</summary>
        /// <param name="navail">The navail.</param>
        /// <param name="n_demand_tot">The n_demand_tot.</param>
        public abstract void DoNSenescedRetranslocation(double navail, double n_demand_tot);
        /// <summary>Does the n partition.</summary>
        /// <param name="GrowthN">The growth n.</param>
        public abstract void DoNPartition(double GrowthN);
        /// <summary>Does the n fix retranslocate.</summary>
        /// <param name="NFixUptake">The n fix uptake.</param>
        /// <param name="nFixDemandTotal">The n fix demand total.</param>
        public abstract void DoNFixRetranslocate(double NFixUptake, double nFixDemandTotal);
        /// <summary>Does the n conccentration limits.</summary>
        public abstract void DoNConccentrationLimits();
        /// <summary>Zeroes the DLT n senesced trans.</summary>
        public abstract void ZeroDltNSenescedTrans();
        /// <summary>Does the n uptake.</summary>
        /// <param name="PotNFix">The pot n fix.</param>
        public abstract void DoNUptake(double PotNFix);

        // cover
        /// <summary>Gets or sets the cover green.</summary>
        /// <value>The cover green.</value>
        [XmlIgnore]
        public abstract double CoverGreen { get; protected set; }
        /// <summary>Gets or sets the cover sen.</summary>
        /// <value>The cover sen.</value>
        [XmlIgnore]
        public abstract double CoverSen { get; protected set; }
        /// <summary>Does the potential rue.</summary>
        public abstract void DoPotentialRUE();
        /// <summary>Intercepts the radiation.</summary>
        /// <param name="incomingSolarRadiation">The incoming solar radiation.</param>
        /// <returns></returns>
        public abstract double interceptRadiation(double incomingSolarRadiation);
        /// <summary>Does the cover.</summary>
        public abstract void DoCover();

        // update
        /// <summary>Updates this instance.</summary>
        public abstract void Update();

        // events
        /// <summary>Called when [prepare].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public abstract void OnPrepare(object sender, EventArgs e);
        /// <summary>Called when [harvest].</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        public abstract void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved);
        /// <summary>Called when [end crop].</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        public abstract void OnEndCrop(BiomassRemovedType BiomassRemoved);

        // grazing
        /// <summary>Gets the available to animal.</summary>
        /// <value>The available to animal.</value>
        public abstract AvailableToAnimalelementType[] AvailableToAnimal { get; }
        /// <summary>Sets the removed by animal.</summary>
        /// <value>The removed by animal.</value>
        public abstract RemovedByAnimalType RemovedByAnimal { set; }
        
    }
}