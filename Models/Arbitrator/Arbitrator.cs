using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF;
using Models.Soils;

namespace Models.Arbitrator
{

    /// <summary>
    /// Vals u-beaut soil arbitrator
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Arbitrator : Model
    {

        [Link]
        Soils.Soil Soil = null;

        [Link]
        WeatherFile Weather = null;

        ICrop2[] plants ;

        // Plant variables
        double[] potentialEP;        
        double[] actualEP;
        double[,] actualEPPlantLayer;
        double totalSWDemand;
        public double[] scalerPlant {get; set;}  //


        // Soil variables
        public double[] dltSWdep { get; set; }
        double[,] swUptakePlantLayer;
        double[,] availWaterDepPlantLayer ;
        double[] availWaterDepPlant ;
        double[] availWaterDepLayer ;
        double availWaterDepSum ;
        double[] totalAvailWaterDepLayer ;  
        public double[] scalerLayer {get; set;}  //how much to scale back the water uptake because there is not enough water in the layer to staisfy

        // Plant nitrogen variables
        double[] potentialNitrogenDemand;
        double totalNitrogenDemand;
        double[] actualNitrogenSupply;
        public double[] scalerPlantNitrogen { get; set; }  //

        // Soil nitrate variables
        public double[] dltNO3 { get; set; }
        double[,] no3UptakePlantLayer;
        double[,] availNO3PlantLayer;
        double[] availNO3Plant;
        double[] availNO3Layer;
        double availNO3Sum;
        double[] totalAvailNO3Layer;  
        public double[] scalerLayerNitrogen { get; set; }  

        // Soil ammonium variables
        public double[] dltNH4 { get; set; }
        double[,] NH4UptakePlantLayer;
        double[,] availNH4PlantLayer;
        double[] availNH4Plant;
        double[] availNH4Layer;
        double availNH4Sum;
        double[] totalAvailNH4Layer;

        double[,] availNitrogenPlantLayer;
        double[] availNitrogenPlant;
        double[] availNitrogenLayer;

        double[,] actualNitrogenSupplyPlantLayer;
        public double[] dltNitrogen { get; set; }
        
        
        // soil water evaporation stuff
        public double ArbitEOS { get; set; }  //

        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        public event NitrogenChangedDelegate NitrogenChanged;

                
        //public double[]  { get; set; }

        class CanopyProps
        {
            public double laiGreen;
            public double laiTotal;
        }
        //public CanopyProps[,] myCanopy;

        public override void OnSimulationCommencing()
        {
            // this is init
            //plants = (ICrop2[])this.Children.MatchingMultiple(typeof(ICrop2));
            plants = this.Plants;

            // myCanopy = new CanopyProps[plants.Length, 0];  // later make this the layering in the canopy
            // for (int i = 0; i < plants.Length; i++)
            // {
            //    myCanopy[0, 0].laiGreen = plants[i].CanopyProperties.LAI;
            // }
            

            actualEP = new double[plants.Length];
            actualEPPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            potentialEP = new double[plants.Length];

            potentialNitrogenDemand = new double[plants.Length];
            actualNitrogenSupply = new double[plants.Length];
            actualNitrogenSupplyPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];

            scalerPlant = new double[plants.Length];  //
            scalerPlantNitrogen = new double[plants.Length];  //

            // Soil variables
            swUptakePlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            availWaterDepPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            availWaterDepPlant = new double[plants.Length];
            availWaterDepLayer = new double[Soil.SoilWater.dlayer.Length];
            
            totalAvailWaterDepLayer = new double[Soil.SoilWater.dlayer.Length];  //water between sw and ll15 - temporary solution
            scalerLayer = new double[Soil.SoilWater.dlayer.Length];  //how much to scale back the water uptake because there is not enough water in the layer to staisfy
            dltSWdep = new double[Soil.SoilWater.dlayer.Length];

            actualNitrogenSupplyPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            availNitrogenPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            availNitrogenPlant = new double[plants.Length];
            availNitrogenLayer = new double[Soil.SoilWater.dlayer.Length];

            //totalAvailNitrogenLayer = new double[Soil.SoilWater.dlayer.Length];  
            scalerLayerNitrogen = new double[Soil.SoilWater.dlayer.Length];
            dltNO3 = new double[Soil.SoilWater.dlayer.Length];
            dltNH4 = new double[Soil.SoilWater.dlayer.Length];
            dltNitrogen = new double[Soil.SoilWater.dlayer.Length];
        }


        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            Utility.Math.Zero(actualEP);
            Utility.Math.Zero(actualEPPlantLayer);
            Utility.Math.Zero(potentialEP);
            totalSWDemand=0.0;
            Utility.Math.Zero(swUptakePlantLayer);
            Utility.Math.Zero(scalerPlant);
            Utility.Math.Zero(availWaterDepPlantLayer);
            Utility.Math.Zero(availWaterDepPlant);
            Utility.Math.Zero(availWaterDepLayer);
            availWaterDepSum = 0.0;
            Utility.Math.Zero(totalAvailWaterDepLayer);
            Utility.Math.Zero(scalerLayer);
            Utility.Math.Zero(dltSWdep);

            Utility.Math.Zero(potentialNitrogenDemand);
            Utility.Math.Zero(actualNitrogenSupply);
            Utility.Math.Zero(actualNitrogenSupplyPlantLayer);
            Utility.Math.Zero(scalerPlantNitrogen);
            Utility.Math.Zero(dltNO3);
            Utility.Math.Zero(availNO3Plant);
            Utility.Math.Zero(availNO3Layer);
            Utility.Math.Zero(totalAvailNO3Layer);
            Utility.Math.Zero(scalerLayerNitrogen);
            Utility.Math.Zero(dltNH4);
            Utility.Math.Zero(availNH4Plant);
            Utility.Math.Zero(availNH4Layer);
            Utility.Math.Zero(totalAvailNH4Layer);

            Utility.Math.Zero(no3UptakePlantLayer);
            Utility.Math.Zero(availNO3PlantLayer);
            Utility.Math.Zero(NH4UptakePlantLayer);
            Utility.Math.Zero(availNH4PlantLayer);
            Utility.Math.Zero(availNitrogenPlantLayer);
            Utility.Math.Zero(availNitrogenPlant);
            Utility.Math.Zero(availNitrogenLayer);
            Utility.Math.Zero(dltNitrogen);

            

            totalNitrogenDemand = 0.0;
            availNO3Sum = 0.0;
            availNH4Sum = 0.0;
        }

        [EventSubscribe("DoEnergyArbitration")]
        private void OnDoEnergyArbitration(object sender, EventArgs e)
        {
            //ToDO
            // Soil water evaporation
            //

            // i is for plants
            // j is for layers in the canopy - layers are from the top downwards

            // THIS NEEDS TO GO ONCE THE PROPER STUFF IS IN HERE
            for (int i = 0; i < plants.Length; i++)
            {
                //plants[i].PotentialEP = 5.0;
                ArbitEOS = 0.0;  // need to set EOS but doesnot seem to be effective
            }

            //Agenda
            //?when does rainfall and irrigation interception happen? - deal with this later!
            // break the canopy into layers
            // calculate the light profile down the canopy and to the soil surface
            //      - available to crops for growth
            //      - radiation to soil surface goes to SoilTemperature for heat calculations
            // calculate the Penman-Monteith potential evapotranspiration for each compartment (species x canopy layer) and potential soil water evaporation
            //      - ? crops should have supplied the non-water effects of stomatal conductance by this time (based on yesterday's states) - check
            //      - send the soil water transpiration demand back to the crops and the soil water evaporative demand to the soil water balance
            // consistent with the 2004 documentation, interception of irrigation is not considered in Arbitrator

            // Get the canopy height and depth information and break it into layers and compoents
            // FOR NOW WILL ONLY DEAL WITH A SINGLE LAYER - HEIGHT IS THE MAXIMUM HEIGHT AND DEPTH = HEIGHT
            // Create an array to hold the properties

            for (int i = 0; i < plants.Length; i++)
            {
                for (int j = 0; j < plants.Length; j++)
                {
                }
            }



        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            //ToDO
            // Actual soil water evaporation
            //




            // use i for the plant loop and j for the layer loop
 
            // calculate the potentially available water and sum the demand
            for (int i = 0 ; i<plants.Length; i++)
            {
                potentialEP[i] = plants[i].PotentialEP; // note that eventually PotentialEP will be calculated above in the EnergyArbitration 
                totalSWDemand += potentialEP[i];
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {

                    double temp = (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LowerLimitDep[j]) * plants[i].RootProperties.KL[j] * plants[i].RootProperties.RootExplorationByLayer[j];
                    availWaterDepPlantLayer[i, j] = Math.Max(temp, 0.0);  // 

                    // sum by plant for convenience
                    availWaterDepPlant[i] += availWaterDepPlantLayer[i, j];
               }
                // if this was the only plant in the system, would there be enough water to satify demand?  If yes then scaler = 1 otherwise < 1
                // when it comes to uptake then this scaler gets applied across all layers for this plant so that uptake cannot exceed demand
                if (availWaterDepPlant[i] >= potentialEP[i])
                {
                    scalerPlant[i] = Utility.Math.Divide(potentialEP[i], availWaterDepPlant[i], 0.0);
                }
                else
                {
                    scalerPlant[i] = Utility.Math.Divide(Utility.Math.Sum(availWaterDepPlant), potentialEP[i], 0.0);
                    scalerPlant[i] = Utility.Math.Constrain(scalerPlant[i], 0.0, 1.0);
                }
            }


            // calculate the maximum water available in each layer
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    availWaterDepLayer[j] = Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j];
                    double[] tempDemandLayer = new double[Soil.SoilWater.dlayer.Length] ;
                    tempDemandLayer[j] = 0.0;
                    for (int i = 0; i < plants.Length; i++)
                    {
                        tempDemandLayer[j] += availWaterDepPlantLayer[i, j] * scalerPlant[i];
                    }
                // add up all the total (scaled) demand from the solo-plant calculations above and compare against the water in the layer
                // and see if uptake in any layer needs to be constrained
                    scalerLayer[j] = Utility.Math.Divide(availWaterDepLayer[j], tempDemandLayer[j], 0.0);
                    scalerLayer[j] = Utility.Math.Constrain(scalerLayer[j], 0.0, 1.0);
                }

                
            // calculate the uptakes as the demands scaled by plant and layer
            // calculate the dlts to send back to the soil water model
            // calculate the actual uptake for each plant and send to plant model
            for (int i = 0; i < plants.Length; i++)
            {
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    actualEPPlantLayer[i, j] = availWaterDepPlantLayer[i, j] * scalerPlant[i] * scalerLayer[j];
                    actualEP[i] += actualEPPlantLayer[i, j];
                    dltSWdep[j] += -1.0 * actualEPPlantLayer[i, j];  // -ve to reduce water content in the soil
                }
                // send the actual EP to the plants
                plants[i].ActualEP = actualEP[i];
            }

            // send the change in soil water to the soil water module
            Soil.SoilWater.dlt_sw_dep = dltSWdep;
        }

        [EventSubscribe("DoNutrientArbitration")]
        private void OnDoNutrientArbitration(object sender, EventArgs e)
        {
            // use i for the plant loop and j for the layer loop

            NitrogenChangedType NUptakeType = new NitrogenChangedType();
            NUptakeType.Sender = Name;
            NUptakeType.SenderType = "Plant";
            NUptakeType.DeltaNO3 = new double[Soil.SoilWater.dlayer.Length];
            NUptakeType.DeltaNH4 = new double[Soil.SoilWater.dlayer.Length];

            // calculate the potentially available water and sum the demand
            for (int i = 0; i < plants.Length; i++)
            {
                potentialNitrogenDemand[i] = plants[i].potentialNitrogenDemand; // note that eventually PotentialEP will be calculated above in the EnergyArbitration 
                totalNitrogenDemand += potentialNitrogenDemand[i];
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {

                    double temp = (Soil.SoilNitrogen.no3[j] - Soil.SoilNitrogen.nh4[j]) * (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]) * plants[i].RootProperties.KL[j] * plants[i].RootProperties.RootExplorationByLayer[j];
                    availNitrogenPlantLayer[i, j] = Math.Max(temp, 0.0);  // 

                    // sum by plant for convenience
                    availNitrogenPlant[i] += availNitrogenPlantLayer[i, j];
                }
                // if this was the only plant in the system, would there be enough water to satify demand?  If yes then scaler = 1 otherwise < 1
                // when it comes to uptake then this scaler gets applied across all layers for this plant so that uptake cannot exceed demand
                if (availNitrogenPlant[i] >= potentialNitrogenDemand[i])
                {
                    scalerPlantNitrogen[i] = Utility.Math.Divide(potentialNitrogenDemand[i], availNitrogenPlant[i], 0.0);
                }
                else
                {
                    scalerPlantNitrogen[i] = Utility.Math.Divide(Utility.Math.Sum(availNitrogenPlant), potentialEP[i], 0.0);
                    scalerPlantNitrogen[i] = Utility.Math.Constrain(scalerPlantNitrogen[i], 0.0, 1.0);
                }
            }


            // calculate the maximum water available in each layer
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
            {
                availNitrogenLayer[j] = Soil.SoilNitrogen.no3[j] - Soil.SoilNitrogen.nh4[j];
                double[] tempDemandLayer = new double[Soil.SoilWater.dlayer.Length];
                tempDemandLayer[j] = 0.0;
                for (int i = 0; i < plants.Length; i++)
                {
                    tempDemandLayer[j] += availNitrogenPlantLayer[i, j] * scalerPlantNitrogen[i];
                }
                // add up all the total (scaled) demand from the solo-plant calculations above and compare against the water in the layer
                // and see if uptake in any layer needs to be constrained
                scalerLayerNitrogen[j] = Utility.Math.Divide(availNitrogenLayer[j], tempDemandLayer[j], 0.0);
                scalerLayerNitrogen[j] = Utility.Math.Constrain(scalerLayerNitrogen[j], 0.0, 1.0);
            }


            // calculate the uptakes as the demands scaled by plant and layer
            // calculate the dlts to send back to the soil water model
            // calculate the actual uptake for each plant and send to plant model
            for (int i = 0; i < plants.Length; i++)
            {
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    actualNitrogenSupplyPlantLayer[i, j] = availNitrogenPlantLayer[i, j] * scalerPlantNitrogen[i] * scalerLayerNitrogen[j];
                    actualNitrogenSupply[i] += actualNitrogenSupplyPlantLayer[i, j];
                    dltNitrogen[j] += -1.0 * actualNitrogenSupplyPlantLayer[i, j];  // -ve to reduce water content in the soil
                    double tempNO3NH4Ratio = Utility.Math.Divide(Soil.SoilNitrogen.no3[j], (Soil.SoilNitrogen.no3[j] - Soil.SoilNitrogen.nh4[j]), 0.0);
                    dltNO3[j] += -1.0 * actualNitrogenSupplyPlantLayer[i, j] * tempNO3NH4Ratio;
                    dltNH4[j] += -1.0 * actualNitrogenSupplyPlantLayer[i, j] * (1.0 - tempNO3NH4Ratio);
                    NUptakeType.DeltaNO3[j] = dltNO3[j];
                    NUptakeType.DeltaNH4[j] = dltNH4[j];
                }
                // send the actual EP to the plants
                plants[i].actualNitrogenSupply = actualNitrogenSupply[i];
            }

            // send the change in soil water to the soil water module

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);

        }

    }
}
