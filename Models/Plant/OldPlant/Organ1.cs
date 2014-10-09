using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// A base organ for plant15
    /// </summary>
    public interface Organ1
    {
        /// <summary>Gets the name.</summary>
        /// <value>The name.</value>
        string Name { get; }
        /// <summary>Gets the live.</summary>
        /// <value>The live.</value>
        Biomass Live { get; }
        /// <summary>Gets the dead.</summary>
        /// <value>The dead.</value>
        Biomass Dead { get; }
        /// <summary>Gets the senescing.</summary>
        /// <value>The senescing.</value>
        Biomass Senescing { get; }
        /// <summary>Gets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        Biomass Retranslocation { get; }
        /// <summary>Gets the growth.</summary>
        /// <value>The growth.</value>
        Biomass Growth { get; }
        /// <summary>Gets the detaching.</summary>
        /// <value>The detaching.</value>
        Biomass Detaching { get; }
        /// <summary>Gets the green removed.</summary>
        /// <value>The green removed.</value>
        Biomass GreenRemoved { get; }
        /// <summary>Gets the senesced removed.</summary>
        /// <value>The senesced removed.</value>
        Biomass SenescedRemoved { get; }

        // Soil water
        /// <summary>Gets the sw supply.</summary>
        /// <value>The sw supply.</value>
        double SWSupply { get; }
        /// <summary>Gets the sw demand.</summary>
        /// <value>The sw demand.</value>
        double SWDemand { get; }
        /// <summary>Gets the sw uptake.</summary>
        /// <value>The sw uptake.</value>
        double SWUptake { get; }
        /// <summary>Does the sw demand.</summary>
        /// <param name="Supply">The supply.</param>
        void DoSWDemand(double Supply);
        /// <summary>Does the sw uptake.</summary>
        /// <param name="SWDemand">The sw demand.</param>
        void DoSWUptake(double SWDemand);


        // dry matter
        /// <summary>Gets the dm supply.</summary>
        /// <value>The dm supply.</value>
        double DMSupply { get; }
        /// <summary>Gets the dm retrans supply.</summary>
        /// <value>The dm retrans supply.</value>
        double DMRetransSupply { get; }
        /// <summary>Gets the DLT dm pot rue.</summary>
        /// <value>The DLT dm pot rue.</value>
        double dltDmPotRue { get; }
        /// <summary>Gets the dm green demand.</summary>
        /// <value>The dm green demand.</value>
        double DMGreenDemand { get; }
        /// <summary>Gets the dm demand differential.</summary>
        /// <value>The dm demand differential.</value>
        double DMDemandDifferential { get; }
        /// <summary>Does the dm demand.</summary>
        /// <param name="DMSupply">The dm supply.</param>
        void DoDMDemand(double DMSupply);
        /// <summary>Does the dm retranslocate.</summary>
        /// <param name="dlt_dm_retrans_to_fruit">The dlt_dm_retrans_to_fruit.</param>
        /// <param name="demand_differential_begin">The demand_differential_begin.</param>
        void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin);
        /// <summary>Gives the dm green.</summary>
        /// <param name="Delta">The delta.</param>
        void GiveDmGreen(double Delta);
        /// <summary>Does the senescence.</summary>
        void DoSenescence();
        /// <summary>Does the detachment.</summary>
        void DoDetachment();
        /// <summary>Removes the biomass.</summary>
        void RemoveBiomass();

        // nitrogen
        /// <summary>Gets the n supply.</summary>
        /// <value>The n supply.</value>
        double NSupply { get; }
        /// <summary>Gets the n demand.</summary>
        /// <value>The n demand.</value>
        double NDemand { get; }
        /// <summary>Gets the n uptake.</summary>
        /// <value>The n uptake.</value>
        double NUptake { get; }
        /// <summary>Gets the soil n demand.</summary>
        /// <value>The soil n demand.</value>
        double SoilNDemand { get; }
        /// <summary>Gets the n capacity.</summary>
        /// <value>The n capacity.</value>
        double NCapacity { get; }
        /// <summary>Gets the n demand differential.</summary>
        /// <value>The n demand differential.</value>
        double NDemandDifferential { get; }
        /// <summary>Gets the available retranslocate n.</summary>
        /// <value>The available retranslocate n.</value>
        double AvailableRetranslocateN { get; }
        /// <summary>Gets the DLT n senesced retrans.</summary>
        /// <value>The DLT n senesced retrans.</value>
        double DltNSenescedRetrans { get; }
        /// <summary>Does the n demand.</summary>
        /// <param name="IncludeRetranslocation">if set to <c>true</c> [include retranslocation].</param>
        void DoNDemand(bool IncludeRetranslocation);
        /// <summary>Does the n demand1 pot.</summary>
        /// <param name="dltDmPotRue">The DLT dm pot rue.</param>
        void DoNDemand1Pot(double dltDmPotRue);
        /// <summary>Does the soil n demand.</summary>
        void DoSoilNDemand();
        /// <summary>Does the n supply.</summary>
        void DoNSupply();
        /// <summary>Does the n retranslocate.</summary>
        /// <param name="availableRetranslocateN">The available retranslocate n.</param>
        /// <param name="GrainNDemand">The grain n demand.</param>
        void DoNRetranslocate(double availableRetranslocateN, double GrainNDemand);
        /// <summary>Does the n senescence.</summary>
        void DoNSenescence();
        /// <summary>Does the n senesced retranslocation.</summary>
        /// <param name="navail">The navail.</param>
        /// <param name="n_demand_tot">The n_demand_tot.</param>
        void DoNSenescedRetranslocation(double navail, double n_demand_tot);
        /// <summary>Does the n partition.</summary>
        /// <param name="GrowthN">The growth n.</param>
        void DoNPartition(double GrowthN);
        /// <summary>Does the n fix retranslocate.</summary>
        /// <param name="NFixUptake">The n fix uptake.</param>
        /// <param name="nFixDemandTotal">The n fix demand total.</param>
        void DoNFixRetranslocate(double NFixUptake, double nFixDemandTotal);
        /// <summary>Does the n conccentration limits.</summary>
        void DoNConccentrationLimits();
        /// <summary>Zeroes the DLT n senesced trans.</summary>
        void ZeroDltNSenescedTrans();
        /// <summary>Does the n uptake.</summary>
        /// <param name="PotNFix">The pot n fix.</param>
        void DoNUptake(double PotNFix);

        // cover
        /// <summary>Gets the cover green.</summary>
        /// <value>The cover green.</value>
        double CoverGreen { get; }
        /// <summary>Gets the cover sen.</summary>
        /// <value>The cover sen.</value>
        double CoverSen { get; }
        /// <summary>Does the potential rue.</summary>
        void DoPotentialRUE();
        /// <summary>Intercepts the radiation.</summary>
        /// <param name="incomingSolarRadiation">The incoming solar radiation.</param>
        /// <returns></returns>
        double interceptRadiation(double incomingSolarRadiation);
        /// <summary>Does the cover.</summary>
        void DoCover();

        // update
        /// <summary>Updates this instance.</summary>
        void Update();

        // events
        /// <summary>Called when [prepare].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void OnPrepare(object sender, EventArgs e);
        /// <summary>Called when [harvest].</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved);
        /// <summary>Called when [end crop].</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        void OnEndCrop(BiomassRemovedType BiomassRemoved);

        // grazing
        /// <summary>Gets the available to animal.</summary>
        /// <value>The available to animal.</value>
        AvailableToAnimalelementType[] AvailableToAnimal { get; }
        /// <summary>Sets the removed by animal.</summary>
        /// <value>The removed by animal.</value>
       RemovedByAnimalType RemovedByAnimal { set; }
    }
}