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
        public double SWDemand; //TODO remove
        public Dictionary<string, double> SWStrength = new Dictionary<string,double>(); //SW source strength for each field/zone the crop is in
        public List<RootZone> RootZones;

        /// <summary>
        /// Calculate the best paddocks to take water from when a crop is in multiple root zones.
        /// This method calculates the relative source strength of each paddock.
        /// </summary>
        /// <param name="RootSystem">A RootData structure provided by a crop.</param>
        /// <returns>A Dictionary containing the paddock names and relative strengths</returns>
        public Dictionary<string, double> CalcSWSourceStrength() //TODO move to rootsystem
        {
            Dictionary<string, double> SoilWaters = new Dictionary<string, double>();
            string[] ZoneNames = new string[RootZones.Count];
            double[] SWDeps = new double[RootZones.Count];
            double TotalSW;

            for (int i = 0; i < ZoneNames.Length; i++)
            {
                double[] SWlayers;
                ZoneNames[i] = RootZones[i].Zone.Name;

                SWlayers = (double[])RootZones[i].Soil.SoilWater.sw_dep;
                SWDeps[i] = (double)Utility.Math.Sum(SWlayers);
            }

            TotalSW = (double)Utility.Math.Sum(SWDeps);
            for (int i = 0; i < ZoneNames.Length; i++)
                SoilWaters.Add(ZoneNames[i], SWDeps[i] / TotalSW); //TODO subtract ll // * RootData.RootZones[i].Zone.Area); //ignore area for now; need to find a better way of implementing it

            return SoilWaters;
        }
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

    public struct UptakeInfo
    {
        public string ZoneName;
        public double[] SWDep;
        public double[] Uptake;
    }

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        [Link]
        Simulation Simulation;
        List<RootSystem> RootSystems = new List<RootSystem>();
        List<RootZone> RootZones = new List<RootZone>();

        // Initialize IFormatProvider to print matrix/vector data (debug - allows matrices to be printed properly)
        CultureInfo formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();

        /// <summary>
        /// The following event handler will be called once at the beginning of the simulation
        /// </summary>
        public override void OnSimulationCommencing()
        {
            formatProvider.TextInfo.ListSeparator = " ";


            foreach (ICrop plant in Simulation.FindAll(typeof(ICrop)))
            {
                PMF.SimpleTree Tree = (PMF.SimpleTree)plant;
                string[] zoneList = Tree.zones.Split(',');
                int NumPlots = zoneList.Count();
                RootSystem currentSystem = new RootSystem();
                RootZone currentZone = new RootZone();

                foreach (string zone in zoneList)
                {
                    currentSystem.Crop = plant;
                    currentSystem.RootZones = new List<RootZone>();
                    currentZone.Zone = (Zone)(this.Parent as Model).Find(zone.Trim());
                    if (currentZone.Zone == null)
                        throw new ApsimXException(this.FullPath, "Could not find zone " + zone);
                    currentZone.Soil = (Soil)currentZone.Zone.Find(typeof(Soil));
                    if (currentZone.Soil == null)
                        throw new ApsimXException(this.FullPath, "Could not find soil in zone " + zone);
                    currentZone.RootDepth = 500;
                    currentZone.Name = Name;
                    currentZone.Parent = currentSystem;
                    currentSystem.RootZones.Add(currentZone);
                }
                RootSystems.Add(currentSystem);            }
        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoSoilArbitration(object sender, EventArgs e)
        {
            List<RootZone> RootZones = new List<RootZone>(); //get all rootZones
            List<UptakeInfo> Uptakes = new List<UptakeInfo>();

            //send all plants available sw_dep, will need to make it multi zone aware
            //also add RootZones to main list
            foreach (RootSystem rs in RootSystems)
            {
                Uptakes.Add(rs.Crop.GetPotSWUptake(new UptakeInfo() { ZoneName = rs.RootZones[0].Zone.Name, 
                                                       SWDep = rs.RootZones[0].Soil.SoilWater.sw_dep, 
                                                       Uptake = new double[rs.RootZones[0].Soil.Thickness.Length] }));
                RootZones.AddRange(rs.RootZones);
            }

            // science

            //send plants actual water uptake

            //modify soil water
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