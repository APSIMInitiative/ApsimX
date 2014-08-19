using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF;

namespace Models.Arbitrator
{

    /// <summary>
    /// Vals ubeaut soil arbitrator
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Arbitrator : Model
    {

        [Link]
        Soils.Soil Soil = null;

        ICrop2[] plants ;

        // Plant variables
        double[] actualEP;
        double[,] actualEPPlantLayer;
        double[] potentialEP;
        double totalSWDemand;
        public double[] scalerPlant {get; set;}  //

        // Soil variables
        public double[] dltSWdep { get; set; }
        double[,] swUptakePlantLayer;
        double[,] availWaterDepPlantLayer ;
        double[] availWaterDepPlant ;
        double[] availWaterDepLayer ;
        double availWaterDepSum;
        double[] totalAvailWaterDepLayer ;  //water between sw and ll15 - temporary solution
        public double[] scalerLayer {get; set;}  //how much to scale back the water uptake because there is not enough water in the layer to staisfy

        public double ArbitEOS { get; set; }  //


                
        //public double[]  { get; set; }


        public override void OnSimulationCommencing()
        {
            // this is init
            //plants = (ICrop2[])this.Children.MatchingMultiple(typeof(ICrop2));
            plants = this.Plants;

            actualEP = new double[plants.Length];
            actualEPPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            potentialEP = new double[plants.Length];
            
            scalerPlant = new double[plants.Length];  //

            // Soil variables
            swUptakePlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            availWaterDepPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            availWaterDepPlant = new double[plants.Length];
            availWaterDepLayer = new double[Soil.SoilWater.dlayer.Length];
            
            totalAvailWaterDepLayer = new double[Soil.SoilWater.dlayer.Length];  //water between sw and ll15 - temporary solution
            scalerLayer = new double[Soil.SoilWater.dlayer.Length];  //how much to scale back the water uptake because there is not enough water in the layer to staisfy
            dltSWdep = new double[Soil.SoilWater.dlayer.Length];

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
        }

        [EventSubscribe("DoEnergyArbitration")]
        private void OnDoEnergyArbitration(object sender, EventArgs e)
        {
            for (int i = 0; i < plants.Length; i++)
            {
                plants[i].PotentialEP = 10.0;
                ArbitEOS = 0.0;
            }
        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            // use i for the plant loop and j for the layer loop
 
            // calculate the potentially available water and sum the demand
            for (int i = 0 ; i<plants.Length; i++)
            {
                potentialEP[i] = plants[i].PotentialEP; // note that eventually PotentialEP will be calculated above in the EnergyArbitration 
                totalSWDemand += potentialEP[i];
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    
                    double temp = (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LLDep[j]) * plants[i].RootProperties.KL[j] * plants[i].RootProperties.RootExplorationByLayer[j];
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
                    dltSWdep[j] += -1.0 * actualEPPlantLayer[i, j];
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
            double[] no3 = Soil.SoilNitrogen.no3;

        }

    }
}
