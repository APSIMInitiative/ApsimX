using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF;
using Models.Soils;

namespace Models.Arbitrator
{

    /// <summary>
    /// Vals u-beaut (VS - please note that DH wrote that) soil arbitrator
    /// TODO - convert to god-level
    /// TODO - need to figure out how to get multi-zone root systems into this - structure there to do the uptake but note comments in code related to needs
    /// TODO - alternate uptake methods
    /// TODO - currently is implicitly 1 ha per zone - need to convert into proper consideration of area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Arbitrator : Model
    {

        /// <summary>The soil</summary>
        [Link]
        Soils.Soil Soil = null;

        /// <summary>The summary</summary>
        [Link]
        Summary Summary = null;

        /// <summary>The zone</summary>
        [Link]
        Zone zone = null;

        /// <summary>The plants</summary>
        List<ICrop2> plants;

        #region // initial definitions

        /// <summary>
        /// This will hold a range of arbitration methods for testing - will eventually settle on one standard method
        /// </summary>
        /// <value>The arbitration method.</value>
        [Description("Arbitration method: old / new - use old - new only for testing")]
        public string ArbitrationMethod { get; set; }

        /// <summary>Gets or sets the nutrient uptake method.</summary>
        /// <value>The nutrient uptake method.</value>
        [Description("Potential nutrient uptake method: 1=where the roots are; 2=PMF concentration based; 3=OilPalm amount based")]
        public int NutrientUptakeMethod { get; set; }

        /// <summary>
        /// Potential nutrient supply in layers (kgN/ha/layer) - this is the potential (demand-limited) supply if this was the only plant in the simualtion
        /// </summary>
        //[XmlIgnore]
        //[Description("Potential nutrient supply in layers for the first plant")]
        //public double[] Plant1potentialSupplyNitrogenPlantLayer
        //{
        //    get
        //    {
        //        double[] result = new double[Soil.SoilWater.dlayer.Length];
        //        for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
        //        {
        //            result[j] = potentialSupplyNitrogenPlantLayer[0, j];
        //        }
        //        return result;
        //    }
        //}


        
        // these only (I think) used by the routines that Hamish using for testing - marked for deletion
        double[,] potentialSupplyWaterPlantLayer;
        /// <summary>The uptake water plant layer</summary>
        double[,] uptakeWaterPlantLayer;
        /// <summary>The potential supply nitrogen plant layer</summary>
        double[,] potentialSupplyNitrogenPlantLayer;
        /// <summary>
        /// The potential supply property n o3 plant layer
        /// </summary>
        double[,] potentialSupplyPropNO3PlantLayer;
        /// <summary>The uptake nitrogen plant layer</summary>
        double[,] uptakeNitrogenPlantLayer;
        /// <summary>
        /// The uptake nitrogen property n o3 plant layer
        /// </summary>
        double[,] uptakeNitrogenPropNO3PlantLayer;

        // soil water evaporation stuff
        //public double ArbitEOS { get; set; }  //

        /// <summary>
        /// The NitrogenChangedDelegate is for the Arbitrator to set the change in nitrate and ammonium in SoilNitrogen
        /// Eventually this should be changes to a set rather than an event
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        /// <summary>To publish the nitrogen change event</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        /// <summary>
        /// 
        /// </summary>
        class CanopyProps
        {
            //public double laiGreen;
            //public double laiTotal;
        }
        //public CanopyProps[,] myCanopy;

        // new Arbitration parameters from here

        /// <summary>Soil water demand (mm) from each of the plants being arbitrated</summary>
        double[] demandWater;

        /// <summary>Nitrogen demand (mm) from each of the plants being arbitrated</summary>
        double[] demandNitrogen;

        /// <summary>The number of zones under consideration</summary>
        int zones;

        /// <summary>
        /// The number of lower bounds of uptake under consideration.
        /// As the uptake method implemented for nitrogen is the second order method this is currently only caluclated for water and comes from the crop lower limit
        /// </summary>
        int bounds;

        /// <summary>
        /// The number of pools that can saatify the demand.  Not relevant for water.  For nitrogen is nitrate and ammonium.
        /// </summary>
        int forms;

        /// <summary>
        /// rootExploration is 3D array [plant, layer, zone] and holds the relative presence (0-1) of roots in each of the physical areas (layer, zone) for each plant
        /// It is populated from RootExplorationByLayer (0-1) which considers a single zone only.
        /// </summary>
        double[, ,] rootExploration;  // has indexes for plant, layer, zone - need to figure out zones

        /// <summary>
        /// lowerBound is a holds the values of the lowest amount of water that can be extracted  from each layer, zone and bound.
        /// Current it contains a dummy dimension for plant - which can be removed aat any time
        /// </summary>
        double[, , ,] lowerBound;  // has indexes for plant, layer, zone and bound

        /// <summary>
        /// uptakePreference [plant, layer, zone] and holds the relative preference (0-1) for uptake from different physical areas (layer, zone) for each plant
        /// It can be used to create a greater preference for uptake near the soil surface or from zones nearer the main stem if appropriate
        /// It is populated from UptakePreferenceByLayer (0-1) which currently considers a single zone only.
        /// </summary>
        double[, ,] uptakePreference;  // indexes for plant, layer, zone - this is a thing that the plant module can set to 

        
        // these two parameters might be used to get rid of much of the if water if nitrogen stuff
        //double[, , , ,] uptakeParameterOnAmountInLayer;
        //double[, , , ,] uptakeParameterOnConcentrationInSoil;

        /// <summary>
        /// resource (mm or kgN/ha) is the amount of water or nitrogen held in each layer and zone, bound and form for each plant
        /// the dimensions are [plant, layer, zone, bound, form]
        /// </summary>
        double[, , , ,] resource;

        /// <summary>
        /// extractable (mm or kgN/ha) is the amount of water or nitrogen in each layer and zone, bound and form for each plant that would be
        /// available to the plant if it were the only plant in the simulation.  It is not limited by the plant's demand for the resource.
        /// the dimensions are [plant, layer, zone, bound, form]
        /// </summary>
        double[, , , ,] extractable;

        /// <summary>
        /// demand (mm or kgN/ha) is the amount of water or nitrogen in each layer and zone, bound and form for each plant that would be
        /// extracted by the plant if it were the only plant in the simulation.  It is extractable constrained by demandWater or demandNitrogen
        /// the dimensions are [plant, layer, zone, bound, form]
        /// </summary>
        double[, , , ,] demand;

        /// <summary>
        /// demandForResource (mm or kgN/ha) is the amount of water or nitrogen in each layer and zone, bound and form that is demanded across all plants
        /// the dimensions are [not_used, layer, zone, bound, form]
        /// </summary>
        double[, , , ,] demandForResource;

        /// <summary>
        /// totalForResource (mm or kgN/ha) is the sum of resource in each layer and zone, bound and form that is demanded across all plants
        /// the dimensions are [not_used, layer, zone, bound, form]
        /// </summary>
        double[, , , ,] totalForResource;

        /// <summary>
        /// uptake (mm or kgN/ha) is the amount of water or nitrogen taken up in each layer and zone, bound and form for each plant
        /// the dimensions are [plant, layer, zone, bound, form]
        /// </summary>
        double[, , , ,] uptake;

        /// <summary>
        /// extractableByPlant (mm or kgN/ha) is the sum of water or nitrogen taken in extractable for each plant
        /// the dimensions are [plant]
        /// </summary>
        double[] extractableByPlant;

        // <summary>This is "water" or "nitrogen" and holds the resource that is being arbitrated</summary>
        //string resourceToArbitrate;

#endregion  // defintions


        /// <summary>
        /// Runs at the start of the simulation, here only reads the aribtration method to be used
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.Exception">Invalid AribtrationMethod selected</exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // Check that ArbitrationMethod is valid
            if (ArbitrationMethod.ToLower() == "old")
            {
                ZeroHamishVariables();
            }
            else if (ArbitrationMethod.ToLower() == "new")
            {
                ZeroDailyVariables();
            }
            else
                throw new Exception("Invalid AribtrationMethod selected");

            // get the list of crops in the simulation
            plants = zone.Plants;

            // TODO - find out about zones and areas and plants in each

            // not needed if not used for energy arbitration as well
            // myCanopy = new CanopyProps[plants.Length, 0];  // later make this the layering in the canopy
            // for (int i = 0; i < plants.Length; i++)
            // {
            //    myCanopy[0, 0].laiGreen = plants[i].CanopyProperties.LAI;
            // }

            // Old stuff - delete once Hamish is done
            potentialSupplyWaterPlantLayer = new double[plants.Count, Soil.SoilWater.dlayer.Length];
            uptakeWaterPlantLayer = new double[plants.Count, Soil.SoilWater.dlayer.Length];
            potentialSupplyNitrogenPlantLayer = new double[plants.Count, Soil.SoilWater.dlayer.Length];
            potentialSupplyPropNO3PlantLayer = new double[plants.Count, Soil.SoilWater.dlayer.Length];
            uptakeNitrogenPlantLayer = new double[plants.Count, Soil.SoilWater.dlayer.Length];
            uptakeNitrogenPropNO3PlantLayer = new double[plants.Count, Soil.SoilWater.dlayer.Length];

            // new Arbitration parameters from here
            // set some dimensions
            // when get the zone connections will have to find plants across all zones and the layer dimension will be the maximum number of layers across all zones
            zones = 1; // as a starting point - will we need to consider redimensioning?  Depends on when the root system information is available - or just allow for all zones beneath current location
            bounds = plants.Count;  // the number of bounds cannot exceed the number of plants
            forms = 2;  // 2 for nitrogen and 1 for water
            demandWater = new double[plants.Count];
            demandNitrogen = new double[plants.Count];
            rootExploration = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones];
            lowerBound = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds];
            uptakePreference = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones];
            resource = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds, forms];
            extractable = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds, forms];
            demand = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds, forms];
            demandForResource = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds, forms];
            totalForResource = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds, forms];
            uptake = new double[plants.Count, Soil.SoilWater.dlayer.Length, zones, bounds, forms];
            extractableByPlant = new double[plants.Count];
        }

        /// <summary>Zero variables at the start of each day</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            ZeroDailyVariables();
            ZeroHamishVariables();  // get rid of this when Hamish is done
        }

        /// <summary>Do the arbitration between the plants for water</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            if (ArbitrationMethod.ToLower() == "new")
            {
                ZeroCalculationVariables();
                forms = 1;
                CalculateLowerBounds("water");
                CalculateResource("water");
                CalculateExtractable("water");
                CalculateDemand("water");
                CalculateUptake("water");
                SetWaterUptake("water");

            }
            else
            {
                Old_OnDoWaterArbitration();
            }
        }

        /// <summary>
        /// Do the arbitration between the plants for nutrients
        /// This is only set up for nitrogen at the moment but is designed to be extended to other nutrients
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoNutrientArbitration")]
        private void OnDoNutrientArbitration(object sender, EventArgs e)
        {
            if (ArbitrationMethod.ToLower() == "new")
            {
                ZeroCalculationVariables();
                forms = 2;  // ie NO3 and NH4
                CalculateLowerBounds("nitrogen");
                CalculateResource("nitrogen");
                CalculateExtractable("nitrogen");
                CalculateDemand("nitrogen");
                CalculateUptake("nitrogen");
                SetNitrogenUptake("nitrogen");

            }
            else
            {
                Old_OnDoNutrientArbitration();
            }
        }

        /// <summary>
        /// Calculate the results of the competition between the plants for light and calculate the soil water demadn by the plants and or soil water evaporation
        /// Not currently implemented
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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

        /// <summary>Zero the values at the start of each day</summary>
        private void ZeroDailyVariables()
        {
            Utility.Math.Zero(demandWater);
            Utility.Math.Zero(demandNitrogen);
            Utility.Math.Zero(rootExploration);
            Utility.Math.Zero(lowerBound);
            Utility.Math.Zero(uptakePreference);

            //mypaddock.Children.
            // Iterate through all child paddocks.
            //foreach (Zone paddock in mypaddock.FindAll(typeof(Zone)))
            //{
            // Find soil in paddock.
            //Soil soil = paddock.Find(typeof(Soil));

            // Find plants in paddock.
            //ICrop2[] plants = paddock.Plants;
            //}

        }

        /// <summary>Zero the values that are needed for each set of arbitration calculations</summary>
        private void ZeroCalculationVariables()
        {
            Utility.Math.Zero(resource);
            Utility.Math.Zero(extractable);
            Utility.Math.Zero(demand);
            Utility.Math.Zero(uptake);
            Utility.Math.Zero(demandForResource);
            Utility.Math.Zero(totalForResource);
            Utility.Math.Zero(extractableByPlant);
        }

        /// <summary>
        /// Calculate the number of lower bounds that exist in each layer-zone combination and assign value to those bounds
        /// This does not need to be done each day unless LL becomes dynamic
        /// Leave here for now but could be moved either to OnSimulationCommencing or test for bounds and only do when null
        /// </summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void CalculateLowerBounds(string resourceToArbitrate)
        {
            // set up the bounds here  
            // the tolerance is used to trmpoarily convert the double array of lower limits into an integer array.  This then allows the usage of Distinct to remove duplicates.
            // At present set tolerance to a low value - this will end up with more bounds but will be more accurate with the avaialbel weater to plants
            double tolerance = 0.001;
            if (resourceToArbitrate.ToLower() == "water")
            {
                int[] tempBoundsArray = new int[plants.Count];
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        // get the lower bounds from the plants and put them into a temporary array for sorting, comparison and shortening before assigning bounds
                        // for the unique values, divide each element by the tolerance (which is mm diference that is interesting) and convert to an integer
                        // then sort, reverse and then use the Distinct method to shorten the array into functionally different values
                        // lastly multiply by the tolerance again to get the bounds in mm
                        // this is redundant but non-troublesome for a single-plant simulation
                        for (int p = 0; p < plants.Count; p++)
                        {
                            tempBoundsArray[p] = Convert.ToInt32(Math.Round(plants[p].RootProperties.LowerLimitDep[l] / tolerance, 0));
                        }
                        // 
                        var tempShortenedBoundsArray = tempBoundsArray.Distinct().ToArray();
                        // sort the shortened array and then put it into descending order - wettest first
                        Array.Sort(tempShortenedBoundsArray);
                        Array.Reverse(tempShortenedBoundsArray);
                        bounds = tempShortenedBoundsArray.Length;
                        for (int b = 0; b < bounds; b++)
                        {
                            lowerBound[0, l, z, b] = tempShortenedBoundsArray[b] * tolerance;
                        }
                    }
                }
            }
            else if (resourceToArbitrate.ToLower() == "nitrogen")
            {
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        bounds = 1;  // assume they are all have the same lower bound
                        lowerBound[0, l, z, 0] = 0.0;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the amount of the water or nitrogen that is assessible by each of the plants by layer-zone and bound-form
        /// </summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void CalculateResource(string resourceToArbitrate)
        {

            for (int p = 0; p < plants.Count; p++)
            {
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++)
                    {
                        for (int b = 0; b < bounds; b++)
                        {
                            for (int f = 0; f < forms; f++)
                            {
                                if (resourceToArbitrate.ToLower() == "water")
                                {
                                    if (b == 0)
                                    {
                                        resource[p, l, z, b, f] = Math.Max(0.0, (Soil.SoilWater.sw_dep[l] - Math.Max(plants[p].RootProperties.LowerLimitDep[l], lowerBound[0, l, z, b])));
                                    }
                                    else
                                    {
                                        resource[p, l, z, b, f] = Math.Max(0.0, (Soil.SoilWater.sw_dep[l] - Math.Max(plants[p].RootProperties.LowerLimitDep[l], lowerBound[0, l, z, b]))) - resource[p, l, z, b - 1, f];
                                    }
                                }
                                else if (resourceToArbitrate.ToLower() == "nitrogen")
                                {
                                    if (f == 0)
                                    {
                                        resource[p, l, z, b, f] = Math.Max(0.0, Soil.SoilNitrogen.NO3[l]);
                                    }
                                    else
                                    {
                                        resource[p, l, z, b, f] = Math.Max(0.0, Soil.SoilNitrogen.nh4[l]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the amount that the plant could potentially exctract if it was the only plant in the simulation and demand &gt; resource
        /// </summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void CalculateExtractable(string resourceToArbitrate)
        {
            // calculate extractable 
            for (int p = 0; p < plants.Count; p++)
            {
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        for (int b = 0; b < bounds; b++)  
                        {
                            for (int f = 0; f < forms; f++)  
                            {
                                if (resourceToArbitrate.ToLower() == "water")
                                {
                                    extractable[p, l, z, b, f] = plants[p].RootProperties.UptakePreferenceByLayer[l]   // later add in zone
                                                               * plants[p].RootProperties.RootExplorationByLayer[l]    // later add in zone
                                                               * plants[p].RootProperties.KL[l]                        // later add in zone
                                                               * resource[p, l, z, b, f];                              // the usage of 0 instead of p is intended - there is no actual p dimension in resource
                                    extractableByPlant[p] += extractable[p, l, z, b, f];
                                }
                                else if (resourceToArbitrate.ToLower() == "nitrogen")
                                {
                                    double relativeSoilWaterContent = Utility.Math.Constrain(Utility.Math.Divide((Soil.SoilWater.sw_dep[l] - Soil.SoilWater.ll15_dep[l]), (Soil.SoilWater.dul_dep[l] - Soil.SoilWater.ll15_dep[l]), 0.0), 0.0, 1.0);
                                    if (f == 0)
                                    {
                                        extractable[p, l, z, b, f] = relativeSoilWaterContent
                                                                   * plants[p].RootProperties.UptakePreferenceByLayer[l]   // later add in zone
                                                                   * plants[p].RootProperties.RootExplorationByLayer[l]    // later add in zone
                                                                   * plants[p].RootProperties.KNO3                        // later add in zone
                                                                   * Soil.SoilNitrogen.NO3ppm[l]
                                                                   * resource[p, l, z, b, f];   
                                    }
                                    else
                                    {
                                        extractable[p, l, z, b, f] = relativeSoilWaterContent
                                                                   * plants[p].RootProperties.UptakePreferenceByLayer[l]   // later add in zone
                                                                   * plants[p].RootProperties.RootExplorationByLayer[l]    // later add in zone
                                                                   * plants[p].RootProperties.KNH4                        // later add in zone
                                                                   * Soil.SoilNitrogen.nh4ppm[l]
                                                                   * resource[p, l, z, b, f];   
                                    }
                                    extractableByPlant[p] += extractable[p, l, z, b, f];
                                }
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Distribute the satisfyable (if this was a solo plant) across layers, zones, bounds and forms
        /// It was useful to seperate Extractable from Demand during development but could be amalgamated easily
        /// </summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void CalculateDemand(string resourceToArbitrate)
        {
            // calculate demand distributed over layers etc
            for (int p = 0; p < plants.Count; p++) 
            {
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        for (int b = 0; b < bounds; b++)  
                        {
                            for (int f = 0; f < forms; f++)
                            {
                                // demand here is a bad name as is limited by extractable - satisfyable demand if solo plant
                                if (resourceToArbitrate.ToLower() == "water")
                                {
                                    demand[p, l, z, b, f] = Math.Min(plants[p].demandWater, extractableByPlant[p])                                                           // ramp back the demand if not enough extractable resource
                                                          * Utility.Math.Constrain(Utility.Math.Divide(extractable[p, l, z, b, f], extractableByPlant[p], 0.0), 0.0, 1.0);   // and then distribute it over layers etc 

                                    demandForResource[0, l, z, b, f] += demand[p, l, z, b, f]; // this is the summed demand of all the plants for the layer, zone, bound and form - retain the first dimension for convienience
                                    totalForResource[0, l, z, b, f] += resource[p, l, z, b, f]; // this is the summed demand of all the plants for the layer, zone, bound and form - retain the first dimension for convienience
                                }
                                else if (resourceToArbitrate.ToLower() == "nitrogen")
                                {
                                    demand[p, l, z, b, f] = Math.Min(plants[p].demandNitrogen, extractableByPlant[p])                                                        // ramp back the demand if not enough extractable resource
                                                          * Utility.Math.Constrain(Utility.Math.Divide(extractable[p, l, z, b, f], extractableByPlant[p], 0.0), 0.0, 1.0);   // and then distribute it over layers etc 
                                    demandForResource[0, l, z, b, f] += demand[p, l, z, b, f]; // this is the summed demand of all the plants for the layer, zone, bound and form - retain the first dimension for convienience
                                    totalForResource[0, l, z, b, f] += resource[p, l, z, b, f]; // this is the summed demand of all the plants for the layer, zone, bound and form - retain the first dimension for convienience
                                }
                            }
                        }
                    }
                }
            }

        }

        /// <summary>Calculate the uptake for each plant across the layers, zones, bounds, and forms</summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void CalculateUptake(string resourceToArbitrate)
        {
            // calculate uptake distributed over layers etc - for now bounds and forms = 1
            for (int p = 0; p < plants.Count; p++)  // plant is not relevant for resource
            {
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        for (int b = 0; b < bounds; b++) 
                        {
                            for (int f = 0; f < forms; f++)
                            {
                                // ramp everything back if the resource < demand - note that resource is already only that which can potententially be extracted (i.e. not necessarily the total amount in the soil
                                uptake[p, l, z, b, f] = Utility.Math.Constrain(Utility.Math.Divide(totalForResource[0, l, z, b, f], demandForResource[0, l, z, b, f], 0.0), 0.0, 1.0)
                                                      * demand[p, l, z, b, f];
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send the water uptake arrays back to the plants and send the change in water storage back to the soil
        /// </summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void SetWaterUptake(string resourceToArbitrate)
        {
            double[] dltSWdep = new double[Soil.SoilWater.dlayer.Length];   // to hold the changes in soil water depth
            for (int p = 0; p < plants.Count; p++)
            {
                double[] dummyArray1 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing - will have to re-think this with when zones come in
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        for (int b = 0; b < bounds; b++)
                        {
                            dummyArray1[l] += uptake[p, l, z, b, 0];       // only 1 form but need to sum across bounds 
                            dltSWdep[l] += -1.0 * uptake[p, l, z, b, 0];   // -ve to reduce water content in the soil
                        }
                    }
                }
                // set uptakes in each plant
                plants[p].uptakeWater = dummyArray1;
            }
            // and finally set the changed soil resources
            Soil.SoilWater.dlt_sw_dep = dltSWdep;   // don't forget to look at this when zones come into play
        }

        /// <summary>
        /// Send the nitrogen uptake arrays back to the plants and send the change in nitrogen back to the soil
        /// </summary>
        /// <param name="resourceToArbitrate">The resource to arbitrate.</param>
        private void SetNitrogenUptake(string resourceToArbitrate)
        {
            NitrogenChangedType NUptakeType = new NitrogenChangedType();
            NUptakeType.Sender = Name;
            NUptakeType.SenderType = "Plant";
            NUptakeType.DeltaNO3 = new double[Soil.SoilWater.dlayer.Length];
            NUptakeType.DeltaNH4 = new double[Soil.SoilWater.dlayer.Length];

            for (int p = 0; p < plants.Count; p++)
            {
                double[] dummyArray1 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing - will have to re-think this with when zones come in
                double[] dummyArray2 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing - will have to re-think this with when zones come in
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                {
                    for (int z = 0; z < zones; z++) // for now set zones is to 1
                    {
                        for (int b = 0; b < bounds; b++)
                        {
                            for (int f = 0; f < forms; f++)  
                            {
                                dummyArray1[l] += uptake[p, l, z, b, f];       // add the forms together to give the total nitrogen uptake
                                if (f == 0)
                                {
                                    NUptakeType.DeltaNO3[l] += -1.0 * uptake[p, l, z, b, f];
                                    dummyArray2[l] += uptake[p, l, z, b, f];   // nitrate only uptake so can do the proportion before sending to the plant
                                }
                                else
                                {
                                    NUptakeType.DeltaNH4[l] += -1.0 * uptake[p, l, z, b, f];
                                }
                            }
                        }
                    }
                }
                // set uptakes in each plant
                plants[p].uptakeNitrogen = dummyArray1;
                for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)  // don't forget to deal with zones at some point
                {
                    dummyArray2[l] = Utility.Math.Divide(dummyArray2[l], dummyArray1[l], 0.0);
                }
                plants[p].uptakeNitrogenPropNO3 = dummyArray2;
            }
            // and finally set the changed soil resources
            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);

        }

        /// <summary>Old_s the on do water arbitration.</summary>
        private void Old_OnDoWaterArbitration()
        {
            //ToDO
            // Actual soil water evaporation
            //

            // use i for the plant loop and j for the layer loop

            double tempSupply = 0.0;  // this zeros the variable for each crop - calculates the potentialSupply for the crop for all layers - will be used to compare against demand
            // calculate the potentially available water and sum the demand
            for (int i = 0; i < plants.Count; i++)
            {
                if (plants[i].PlantEmerged == true)
                {
                    demandWater[i] = plants[i].demandWater; // note that eventually demandWater will be calculated above in the EnergyArbitration 
                    tempSupply = 0.0;
                    for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                    {
                        // this step gives the proportion of the root zone that is this layer
                        potentialSupplyWaterPlantLayer[i, j] = plants[i].RootProperties.RootExplorationByLayer[j] * plants[i].RootProperties.KL[j] * Math.Max(0.0, (Soil.SoilWater.sw_dep[j] - plants[i].RootProperties.LowerLimitDep[j]));
                        //SWSupply[layer] = Math.Max(0.0, * RootProportion(layer, Depth) * Soil.KL(this.Plant.Name)[layer] * KLModifier.Value * (Soil.SoilWater.sw_dep[layer] - Soil.LL(this.Plant.Name)[layer] * Soil.SoilWater.dlayer[layer]) );
                        tempSupply += potentialSupplyWaterPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below
                    }
                    for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                    {
                        // if the potential supply calculated above is greater than demand then scale it back - note that this is still a potential supply as a solo crop
                        potentialSupplyWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(demandWater[i], tempSupply, 0.0));
                    }
                }
            }
            // calculate the maximum amount of water available in each layer
            double[] totalAvailableWater;
            totalAvailableWater = new double[Soil.SoilWater.dlayer.Length];
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
            {
                totalAvailableWater[j] = Math.Max(0.0, Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]);
            }

            // compare the potential water supply against the total available water
            // if supply exceeds demand then satisfy all demands, otherwise scale back by relative demand
            double[] dltSWdep = new double[Soil.SoilWater.dlayer.Length];   // to hold the changes in soil water depth
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++) // loop through the layers in the outer loop
            {
                for (int i = 0; i < plants.Count; i++)
                {
                    uptakeWaterPlantLayer[i, j] = potentialSupplyWaterPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(totalAvailableWater[j], Utility.Math.Sum(demandWater), 0.0));
                    dltSWdep[j] += -1.0 * uptakeWaterPlantLayer[i, j];  // -ve to reduce water content in the soil
                }
            }  // close the layer loop

            // send the actual transpiration to the plants

            for (int i = 0; i < plants.Count; i++)
            {
                if (plants[i].PlantEmerged == true)
                {
                    double[] dummyArray = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing
                    for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)           // cannot set a particular dimension from a 2D arrary into a 1D array directly so need a temporary variable
                    {
                        dummyArray[j] = uptakeWaterPlantLayer[i, j];
                    }
                    //tempDepthArray.CopyTo(plants[i].uptakeWater, 0);  // need to use this because of the thing in .NET about pointers not values being set for arrays - only needed if the array is not newly created
                    plants[i].uptakeWater = dummyArray;
                    // debugging into SummaryFile
                    //Summary.WriteMessage(FullPath, "Arbitrator is setting the value of plants[" + i.ToString() + "].uptakeWater(3) to  " + plants[i].uptakeWater[3].ToString());
                }
            }
            // send the change in soil water to the soil water module
            Soil.SoilWater.dlt_sw_dep = dltSWdep;
        }

        /// <summary>Old_s the on do nutrient arbitration.</summary>
        /// <exception cref="System.Exception">Invalid potential uptake method selected</exception>
        private void Old_OnDoNutrientArbitration()
        {
            // use i for the plant loop and j for the layer loop

            NitrogenChangedType NUptakeType = new NitrogenChangedType();
            NUptakeType.Sender = Name;
            NUptakeType.SenderType = "Plant";
            NUptakeType.DeltaNO3 = new double[Soil.SoilWater.dlayer.Length];
            NUptakeType.DeltaNH4 = new double[Soil.SoilWater.dlayer.Length];

            double tempSupply = 0.0;  // this zeros the variable for each crop - calculates the potentialSupply for the crop for all layers - will be used to compare against demand
            // calculate the potentially available water and sum the demand
            for (int i = 0; i < plants.Count; i++)
            {
                if (NutrientUptakeMethod == 2)
                {
                    demandNitrogen[i] = Math.Min(plants[i].demandNitrogen, plants[i].RootProperties.MaximumDailyNUptake); //HEB added in Maximum daily uptake constraint
                }
                else
                {
                    demandNitrogen[i] = plants[i].demandNitrogen;
                }

                tempSupply = 0.0;  // this is the sum of potentialSupplyNitrogenPlantLayer[i, j] across j for each plant - used in the limitation to demand
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    if (NutrientUptakeMethod == 1) // use the soil KL with no water effect
                    {
                        potentialSupplyNitrogenPlantLayer[i, j] = Utility.Math.Divide(plants[i].RootProperties.KL[j], plants.Count, 0.0)
                                                                * (Soil.SoilNitrogen.NO3[j] + Soil.SoilNitrogen.nh4[j])
                                                                * plants[i].RootProperties.RootExplorationByLayer[j];
                        tempSupply += potentialSupplyNitrogenPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below

                        double tempNO3OnlySupply = Utility.Math.Divide(plants[i].RootProperties.KL[j], plants.Count, 0.0)
                                                 * Soil.SoilNitrogen.NO3[j]
                                                 * plants[i].RootProperties.RootExplorationByLayer[j];
                        potentialSupplyPropNO3PlantLayer[i, j] = Utility.Math.Divide(tempNO3OnlySupply, potentialSupplyNitrogenPlantLayer[i, j], 0.0);
                    }
                    else if (NutrientUptakeMethod == 2)
                    {
                        //I blocked the N arbitration out to get water working correctly with the arbitrator first.
                        /*   //Fixme.  The PMF implementation was not adjusting N Uptake in partially rooted layer.  I have left it as this for testing but will need to introduce it soon.
                        //method from PMF - based on concentration  
                        //HEB.  I have rewritten this section of code to make it function identically to current PMF implementation for purposes of testing

                        double relativeSoilWaterContent = 0;
                        if (Plants[i].RootProperties.RootLengthDensityByVolume[j] > 0.0) // Test to see if there are roots in this layer.
                        {

                            relativeSoilWaterContent = (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]) / (Soil.SoilWater.dul_dep[j] - Soil.SoilWater.ll15_dep[j]);
                            relativeSoilWaterContent = Utility.Math.Constrain(relativeSoilWaterContent, 0, 1.0);
                            potentialSupplyNitrogenPlantLayer[i, j] = Soil.SoilNitrogen.no3ppm[j] * Soil.SoilNitrogen.no3[j] * plants[i].RootProperties.KNO3 * relativeSoilWaterContent;
                        }
                        else
                        {
                            potentialSupplyNitrogenPlantLayer[i, j] = 0;
                        }

                        double tempNO3OnlySupply = 0;
                        tempNO3OnlySupply = potentialSupplyNitrogenPlantLayer[i, j];
                        potentialSupplyNitrogenPlantLayer[i, j] += Soil.SoilNitrogen.nh4ppm[j] * Soil.SoilNitrogen.nh4[j] * plants[i].RootProperties.KNH4 * relativeSoilWaterContent;
                        tempSupply += potentialSupplyNitrogenPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below

                        if (potentialSupplyNitrogenPlantLayer[i, j] == 0)
                            potentialSupplyPropNO3PlantLayer[i, j] = 1;
                        else
                            potentialSupplyPropNO3PlantLayer[i, j] = tempNO3OnlySupply / potentialSupplyNitrogenPlantLayer[i, j];
                    */
                    }
                    else if (NutrientUptakeMethod == 3)
                    {
                        //method from OilPlam - based on amount
                        double relativeSoilWaterContent = 0;
                        relativeSoilWaterContent = Utility.Math.Constrain(Utility.Math.Divide((Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]), (Soil.SoilWater.dul_dep[j] - Soil.SoilWater.ll15_dep[j]), 0.0), 0.0, 1.0);

                        potentialSupplyNitrogenPlantLayer[i, j] = Math.Max(0.0, plants[i].RootProperties.RootExplorationByLayer[j] * (Utility.Math.Divide(plants[i].RootProperties.KNO3, plants.Count, 0.0) * Soil.SoilNitrogen.NO3[j] + Utility.Math.Divide(plants[i].RootProperties.KNH4, plants.Count, 0.0) * Soil.SoilNitrogen.nh4[j]) * relativeSoilWaterContent);
                        tempSupply += potentialSupplyNitrogenPlantLayer[i, j]; // temporary add up the supply of water across all layers for this crop, then scale back if needed below

                        double tempNO3OnlySupply = 0;
                        tempNO3OnlySupply = Math.Max(0.0, plants[i].RootProperties.RootExplorationByLayer[j] * (Utility.Math.Divide(plants[i].RootProperties.KNO3, plants.Count, 0.0) * Soil.SoilNitrogen.NO3[j]) * relativeSoilWaterContent);
                        potentialSupplyPropNO3PlantLayer[i, j] = Utility.Math.Divide(tempNO3OnlySupply, potentialSupplyNitrogenPlantLayer[i, j], 0.0);
                    }
                    else
                    {
                        throw new Exception("Invalid potential uptake method selected");
                    }
                }
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
                {
                    // if the potential supply calculated above is greater than demand then scale it back - note that this is still a potential supply as a solo crop
                    potentialSupplyNitrogenPlantLayer[i, j] = potentialSupplyNitrogenPlantLayer[i, j] * Math.Min(1.0, Utility.Math.Divide(demandNitrogen[i], tempSupply, 0.0));
                }
            }  // close the plants loop - by here have the potentialSupply by plant and layer.  This is the sole plant demand - no competition yet

            // calculate the maximum amount of nitrogen available in each layer
            double[] totalAvailableNitrogen;
            totalAvailableNitrogen = new double[Soil.SoilWater.dlayer.Length];
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)
            {
                totalAvailableNitrogen[j] = Soil.SoilNitrogen.NO3[j] + Soil.SoilNitrogen.nh4[j];
            }

            // compare the potential nitrogen supply against the total available nitrogen
            // if supply exceeds demand then satisfy all demands, otherwise scale back by relative demand
            for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++) // loop through the layers in the outer loop
            {
                for (int i = 0; i < plants.Count; i++)
                {
                    uptakeNitrogenPlantLayer[i, j] = potentialSupplyNitrogenPlantLayer[i, j]
                                                   * Utility.Math.Constrain(Utility.Math.Divide(Utility.Math.Sum(totalAvailableNitrogen), Utility.Math.Sum(demandNitrogen), 0.0), 0.0, 1.0);
                    uptakeNitrogenPropNO3PlantLayer[i, j] = 0.0;
                    NUptakeType.DeltaNO3[j] += -1.0 * uptakeNitrogenPlantLayer[i, j] * potentialSupplyPropNO3PlantLayer[i, j];  // -ve to reduce water content in the soil
                    NUptakeType.DeltaNH4[j] += -1.0 * uptakeNitrogenPlantLayer[i, j] * (1.0 - potentialSupplyPropNO3PlantLayer[i, j]);  // -ve to reduce water content in the soil

                    if ((i == plants.Count - 1) && (j == 0))
                    {
                        Summary.WriteMessage(this, potentialSupplyNitrogenPlantLayer[0, j] + " " + potentialSupplyNitrogenPlantLayer[i, j] + " " + NUptakeType.DeltaNO3[j]);
                    }
                }
            }  // close the layer loop

            for (int i = 0; i < plants.Count; i++)
            {
                double[] dummyArray1 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing
                double[] dummyArray2 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing
                for (int j = 0; j < Soil.SoilWater.dlayer.Length; j++)           // cannot set a particular dimension from a 2D arrary into a 1D array directly so need a temporary variable
                {
                    dummyArray1[j] = uptakeNitrogenPlantLayer[i, j];
                    dummyArray2[j] = uptakeNitrogenPropNO3PlantLayer[i, j];
                }
                double testingvalue = Utility.Math.Sum(dummyArray1);
                plants[i].uptakeNitrogen = dummyArray1;
                plants[i].uptakeNitrogenPropNO3 = dummyArray2;
                // debugging into SummaryFile
                //Summary.WriteMessage(FullPath, "Arbitrator is setting the value of plants[" + i.ToString() + "].supplyWater(3) to  " + plants[i].supplyWater[3].ToString());
            }


            // send the change in soil soil nitrate and ammonium to the soil nitrogen module

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);

        }

        /// <summary>Zeroes the hamish variables.</summary>
        private void ZeroHamishVariables()
        {
            Utility.Math.Zero(potentialSupplyWaterPlantLayer);
            Utility.Math.Zero(uptakeWaterPlantLayer);
            Utility.Math.Zero(potentialSupplyNitrogenPlantLayer);
            Utility.Math.Zero(uptakeNitrogenPlantLayer);
            Utility.Math.Zero(uptakeNitrogenPropNO3PlantLayer);
            Utility.Math.Zero(extractableByPlant);
        }



    }


}
