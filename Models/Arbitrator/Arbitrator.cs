using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF;

namespace Models.Arbitrator
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Arbitrator : Model
    {

        [Link]
        Soils.Soil Soil = null;
                
        //public double[]  { get; set; }


        public override void OnSimulationCommencing()
        {
            /// this is init
        }


        [EventSubscribe("DoEnergyArbitration")]
        private void OnDoEnergyArbitration(object sender, EventArgs e)
        {
        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            ICrop2[] plants = (ICrop2[])this.Children.MatchingMultiple(typeof(ICrop2));

            // Plant variables
            double[] actualEP = new double[plants.Length];
            double[] potentialEP = new double[plants.Length];
            double totalSWDemand;

            // Soil variables
            double[,] swUptakePlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            double[,] availWaterDepPlantLayer = new double[plants.Length, Soil.SoilWater.dlayer.Length];
            double[] availWaterDepPlant = new double[plants.Length];
            double[] availWaterDepLayer = new double[Soil.SoilWater.dlayer.Length];
            double availWaterDepSum;


            // Collect the plant information needed to do the water arbitration
            // soil water demand, kl, root depth, 
            // use i for the plant loop and j for the layer loop

            // zero some values - if these are delcared in this routine then do not need zeroing as are created each day anyway ??????
            availWaterDepSum = 0.0;
            totalSWDemand = 0.0;
            double swCheck = 0.0;
            for (int i = 0 ; i<plants.Length; i++)
            {
                availWaterDepPlant[i] = 0.0;
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    availWaterDepPlantLayer[i, j] = 0.0;
                    availWaterDepLayer[j] = 0.0;
                    swUptakePlantLayer[i, j] = 0.0;
                }
            }
            // delete above here?
            
 
            // calculate the potentially available water and sum the demand
            for (int i = 0 ; i<plants.Length; i++)
            {
                totalSWDemand += potentialEP[i];
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    double temp = (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LLDep[j]) * plants[i].RootProperties.KL[j] * plants[i].RootProperties.RootExplorationByLayer[j];
                    availWaterDepPlantLayer[i, j] = Math.Max(temp, 0.0);  // ?? not sum? - no shoud lonly be one combo that fits in ehre
                    availWaterDepSum += availWaterDepPlantLayer[i, j];   // needed?
                }
            }

            // sum by plant and layer for convenience
            for (int i = 0; i < plants.Length; i++)
            {
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    availWaterDepPlant[i] += availWaterDepPlantLayer[i, j];
                    availWaterDepLayer[j] += availWaterDepPlantLayer[i, j];
                }
            }
            // check the arithmetic!
            if (!Utility.Math.FloatsAreEqual(Utility.Math.Sum(availWaterDepPlant),Utility.Math.Sum(availWaterDepLayer)))
            {
                // do nothing
            }


            
            


            // zeroth case - sum of demand is zero
            // first case - sum of all the potentialEP <= sum of all the availWaterDep - if so simple case once thorugh and everyone is happy
            // second case - sum of all the availWaterDep <= 0.0 - no plants get any water
            // third case - scale back by a constant factor - this will be significantly improved later and made much more sensible

            if (Utility.Math.Sum(potentialEP) <= availWaterDepSum)
            {
                // start doing the calculations to remove water from soil
                for (int i = 0; i < plants.Length; i++)
                {
                    actualEP[i] = potentialEP[i] ;  // each crop gets what it wants
                    for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)  // look across the soil layers
                    {
                        swUptakePlantLayer[i,j] += (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LLDep[j]) * plants[i].RootProperties.KL[j] * plants[i].RootProperties.RootExplorationByLayer[j];
                    }
                }
            }
            else if (availWaterDepSum <= 0.0)
            {
                for (int i = 0; i < plants.Length; i++)
                {
                    actualEP[i] = 0.0;
                    for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)  // look across the soil layers
                    {
                        swUptakePlantLayer[i, j] = 0.0;
                    }
                }
            }
            else 
            {
                // calculate the scaling factor
                double tempScaling = Utility.Math.Divide(availWaterDepSum, Utility.Math.Sum(potentialEP), 0.0);   //Check hte default
                for (int i = 0; i < plants.Length; i++)
                {
                    actualEP[i] = tempScaling * potentialEP[i];
                    for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)  // look across the soil layers
                    {
                        swUptakePlantLayer[i, j] = 0.0;
                    }
                }
            }

        }

        [EventSubscribe("DoNutrientArbitration")]
        private void OnDoNutrientArbitration(object sender, EventArgs e)
        {
            double[] no3 = Soil.SoilNitrogen.no3;

        }

    }
}
