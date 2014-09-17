using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF;
using Models.Soils;

namespace Models.ArbitratorGod
{

    /// <summary>
    /// Vals u-beaut (VS - please note that DH wrote that) soil arbitrator
    /// 
    /// TODO - convert to god-level
    /// TODO - need to figure out how to get multi-zone root systems into this - structure there to do the uptake but note comments in code related to needs
    /// TODO - alternate uptake methods
    /// TODO - currently is implicitly 1 ha per zone - need to convert into proper consideration of area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ArbitratorGod : Model
    {

        [Link]
        Simulation Simulation;

        [Link]
        WeatherFile Weather = null;

        [Link]
        Summary Summary = null;

        [Link]
        Clock Clock = null;

        //[Link]
        //Zone mypaddock;

        //ICrop2[] plants;

        /// <summary>
        /// Reference to the soil in the Zone for convienience.
        /// </summary>
        //public Soil Soil;


        #region // initial definitions

        /// <summary>
        /// This will hold a range of arbitration methods for testing - will eventually settle on one standard method
        /// </summary>
        [Description("Arbitration method: old / new - use old - new only for testing")]
        public string ArbitrationMethod { get; set; }

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
        double[,] uptakeWaterPlantLayer;
        double[,] potentialSupplyNitrogenPlantLayer;
        double[,] potentialSupplyPropNO3PlantLayer;
        double[,] uptakeNitrogenPlantLayer;
        double[,] uptakeNitrogenPropNO3PlantLayer;

        // soil water evaporation stuff
        //public double ArbitEOS { get; set; }  //

        /// <summary>
        /// The NitrogenChangedDelegate is for the Arbitrator to set the change in nitrate and ammonium in SoilNitrogen
        /// Eventually this should be changes to a set rather than an event
        /// </summary>
        /// <param name="Data"></param>
        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        /// <summary>
        /// To publish the nitrogen change event
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

        // new Arbitration parameters from here

        /// <summary>
        /// Soil water demand (mm) from each of the plants being arbitrated
        /// </summary>
        double[] demandWater;

        /// <summary>
        /// Nitrogen demand (mm) from each of the plants being arbitrated
        /// </summary>
        double[] demandNitrogen;

        /// <summary>
        /// The number of zones under consideration
        /// </summary>
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

        /// <summary>
        /// This is "water" or "nitrogen" and holds the resource that is being arbitrated
        /// </summary>
        string resourceToArbitrate;

        /// <summary>
        /// used as a counter for zones
        /// </summary>
        int z = 1;


        #endregion  // defintions
          

        /// <summary>
        /// Runs at the start of the simulation, here only reads the aribtration method to be used
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            ZeroDailyVariables();

            // get the list of crops in the simulation
            //plants = this.Plants;

            // TODO - find out about zones and areas and plants in each

            // not needed if not used for energy arbitration as well
            // myCanopy = new CanopyProps[plants.Length, 0];  // later make this the layering in the canopy
            // for (int i = 0; i < plants.Length; i++)
            // {
            //    myCanopy[0, 0].laiGreen = plants[i].CanopyProperties.LAI;
            // }

            // set some dimensions
            // when get the zone connections will have to find plants across all zones and the layer dimension will be the maximum number of layers across all zones

            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

            }
            //zones = 1; // as a starting point - will we need to consider redimensioning?  Depends on when the root system information is available - or just allow for all zones beneath current location

            bounds = maxPlants;  // the number of bounds cannot exceed the number of plants
            forms = 2;  // 2 for nitrogen and 1 for water
            demandWater = new double[maxPlants];
            demandNitrogen = new double[maxPlants];
            rootExploration = new double[maxPlants, maxLayers, zones];
            lowerBound = new double[maxPlants, maxLayers, zones, bounds];
            uptakePreference = new double[maxPlants, maxLayers, zones];
            resource = new double[maxPlants, maxLayers, zones, bounds, forms];
            extractable = new double[maxPlants, maxLayers, zones, bounds, forms];
            demand = new double[maxPlants, maxLayers, zones, bounds, forms];
            demandForResource = new double[maxPlants, maxLayers, zones, bounds, forms];
            totalForResource = new double[maxPlants, maxLayers, zones, bounds, forms];
            uptake = new double[maxPlants, maxLayers, zones, bounds, forms];
            extractableByPlant = new double[maxPlants];
        }
        
        /// <summary>
        /// Zero variables at the start of each day
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
         
            /*
             // Iterate through all child paddocks.
            //int z = -1;
          //foreach (Zone paddock in mypaddock.FindAll(typeof(Zone)))
            foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                z += 1;
                // Find soil in paddock.
                Soil Soil = (Soil)zone.Find(typeof(Soil));
                //double[] SWDep = Soil.SoilWater.sw_dep;

                // Find plants in paddock.
                ICrop2[] plants = zone.Plants;
                double[] test = new double[plants.Length];

                for (int p = 0; p < plants.Length; p++)
                {
                    demandWater[p, z] = plants[p].demandWater;
                    //string myString = "Hello " + Soil.SoilWater.dlayer[0];
                    //Summary.WriteMessage(FullPath, myString);
                }
            }
            */

            ZeroDailyVariables();
        }

        /// <summary>
        /// Do the arbitration between the plants for water
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {

            
            /*
            int z = -1;
            foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                z += 1;
                // Find soil in paddock.
                Soil Soil = (Soil)zone.Find(typeof(Soil));

                // Find plants in paddock.
                ICrop2[] plants = zone.Plants;
                double[] test = new double[plants.Length];

                for (int p = 0; p < plants.Length; p++)
                {
                    for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
                    {
                        plants[p].uptakeWater[l] = demandWater[p, z] + z;
                    }
                }
            }
            */


                ZeroCalculationVariables();
                forms = 1;
                CalculateLowerBounds("water");
                CalculateResource("water");
                CalculateExtractable("water");
                CalculateDemand("water");
                CalculateUptake("water");
                SetWaterUptake("water");


        }

        /// <summary>
        /// Do the arbitration between the plants for nutrients
        /// This is only set up for nitrogen at the moment but is designed to be extended to other nutrients 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoNutrientArbitration")]
        private void OnDoNutrientArbitration(object sender, EventArgs e)
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
        
        /// <summary>
        /// Calculate the results of the competition between the plants for light and calculate the soil water demadn by the plants and or soil water evaporation
        /// Not currently implemented
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Zero the values at the start of each day
        /// </summary>
        private void ZeroDailyVariables()
        {
            Utility.Math.Zero(demandWater);
            Utility.Math.Zero(demandNitrogen);
            Utility.Math.Zero(rootExploration);
            Utility.Math.Zero(lowerBound);
            Utility.Math.Zero(uptakePreference);
        }

        /// <summary>
        /// Zero the values that are needed for each set of arbitration calculations
        /// </summary>
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
        /// <param name="resourceToArbitrate"></param>
        private void CalculateLowerBounds(string resourceToArbitrate)
        {
            // set up the bounds here  
            // the tolerance is used to trmpoarily convert the double array of lower limits into an integer array.  This then allows the usage of Distinct to remove duplicates.
            // At present set tolerance to a low value - this will end up with more bounds but will be more accurate with the available water to plants
            double tolerance = 0.001;
            if (resourceToArbitrate.ToLower() == "water")
            {

                zones = -1;
                int maxPlants = 0;
                int maxLayers = 0;
                foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
                {
                    zones += 1;

                    // Find plants in paddock.
                    List<ICrop2> plants = zone.Plants;
                    maxPlants = Math.Max(plants.Count, maxPlants);

                    // Find soil in paddock.
                    Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                    maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                    int[] tempBoundsArray = new int[plants.Count];

                    for (int l = 0; l < maxLayers; l++)
                    {
                        // get the lower bounds from the plants and put them into a temporary array for sorting, comparison and shortening before assigning bounds
                        // for the unique values, divide each element by the tolerance (which is mm diference that is interesting) and convert to an integer
                        // then sort, reverse and then use the Distinct method to shorten the array into functionally different values
                        // lastly multiply by the tolerance again to get the bounds in mm
                        // this is redundant but non-troublesome for a single-plant simulation
                        for (int p = 0; p < maxPlants; p++)
                        {
                            tempBoundsArray[p] = Convert.ToInt32(Math.Round(plants[p].RootProperties.LowerLimitDep[l] / tolerance, 0));
                        }

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
                zones = -1;
                int maxPlants = 0;
                int maxLayers = 0;
                foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
                {
                    zones += 1;

                    // Find plants in paddock.
                    List<ICrop2> plants = zone.Plants;
                    maxPlants = Math.Max(plants.Count, maxPlants);

                    // Find soil in paddock.
                    Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                    maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                    for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
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
        /// <param name="resourceToArbitrate"></param>
        private void CalculateResource(string resourceToArbitrate)
        {
            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                for (int p = 0; p < maxPlants; p++)
                {
                    for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
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
                                        resource[p, l, z, b, f] = Math.Max(0.0, Soil.SoilNitrogen.no3[l]);
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
        /// Calculate the amount that the plant could potentially exctract if it was the only plant in the simulation and demand > resource
        /// </summary>
        /// <param name="resourceToArbitrate"></param>
        private void CalculateExtractable(string resourceToArbitrate)
        {
            // calculate extractable 
            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                for (int p = 0; p < maxPlants; p++)
                {
                    for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)
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
                                                                   * Soil.SoilNitrogen.no3ppm[l]
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
        /// <param name="resourceToArbitrate"></param>
        private void CalculateDemand(string resourceToArbitrate)
        {
            // calculate demand distributed over layers etc
            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                for (int p = 0; p < maxPlants; p++)
                {
                    for (int l = 0; l < maxLayers; l++)
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

        /// <summary>
        /// Calculate the uptake for each plant across the layers, zones, bounds, and forms
        /// </summary>
        /// <param name="resourceToArbitrate"></param>
        private void CalculateUptake(string resourceToArbitrate)
        {
            // calculate uptake distributed over layers etc - for now bounds and forms = 1
            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                for (int p = 0; p < maxPlants; p++)
                {
                    for (int l = 0; l < maxLayers; l++)
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
        /// <param name="resourceToArbitrate"></param>
        private void SetWaterUptake(string resourceToArbitrate)
        {
            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                double[] dummyArray1 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing - will have to re-think this with when zones come in
                double[] dltSWdep = new double[Soil.SoilWater.dlayer.Length];   // to hold the changes in soil water depth
                for (int p = 0; p < maxPlants; p++)
                {
                    for (int l = 0; l < maxLayers; l++)
                    {
                        for (int b = 0; b < bounds; b++)
                        {
                            dummyArray1[l] += uptake[p, l, z, b, 0];       // only 1 form but need to sum across bounds 
                            dltSWdep[l] += -1.0 * uptake[p, l, z, b, 0];   // -ve to reduce water content in the soil
                        }
                    }
                    plants[p].uptakeWater = dummyArray1;   // will this work when plants cross zones?
                }
                Soil.SoilWater.dlt_sw_dep = dltSWdep;   // assume this gets zeroed each zone loop?
            }
        }

        /// <summary>
        /// Send the nitrogen uptake arrays back to the plants and send the change in nitrogen back to the soil
        /// </summary>
        /// <param name="resourceToArbitrate"></param>
        private void SetNitrogenUptake(string resourceToArbitrate)
        {
            zones = -1;
            int maxPlants = 0;
            int maxLayers = 0;
            foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))  //foreach (Zone zone in Simulation.FindAll(typeof(Zone)))
            {
                zones += 1;

                // Find plants in paddock.
                List<ICrop2> plants = zone.Plants;
                maxPlants = Math.Max(plants.Count, maxPlants);

                // Find soil in paddock.
                Soil Soil = (Soil)Apsim.Find(zone, typeof(Soil));
                maxLayers = Math.Max(Soil.SoilWater.dlayer.Length, maxLayers);

                double[] dummyArray1 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing - will have to re-think this with when zones come in
                double[] dummyArray2 = new double[Soil.SoilWater.dlayer.Length];  // have to create a new array for each plant to avoid the .NET pointer thing - will have to re-think this with when zones come in

                // move this inside the zone loop - needs to get zeroed for each seperate zone
                NitrogenChangedType NUptakeType = new NitrogenChangedType();
                NUptakeType.Sender = Name;
                NUptakeType.SenderType = "Plant";
                NUptakeType.DeltaNO3 = new double[Soil.SoilWater.dlayer.Length];
                NUptakeType.DeltaNH4 = new double[Soil.SoilWater.dlayer.Length];

                for (int p = 0; p < maxPlants; p++)
                {
                    for (int l = 0; l < maxLayers; l++)
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
                    // set uptakes in each plant
                    plants[p].uptakeNitrogen = dummyArray1;
                    for (int l = 0; l < Soil.SoilWater.dlayer.Length; l++)  // don't forget to deal with zones at some point
                    {
                        dummyArray2[l] = Utility.Math.Divide(dummyArray2[l], dummyArray1[l], 0.0);  // would be nice to have a utility for this
                    }
                    plants[p].uptakeNitrogenPropNO3 = dummyArray2;
                }
                // and finally set the changed soil resources
                if (NitrogenChanged != null)
                    NitrogenChanged.Invoke(NUptakeType);
            }

        }

    }
}
