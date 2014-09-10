using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Globalization;
using MathNet.Numerics.LinearAlgebra.Double;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A class representing a root system.
    /// </summary>
    public class RootSystem
    {
        public ICrop Crop;
        public Dictionary<string, double> SWStrength = new Dictionary<string,double>(); //SW source strength for each field/zone the crop is in
        public List<RootZone> RootZones;

        public double[] Uptake { get; set; }


    }

    /// <summary>
    /// A class representing a root zone in a field
    /// </summary>
    public class RootZone
    {
        /// <summary>
        /// The parent root system
        /// </summary>
        public RootSystem Parent;
        /// <summary>
        /// Name of the plant that owns this root zone.
        /// </summary>
        public string Name;
        /// <summary>
        /// The field/zone this root zone is in.
        /// </summary>
        public Zone Zone;
        /// <summary>
        /// Reference to the soil in the Zone for convienience.
        /// </summary>
        public Soil Soil;
        /// <summary>
        /// Depth of the roots.
        /// </summary>
        public double RootDepth;
        /// <summary>
        /// Root length density
        /// </summary>
        public double RootLengthDensity;
        /// <summary>
        /// An array that holds the potential sw uptake per soil layer.
        /// </summary>
        public double[] PotSWUptake;
    }

    public class UptakeInfo
    {
        public Zone Zone;
        public ICrop Plant;
        public double[] SWDep;
        public double[] Uptake;
        public double Strength;
    }

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        [Link]
        Simulation Simulation;
        List<RootSystem> RootSystems;
        List<RootZone> RootZones;
        List<Zone> Zones;

        // Initialize IFormatProvider to print matrix/vector data (debug - allows matrices to be printed properly)
        CultureInfo formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();

        /// <summary>
        /// The following event handler will be called once at the beginning of the simulation
        /// </summary>
        public override void OnSimulationCommencing()
        {
            RootSystems = new List<RootSystem>();
            RootZones = new List<RootZone>();
            Zones = new List<Zone>();

            formatProvider.TextInfo.ListSeparator = " ";
            //collect all zones in simulation
            Model[] ZoneAsModel = Simulation.FindAll(typeof(Zone));
            foreach (Model m in ZoneAsModel)
                Zones.Add((Zone)m);

            foreach (ICrop plant in Simulation.FindAll(typeof(ICrop)))
            {
                PMF.SimpleTree Tree = (PMF.SimpleTree)plant;
                string[] zoneList = Tree.zones.Split(',');
                int NumPlots = zoneList.Count();
                RootSystem currentSystem = new RootSystem();
                currentSystem.Crop = plant;
                currentSystem.RootZones = new List<RootZone>();

                foreach (string zone in zoneList)
                {
                    RootZone currentZone = new RootZone();
                    currentZone.Zone = (Zone)(this.Parent as Model).Find(zone.Trim());
                    if (currentZone.Zone == null)
                        throw new ApsimXException(this, "Could not find zone " + zone);
                    currentZone.Soil = (Soil)currentZone.Zone.Find(typeof(Soil));
                    if (currentZone.Soil == null)
                        throw new ApsimXException(this, "Could not find soil in zone " + zone);
                    currentZone.RootDepth = 500;
                    currentZone.Name = zone;
                    currentZone.Parent = currentSystem;
                    currentSystem.RootZones.Add(currentZone);
                }
                RootSystems.Add(currentSystem);
                plant.RootSystem = currentSystem;
            }
        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoSoilArbitration(object sender, EventArgs e)
        {
            List<UptakeInfo> InitSW = new List<UptakeInfo>();

            //construct list of SWDep for all zones
            foreach (RootSystem rs in RootSystems)
                foreach(RootZone rz in rs.RootZones)
            {
                InitSW.Add(new UptakeInfo()
                {
                    Zone = rz.Zone,
                    Plant = rs.Crop,
                    SWDep = rz.Soil.SoilWater.sw_dep,
                    Uptake = null,
                    Strength = 0
                });
            }

            int NumIterations = 2;
            List<UptakeInfo>[] Iterations = new List<UptakeInfo>[NumIterations];
            List<UptakeInfo> UptakeSums = new List<UptakeInfo>();

            // Begin modified euler method.
            // Calculate two uptakes with the second one using the SWDep returned by the first one.
            // Note some derived Lists are updated without being modifed in the orginal list.
            // Since the list members are a Class, they will be passed by reference not value
            // so updating a derived member will update the main List as well.
            for (int i = 0; i < NumIterations; i++) // two iterations
            {
                //send it to all RootSystems (plants) and have them send back their uptakes
                List<UptakeInfo> Run = new List<UptakeInfo>();
                foreach (RootSystem rs in RootSystems)
                {
                    Run.AddRange(rs.Crop.GetPotSWUptake(InitSW));
                }

                // go through all zones and get a new SWDep using given uptakes
                foreach (Zone z in Zones)
                {
                    List<UptakeInfo> ThisZone = Run.AsEnumerable().Where(x => x.Zone.Name.Equals(z.Name)).ToList();
                    UptakeInfo UptakeSum = new UptakeInfo();
                    UptakeSum.Uptake = new double[ThisZone[0].SWDep.Length];
                    UptakeSum.Zone = z;
                    double[] ZoneSWDep = ThisZone[0].SWDep;
                    foreach (UptakeInfo info in ThisZone)
                    {
                        ZoneSWDep = Utility.Math.Subtract(ZoneSWDep, info.Uptake);
                        UptakeSum.Uptake = Utility.Math.Add(UptakeSum.Uptake, info.Uptake); // add this uptake to total uptake for this Zone
                    }

                    //go through them again and set the new SWDep
                    foreach (UptakeInfo info in ThisZone)
                        info.SWDep = ZoneSWDep;
                    UptakeSums.Add(UptakeSum);
                }

                //Repeat with new SWDep
            }

            // Actual uptake then becomes the average.
            foreach (Zone z in Zones)
            {
                List<UptakeInfo> ThisZone = UptakeSums.AsEnumerable().Where(x => x.Zone.Name.Equals(z.Name)).ToList();
                if (ThisZone.Count != 2)
                    throw new ApsimXException(this, "Calculating Euler integration. Number of UptakeSums different to expected value of iterations.");
                 double[] ActualUptake = Utility.Math.Subtract(ThisZone[0].Uptake, ThisZone[1].Uptake);
                 Soil Soil = (Soil)z.Find(typeof(Soil));
                 Soil.SoilWater.sw_dep = Utility.Math.Subtract(Soil.SoilWater.sw_dep, ActualUptake);
            }


        }

        /// <summary>
        /// Calculate how deep roots are in a given layer as a proportion of the layer depth.
        /// </summary>
        /// <param name="layer">Layer number</param>
        /// <param name="root_depth">Depth of the roots</param>
        /// <param name="dlayer">Array of layer depths</param>
        /// <returns>Depth of the roots in the layer as percentage of the layer depth</returns>
        private double RootProportion(int layer, double root_depth, double[] dlayer)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += dlayer[i];
            depth_to_layer_top = depth_to_layer_bottom - dlayer[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = (double)Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / dlayer[layer];
        }

        /// <summary>
        /// Calculate the deepest layer containing roots.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="root_depth"></param>
        /// <param name="dlayer"></param>
        /// <returns>The index of the deepest layer containing roots</returns>
        private int CalcMaxRootLayer(double root_depth, double[] dlayer)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            for (int i = 0; i < dlayer.Length; i++)
            {
                depth_to_layer_bottom += dlayer[i];
                if (root_depth < depth_to_layer_bottom)
                    return i;
            }

            return dlayer.Length; //bottom layer
        }

        private List<RootZone> GetRootZonesInCurrentField(List<RootZone> rz, string Name)
        {
            IEnumerable<RootZone> output = rz.AsEnumerable().Where(x => x.Zone.Name.Equals(Name));
            return output.ToList();
        }
    }
}