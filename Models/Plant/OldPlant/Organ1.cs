using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;

namespace Models.PMF.OldPlant
{
    public interface Organ1
    {
        string Name { get; }
        Biomass Live { get; }
        Biomass Dead { get; }
        Biomass Senescing { get; }
        Biomass Retranslocation { get; }
        Biomass Growth { get; }
        Biomass Detaching { get; }
        Biomass GreenRemoved { get; }
        Biomass SenescedRemoved { get; }

        // Soil water
        double SWSupply { get; }
        double SWDemand { get; }
        double SWUptake { get; }
        void DoSWDemand(double Supply);
        void DoSWUptake(double SWDemand);


        // dry matter
        double DMSupply { get; }
        double DMRetransSupply { get; }
        double dltDmPotRue { get; }
        double DMGreenDemand { get; }
        double DMDemandDifferential { get; }
        void DoDMDemand(double DMSupply);
        void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin);
        void GiveDmGreen(double Delta);
        void DoSenescence();
        void DoDetachment();
        void RemoveBiomass();

        // nitrogen
        double NSupply { get; }
        double NDemand { get; }
        double NUptake { get; }
        double SoilNDemand { get; }
        double NCapacity { get; }
        double NDemandDifferential { get; }
        double AvailableRetranslocateN { get; }
        double DltNSenescedRetrans { get; }
        void DoNDemand(bool IncludeRetranslocation);
        void DoNDemand1Pot(double dltDmPotRue);
        void DoSoilNDemand();
        void DoNSupply();
        void DoNRetranslocate(double availableRetranslocateN, double GrainNDemand);
        void DoNSenescence();
        void DoNSenescedRetranslocation(double navail, double n_demand_tot);
        void DoNPartition(double GrowthN);
        void DoNFixRetranslocate(double NFixUptake, double nFixDemandTotal);
        void DoNConccentrationLimits();
        void ZeroDltNSenescedTrans();
        void DoNUptake(double PotNFix);

        // cover
        double CoverGreen { get; }
        double CoverSen { get; }
        void DoPotentialRUE();
        double interceptRadiation(double incomingSolarRadiation);
        void DoCover();

        // update
        void Update();

        // events
        void OnPrepare(object sender, EventArgs e);
        void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved);
        void OnEndCrop(BiomassRemovedType BiomassRemoved);

        // grazing
        AvailableToAnimalelementType[] AvailableToAnimal { get; }
       RemovedByAnimalType RemovedByAnimal { set; }
    }
}