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

        [Description("Arbitration method: PropDemand/RotatingCall/Others to come")]        public string ArbitrationMethod { get; set; }

        // Plant variables
        double[] demandWater;
        double[,] potentialSupplyWaterPlantLayer;
        double[,] supplyWaterPlantLayer;

        double[] tempDepthArray;
        double tempSupply; // used as a temporary holder for the amount of water across all depths for a particular plant
        
        // Soil variables
        public double[] dltSWdep { get; set; }

        // soil water evaporation stuff
        public double ArbitEOS { get; set; }  //

        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        public event NitrogenChangedDelegate NitrogenChanged;

        class CanopyProps
        {
            public double laiGreen;
            public double laiTotal;
        }
        //public CanopyProps[,] myCanopy;

        public override void OnSimulationCommencing()
        {
            // Check that ArbitrationMethod is valid
            if (ArbitrationMethod.ToLower() == "PropDemand".ToLower()) ;
            // nothing, all good
            else if (ArbitrationMethod.ToLower() == "RotatingCall".ToLower())
                // this will be implemented for testing but not yet so end the simulation
                throw new Exception("The RotatingCall option has not been implemented yet");
            else
                throw new Exception("Invalid AribtrationMethod selected");

            // get the list of crops in the simulation
            plants = this.Plants;

            // myCanopy = new CanopyProps[plants.Length, 0];  // later make this the layering in the canopy
            // for (int i = 0; i < plants.Length; i++)
            // {
            //    myCanopy[0, 0].laiGreen = plants[i].CanopyProperties.LAI;
            // }
            

            demandWater = new double[plants.Length];
            potentialSupplyWaterPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            supplyWaterPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            tempDepthArray = new double[Soil.SoilWater.dlayer.Length];
        }


        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            Utility.Math.Zero(demandWater);
            Utility.Math.Zero(potentialSupplyWaterPlantLayer);
            Utility.Math.Zero(supplyWaterPlantLayer);
            Utility.Math.Zero(dltSWdep);
        }

        [EventSubscribe("DoEnergyArbitration")]
        private void OnDoEnergyArbitration(object sender, EventArgs e)
        {
            // i is for plants
            // j is for layers in the canopy - layers are from the top downwards

            // THIS NEEDS TO GO ONCE THE PROPER STUFF IS IN HERE
            for (int i = 0; i < plants.Length; i++)
            {
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

            //for (int i = 0; i < plants.Length; i++)
            //{
            //    for (int j = 0; j < plants.Length; j++)
            //    {
            //    }
            //}
        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            //ToDO
            // Actual soil water evaporation
            //

            // use i for the plant loop and j for the layer loop

            double tempSupply;  // this zeros the variable for each crop - calculates the potentialSupply for the crop for all layers - will be used to compare against demand
            // calculate the potentially available water and sum the demand
            for (int i = 0 ; i<plants.Length; i++)
            {
                demandWater[i] = plants[i].demandWater; // note that eventually demandWater will be calculated above in the EnergyArbitration 
                tempSupply = 0.0;
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // this step gives the proportion of the root zone that is this layer
                    potentialSupplyWaterPlantLayer[i, j] = Utility.Math.Divide(plants[i].RootProperties.RootExplorationByLayer[j], Utility.Math.Sum(plants[i].RootProperties.RootExplorationByLayer), 0.0);
                    potentialSupplyWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * plants[i].RootProperties.KL[j] * Math.Max(0.0, (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LowerLimitDep[j]));
                    tempSupply+=potentialSupplyWaterPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below
                }
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // if the potential supply calculated above is greater than demand then scale it back - note that this is still a potential supply as a solo crop
                    potentialSupplyWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(Utility.Math.Sum(demandWater), tempSupply, 0.0));
                }
            }

            // calculate the maximum amount of water available in each layer
            double[] totalAvailableWater;
            totalAvailableWater = new double[Soil.SoilWater.dlayer.Length];
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    totalAvailableWater[j] = Math.Max(0.0,Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]);
                }

            // compare the potential water supply against the total available water
            // if supply exceeds demand then satisfy all demands, otherwise scale back by relative demand
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++) // loop through the layers in the outer loop
            {
                for (int i = 0; i < plants.Length; i++)
                {
                    supplyWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(totalAvailableWater[j], Utility.Math.Sum(demandWater), 0.0));
                    dltSWdep[j] += -1.0 * supplyWaterPlantLayer[i, j];  // -ve to reduce water content in the soil
                }
            }  // close the layer loop

            // send the actual transpiration to the plants
            
            for (int i = 0; i < plants.Length; i++)
            {
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    tempDepthArray[j] = supplyWaterPlantLayer[i, j];
                }
                plants[i].supplyWater = tempDepthArray;
            }

            // send the change in soil water to the soil water module
            Soil.SoilWater.dlt_sw_dep = dltSWdep;
        }

 
        /*
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
                potentialNitrogenDemand[i] = plants[i].potentialNitrogenDemand; // note that eventually demandWater will be calculated above in the EnergyArbitration 
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
                    scalerPlantNitrogen[i] = Utility.Math.Divide(Utility.Math.Sum(availNitrogenPlant), demandWater[i], 0.0);
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
        */
    }
}
