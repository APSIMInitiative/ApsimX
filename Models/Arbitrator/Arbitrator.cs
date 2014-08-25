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

        [Link]
        Summary Summary = null;

        ICrop2[] plants ;

        /// <summary>
        /// This will hold a range of arbitration methods for testing - will eventually settle on one standard method
        /// </summary>
        [Description("Arbitration method: PropDemand/RotatingCall/Others to come")]        public string ArbitrationMethod { get; set; }

        // Plant variables
        double[] demandWater;
        double[,] potentialSupplyWaterPlantLayer;
        double[,] supplyWaterPlantLayer;

        double[] demandNitrogen;
        double[,] potentialSupplyNitrogenPlantLayer;
        double[,] supplyNitrogenPlantLayer;
        double[,] supplyNitrogenPropNO3PlantLayer;

        // soil water evaporation stuff
        //public double ArbitEOS { get; set; }  //

        /// <summary>
        /// The NitrogenChangedDelegate is for the Arbitrator to set the change in nitrate and ammonium in SoilNitrogen
        /// </summary>
        /// <param name="Data"></param>
        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        /// <summary>
        /// To publish the change event
        /// </summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        class CanopyProps
        {
            /// <summary>
            /// Grean leaf area index (m2/m2)
            /// </summary>
            public double laiGreen;
            /// <summary>
            /// Total leaf area index (m2/m2)
            /// </summary>
            public double laiTotal;
        }
        //public CanopyProps[,] myCanopy;

        /// <summary>
        /// Runs at the start of the simulation, here only reads the aribtration method to be used
        /// </summary>
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
            
            // size the arrays
            demandWater = new double[plants.Length];
            potentialSupplyWaterPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            supplyWaterPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            
            demandNitrogen = new double[plants.Length];
            potentialSupplyNitrogenPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            supplyNitrogenPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            supplyNitrogenPropNO3PlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
        }


        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            Utility.Math.Zero(demandWater);
            Utility.Math.Zero(potentialSupplyWaterPlantLayer);
            Utility.Math.Zero(supplyWaterPlantLayer);

            Utility.Math.Zero(demandNitrogen);
            Utility.Math.Zero(potentialSupplyNitrogenPlantLayer);
            Utility.Math.Zero(supplyNitrogenPlantLayer);
            Utility.Math.Zero(supplyNitrogenPropNO3PlantLayer);

        }

        [EventSubscribe("DoEnergyArbitration")]
        private void OnDoEnergyArbitration(object sender, EventArgs e)
        {
            // i is for plants
            // j is for layers in the canopy - layers are from the top downwards

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

            double tempSupply = 0.0;  // this zeros the variable for each crop - calculates the potentialSupply for the crop for all layers - will be used to compare against demand
            // calculate the potentially available water and sum the demand
            for (int i = 0 ; i<plants.Length; i++)
            {
                demandWater[i] = plants[i].demandWater; // note that eventually demandWater will be calculated above in the EnergyArbitration 
                tempSupply = 0.0;
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // this step gives the proportion of the root zone that is this layer
                    //potentialSupplyWaterPlantLayer[i, j] = Utility.Math.Divide(plants[i].RootProperties.RootExplorationByLayer[j], Utility.Math.Sum(plants[i].RootProperties.RootExplorationByLayer), 0.0);
                    //potentialSupplyWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * plants[i].RootProperties.KL[j] * Math.Max(0.0, (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LowerLimitDep[j]));
                    potentialSupplyWaterPlantLayer[i, j] = plants[i].RootProperties.RootExplorationByLayer[j] * plants[i].RootProperties.KL[j] * Math.Max(0.0, (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LowerLimitDep[j]));
                    tempSupply += potentialSupplyWaterPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below
                }
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // if the potential supply calculated above is greater than demand then scale it back - note that this is still a potential supply as a solo crop
                    potentialSupplyWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(demandWater[i], tempSupply, 0.0));
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
            double[] dltSWdep = new double[Soil.SoilWater.dlayer.Length];   // to hold the changes in soil water depth
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
                double[] dummyArray = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)           // cannot set a particular dimension from a 2D arrary into a 1D array directly so need a temporary variable
                {
                    dummyArray[j] = supplyWaterPlantLayer[i, j];
                }
                //tempDepthArray.CopyTo(plants[i].supplyWater, 0);  // need to use this because of the thing in .NET about pointers not values being set for arrays - only needed if the array is not newly created
                plants[i].supplyWater = dummyArray;
                // debugging into SummaryFile
                //Summary.WriteMessage(FullPath, "Arbitrator is setting the value of plants[" + i.ToString() + "].supplyWater(3) to  " + plants[i].supplyWater[3].ToString());
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

            double tempSupply = 0.0;  // this zeros the variable for each crop - calculates the potentialSupply for the crop for all layers - will be used to compare against demand
            // calculate the potentially available water and sum the demand
            for (int i = 0; i < plants.Length; i++)
            {
                demandNitrogen[i] = plants[i].demandNitrogen; // note that eventually demandWater will be calculated above in the EnergyArbitration 
                tempSupply = 0.0;
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // this step gives the proportion of the root zone that is this layer
                    //potentialSupplyNitrogenPlantLayer[i, j] = Utility.Math.Divide(plants[i].RootProperties.RootExplorationByLayer[j], Utility.Math.Sum(plants[i].RootProperties.RootExplorationByLayer), 0.0);
                    //potentialSupplyNitrogenPlantLayer[i, j] = potentialSupplyNitrogenPlantLayer[i, j] * plants[i].RootProperties.KL[j] * (Soil.SoilNitrogen.no3[j] + Soil.SoilNitrogen.nh4[j]);
                    potentialSupplyNitrogenPlantLayer[i, j] = plants[i].RootProperties.RootExplorationByLayer[j] * plants[i].RootProperties.KL[j] * (Soil.SoilNitrogen.no3[j] + Soil.SoilNitrogen.nh4[j]);
                    tempSupply += potentialSupplyNitrogenPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below
                }
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // if the potential supply calculated above is greater than demand then scale it back - note that this is still a potential supply as a solo crop
                    potentialSupplyNitrogenPlantLayer[i, j] = potentialSupplyNitrogenPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(demandNitrogen[i], tempSupply, 0.0));
                }
            }

            // calculate the maximum amount of nitrogen available in each layer
            double[] totalAvailableNitrogen;
            totalAvailableNitrogen = new double[Soil.SoilWater.dlayer.Length];
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
            {
                totalAvailableNitrogen[j] = Soil.SoilNitrogen.no3[j]+Soil.SoilNitrogen.nh4[j];
            }

            // compare the potential nitrogen supply against the total available nitrogen
            // if supply exceeds demand then satisfy all demands, otherwise scale back by relative demand
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++) // loop through the layers in the outer loop
            {
                for (int i = 0; i < plants.Length; i++)
                {
                    supplyNitrogenPlantLayer[i, j] = potentialSupplyNitrogenPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(totalAvailableNitrogen[j], Utility.Math.Sum(demandNitrogen), 0.0));
                    supplyNitrogenPropNO3PlantLayer[i, j] = 0.0;
                    if (supplyNitrogenPlantLayer[i, j] > 0.0)
                    {
                        supplyNitrogenPropNO3PlantLayer[i, j] = Utility.Math.Divide(Soil.SoilNitrogen.no3[j], (Soil.SoilNitrogen.no3[j] + Soil.SoilNitrogen.nh4[j]), 0.0);
                    }
                    NUptakeType.DeltaNO3[j] += -1.0 * supplyNitrogenPlantLayer[i, j] * supplyNitrogenPropNO3PlantLayer[i, j];  // -ve to reduce water content in the soil
                    NUptakeType.DeltaNH4[j] += -1.0 * supplyNitrogenPlantLayer[i, j] * (1.0 - supplyNitrogenPropNO3PlantLayer[i, j]);  // -ve to reduce water content in the soil
                }
            }  // close the layer loop

            for (int i = 0; i < plants.Length; i++)
            {
                double[] dummyArray1 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing
                double[] dummyArray2 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)           // cannot set a particular dimension from a 2D arrary into a 1D array directly so need a temporary variable
                {
                    dummyArray1[j] = supplyNitrogenPlantLayer[i, j];
                    dummyArray2[j] = supplyNitrogenPropNO3PlantLayer[i, j];
                }
                plants[i].supplyNitrogen = dummyArray1;
                plants[i].supplyNitrogenPropNO3 = dummyArray2;
                // debugging into SummaryFile
                //Summary.WriteMessage(FullPath, "Arbitrator is setting the value of plants[" + i.ToString() + "].supplyWater(3) to  " + plants[i].supplyWater[3].ToString());
            }


            // send the change in soil soil nitrate and ammonium to the soil nitrogen module

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);

        }
    }
}
