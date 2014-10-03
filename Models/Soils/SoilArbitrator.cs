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
    /// <summary>A class representing a root system.</summary>
    [Serializable]
    public class RootSystem
    {
        /// <summary>The crop</summary>
        public ICrop Crop;
        /// <summary>The root zones</summary>
        public List<RootZone> RootZones;
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The uptake.</value>
        public double[] Uptake { get; set; }


    }

    /// <summary>A class representing a root zone in a field</summary>
    public class RootZone
    {
        /// <summary>The parent root system</summary>
        [NonSerialized]
        public RootSystem Parent;
        /// <summary>Name of the plant that owns this root zone.</summary>
        public string Name;
        /// <summary>The field/zone this root zone is in.</summary>
        public Zone Zone;
        /// <summary>Reference to the soil in the Zone for convienience.</summary>
        public Soil Soil;
        /// <summary>Depth of the roots.</summary>
        public double RootDepth;
        /// <summary>Root length density</summary>
        public double RootLengthDensity;
        /// <summary>An array that holds the potential sw uptake per soil layer.</summary>
        public double[] PotSWUptake;
    }

    /// <summary>
    /// 
    /// </summary>
    public class UptakeInfo
    {
        /// <summary>The zone</summary>
        public Zone Zone;
        /// <summary>The plant</summary>
        public ICrop Plant;
        /// <summary>The sw dep</summary>
        public double[] SWDep;
        /// <summary>The uptake</summary>
        public double[] Uptake;
        /// <summary>The strength</summary>
        public double Strength;
    }

    /// <summary>
    /// A soil arbitrator model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        /// <summary>The simulation</summary>
        [Link]
        Simulation Simulation;
        /// <summary>The summary file</summary>
        [Link]
        Summary SummaryFile;
        /// <summary>The root systems</summary>
        List<RootSystem> RootSystems;
        /// <summary>The root zones</summary>
        List<RootZone> RootZones;
        /// <summary>The zones</summary>
        List<Zone> Zones;

        // Initialize IFormatProvider to print matrix/vector data (debug - allows matrices to be printed properly)
        /// <summary>The format provider</summary>
        CultureInfo formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();

        /// <summary>
        /// The following event handler will be called once at the beginning of the simulation
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">
        /// Could not find zone  + zone
        /// or
        /// Could not find soil in zone  + zone
        /// </exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            RootSystems = new List<RootSystem>();
            RootZones = new List<RootZone>();
            Zones = new List<Zone>();

            formatProvider.TextInfo.ListSeparator = " ";
            //collect all zones in simulation
            List<IModel> ZoneAsModel = Apsim.FindAll(Simulation, typeof(Zone));
            foreach (Model m in ZoneAsModel)
            {
                if (m.GetType() == typeof(Zone))
                    Zones.Add((Zone)m);
            }
            foreach (ICrop plant in Apsim.FindAll(Simulation, typeof(ICrop)))
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
                    currentZone.Zone = (Zone)Apsim.Find(this.Parent, zone.Trim());
                    if (currentZone.Zone == null)
                        throw new ApsimXException(this, "Could not find zone " + zone);
                    currentZone.Soil = (Soil)Apsim.Find(currentZone.Zone, typeof(Soil));
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

        /// <summary>Called when [do soil arbitration].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Calculating Euler integration. Number of UptakeSums different to expected value of iterations.</exception>
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
                    Run.AddRange(rs.Crop.GetSWUptake(InitSW));
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
                if (ThisZone.Count != NumIterations)
                    throw new ApsimXException(this, "Calculating Euler integration. Number of UptakeSums different to expected value of iterations.");
                double[] ActualUptake = Utility.Math.Add(ThisZone[0].Uptake, ThisZone[1].Uptake); //will need to change if we go to more iterations
                ActualUptake = Utility.Math.Divide_Value(ActualUptake, NumIterations);
                Soil Soil = (Soil)Apsim.Find(z,(typeof(Soil)));
                Soil.SoilWater.sw_dep = Utility.Math.Subtract(Soil.SoilWater.sw_dep, ActualUptake);
                SummaryFile.WriteMessage(this, z.Name + " " + String.Join(" ", ActualUptake.Select(x => x.ToString()).ToArray()));
            }

            // Calculate plant uptake
            foreach (RootSystem rs in RootSystems)
            {
                PMF.SimpleTree Tree = (PMF.SimpleTree)rs.Crop;
                int NumUptakes = Tree.Uptakes.Count;
                double[] TotalUptake = new double[Tree.Uptakes[0].Uptake.Length];

                foreach (UptakeInfo uptake in Tree.Uptakes)
                {
                    TotalUptake = Utility.Math.Add(TotalUptake, uptake.Uptake);
                }
                Tree.Uptake = Utility.Math.Divide_Value(TotalUptake, Tree.Uptakes.Count);
            }
        }

        /// <summary>Calculate how deep roots are in a given layer as a proportion of the layer depth.</summary>
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

        /// <summary>Calculate the deepest layer containing roots.</summary>
        /// <param name="root_depth">The root_depth.</param>
        /// <param name="dlayer">The dlayer.</param>
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

        /// <summary>Gets the root zones in current field.</summary>
        /// <param name="rz">The rz.</param>
        /// <param name="Name">The name.</param>
        /// <returns></returns>
        private List<RootZone> GetRootZonesInCurrentField(List<RootZone> rz, string Name)
        {
            IEnumerable<RootZone> output = rz.AsEnumerable().Where(x => x.Zone.Name.Equals(Name));
            return output.ToList();
        }
    }
}