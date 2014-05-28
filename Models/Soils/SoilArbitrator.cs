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
    public class RootSystem
    {
        public double SWDemand;
        public RootZone[] Zones;
    }

    public class RootZone
    {
        public string ZoneName;
        public double ZoneArea; //may not be needed as fields now contain area info
        public double RootDepth;
        public double RootLengthDensity;
        public double[] kl;
        public double[] ll;
        public double[] dlayer;
    }

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        [Link]
        Simulation paddock;

        Soil Soil;
        RootSystem RootData;
        DataTable AllRootSystems;

        // Initialize IFormatProvider to print matrix/vector data
        CultureInfo formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();


        // The following event handler will be called once at the beginning of the simulation
        public override void OnSimulationCommencing()
        {
            AllRootSystems = new DataTable();
            AllRootSystems.Columns.Add("ZoneName", typeof(string));
            AllRootSystems.Columns.Add("CropType", typeof(string));
            AllRootSystems.Columns.Add("SWDemand", typeof(double));
            AllRootSystems.Columns.Add("ZoneStrength", typeof(Dictionary<string, double>)); //KVP of zones and relative strengths for water extraction
            AllRootSystems.Columns.Add("RootSystemZone", typeof(RootZone));
            AllRootSystems.Columns.Add("HasRootSystem", typeof(bool));
            RootData = new RootSystem();
            formatProvider.TextInfo.ListSeparator = " ";
        }

        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {
            //set up data table
            int NumLayers = 0;
            AllRootSystems.Rows.Clear();
            List<Model> models = paddock.Children.AllRecursively();

            foreach (Model m in paddock.Children.AllRecursively())
            {
                string PlantStatus = (string)m.Variables.Get("plant_status");
                
                if (PlantStatus == null)//not a plant
                    continue;

                RootSystem RootData = null;

                    if (PlantStatus != "out") //if crop is not in ground, we don't care about it
                    {
                        RootData = (RootSystem)m.Variables.Get("RootSystem");
                        if (RootData != null) //crop has a RootData structre
                        {
                            Dictionary<string, double> SWStrength = CalcSWSourceStrength(RootData);
                            foreach (RootZone zone in RootData.Zones) //add each zone to the table
                                AllRootSystems.Rows.Add(zone.ZoneName, m.Name, RootData.SWDemand, SWStrength, zone, true);
                            NumLayers = RootData.Zones[0].kl.Length;
                        }
                        else //crop does not have RootData structure, so make one.
                        {
                            Dictionary<string, double> SWStrength = new Dictionary<string, double>();
                            Model Root = null;
                            RootData = new RootSystem();
                            RootData.Zones = new RootZone[1];
                            RootData.Zones[0] = new RootZone();
                            RootData.Zones[0].ZoneName = m.Name;
                            RootData.Zones[0].ZoneArea = 1;

                            Root = (Model)m.Scope.Find("Root");
                            if (Root==null)
                                throw new Exception("Could not get Root structure for crop " + m.Name);

                            try
                            { RootData.SWDemand = (double)m.Variables.Get("WaterDemand"); }
                            catch (Exception) { throw new Exception("Could not get SWDemand for crop " + m.Name); }

                            RootData.Zones[0].RootDepth = (double)Root.Variables.Get("RootDepth");
                            RootData.Zones[0].ll = (double[])Root.Variables.Get("ll");
                            RootData.Zones[0].kl = (double[])Root.Variables.Get("kl");

                            Soil = (Soil)m.Parent.Scope.Find(typeof(Soil));
                            SWStrength.Add(m.Name, 1);

                            RootData.Zones[0].dlayer = (double[])Soil.SoilWater.Variables.Get("dlayer");

                            AllRootSystems.Rows.Add(RootData.Zones[0].ZoneName, m.Name, RootData.SWDemand, SWStrength, RootData.Zones[0], true);
                            NumLayers = RootData.Zones[0].kl.Length;
                        }
                    }
            }
            //use LINQ to extract the paddocks for processing
            IEnumerable<string> paddockNames = AllRootSystems.AsEnumerable().Select<DataRow, string>(name => (string)name.ItemArray[0]).Distinct();

            //do water allocation for each paddock
            foreach (string PaddockName in paddockNames)
            {
                IEnumerable<DataRow> RootZones = AllRootSystems.AsEnumerable().Where(row => row.ItemArray[0].Equals(PaddockName));
                Model p = (Model)paddock.Scope.Find(PaddockName);
                Model fieldProps = (Model)p.Scope.Find("FieldProps");
                double fieldArea = (double)p.Variables.Get("Area");
//                if (fieldProps == null)
//                    throw new Exception("Could not find FieldProps component in field " + PaddockName);

                Soil Soil = (Soil)p.Scope.Find(typeof(Soil));
                double[] SWDep = (double[])Soil.SoilWater.Variables.Get("sw_dep");
                double[] dlayer = (double[])Soil.SoilWater.Variables.Get("dlayer");
                double[] CropSWDemand = new double[RootZones.Count()];
                for (int i = 0; i < RootZones.Count(); i++) //get demand for all crops in paddock using relative SW strength
                {
                    Dictionary<string, double> PaddockSWDemands = (Dictionary<string, double>)RootZones.ToArray()[i].ItemArray[3];
                    CropSWDemand[i] = PaddockSWDemands[p.Name] * (double)RootZones.ToArray()[i].ItemArray[2];
                }
                double[,] RelKLStrength = CalcRelKLStrength(RootZones, CropSWDemand);
                double[,] RelSWLayerStrength = CalcRelSWLayerStrength(RootZones, SWDep, NumLayers);
                double[,] SWSupply = CalcSWSupply(RootZones, SWDep, NumLayers);

                double[,] LayerUptake = new double[RootZones.Count(), NumLayers];
                double[] LastCropSWDemand;
                double[,] LastSWSupply;

                int count = 0;
                do
                {
                    count++;
                    LastCropSWDemand = CropSWDemand;
                    LastSWSupply = SWSupply;

                    for (int i = 0; i < RootZones.Count(); i++) //get as much water as possible for the layer using relative kl strengths
                    {
                        RootZone Zone = (RootZone)RootZones.ToArray()[i].ItemArray[4];
                        for (int j = 0; j < NumLayers; j++)
                        {
                            if (Utility.Math.Sum(CropSWDemand) < Utility.Math.Sum(SWSupply))
                            {
                                LayerUptake[i, j] = CropSWDemand[i] * RelSWLayerStrength[i, j];
                            }
                            else
                                LayerUptake[i, j] = SWSupply[i, j] * RelKLStrength[j, i] * RootProportion(j, Zone.RootDepth, dlayer);

                            if (LayerUptake[i, j] < 0)
                                throw new Exception("Layer uptake should not be negative");
                        }
                    }

                    DenseMatrix Uptake = DenseMatrix.OfArray(LayerUptake);
                  //  Model CurrentPaddock;
                    Model CurrentCrop;
                    for (int i = 0; i < RootZones.Count(); i++) //subtract taken water from the supply and demand
                    {
                        CurrentCrop = (Model)p.Variables.Get((string)RootZones.ToArray()[i].ItemArray[0]);
                      //  CurrentCrop = (Model)CurrentPaddock.Get((string)RootZones.ToArray()[i].ItemArray[1]);
                        CropSWDemand[i] -= Uptake.Row(i).Sum();
                        if (CurrentCrop != null && CurrentCrop.Name.ToLower().Equals("maize"))
                        {
                            CurrentCrop.Variables.Set("SWUptake", Uptake.Row(i).ToArray());
                        }
                        for (int j = 0; j < NumLayers; j++)
                        {
                            SWSupply[i, j] -= LayerUptake[i, j];
                        }
                    }

                    //subtract from soil water
                   for (int j = 0; j < Uptake.ColumnCount; j++)
                    {
                        SWDep[j] -= Uptake.Column(j).Sum() / fieldArea;
                    }

                   Soil.SoilWater.Variables.Set("sw_dep", SWDep);
                } while (Utility.Math.Sum(LastCropSWDemand) != Utility.Math.Sum(CropSWDemand) && Utility.Math.Sum(LastSWSupply) != Utility.Math.Sum(SWSupply));
            }
        }

        /// <summary>
        /// For each crop, calculate the maximum water uptake for each layer.
        /// </summary>
        /// <param name="CropSWDemand">An array holding the SW demand for each crop.</param>
        /// <param name="RelSWLayerStrength">The relative source strength for each crop and layer</param>
        /// <param name="NumLayers">Number of layers in the soil profile</param>
        /// <param name="RootZones">The rootzones to process in current paddock</param>
        /// <param name="dlayer">Layer depth array</param>
        /// <returns>A 2D array containing the maximum uptake for each crop and layer</returns>
        private double[,] CalcCropSWLayerUptake(double[] CropSWDemand, double[,] RelSWLayerStrength, int NumLayers, IEnumerable<DataRow> RootZones, double[] dlayer)
        {
            double[,] CropSWLayerUptake = new double[CropSWDemand.Length, NumLayers];
            for (int i = 0; i < CropSWDemand.Length; i++)
            {
                RootZone Zone = (RootZone)RootZones.ToArray()[i].ItemArray[4];
                for (int j = 0; j < NumLayers; j++)
                {
                    CropSWLayerUptake[i, j] = CropSWDemand[i] * RelSWLayerStrength[i, j] * RootProportion(j, Zone.RootDepth, dlayer);
                }
            }

            return CropSWLayerUptake;
        }

        /// <summary>
        /// Calculate the amount of water available to each crop on a per layer basis.
        /// As crops will have different lower limits, they can have a different supply.
        /// </summary>
        /// <param name="RootZones">The rootzones to process in current paddock</param>
        /// <param name="SWDep">An array containing depth of soil water per layer (mm/mm)</param>
        /// <param name="NumLayers">Number of layers in the soil profile</param>
        /// <returns>A 2D array containing the SW supply available for each crop and layer.</returns>
        private double[,] CalcSWSupply(IEnumerable<DataRow> RootZones, double[] SWDep, int NumLayers)
        {
            RootZone zone = new RootZone();
            double[,] SWSupply = new double[RootZones.Count(), NumLayers];
            for (int i = 0; i < RootZones.Count(); i++) //crops
            {
                zone = (RootZone)RootZones.ToArray()[i].ItemArray[4];
                for (int j = 0; j < NumLayers; j++)
                {
                    SWSupply[i, j] = zone.kl[j] * (SWDep[j] - zone.ll[j] * zone.dlayer[j]) * zone.ZoneArea;
                    if (SWSupply[i, j] < 0)
                        SWSupply[i, j] = 0; //can be < 0 if another crop with a lower LL has extracted below what this one can.
                }
            }
            return SWSupply;
        }


        /// <summary>
        /// Using the depth of soil water and current crop kl value, calculate a relative layer source strength for each crop.
        /// </summary>
        /// <param name="RootZones">The rootzones to process in current paddock</param>
        /// <param name="SWDep">An array containing depth of soil water per layer (mm/mm)</param>
        /// <param name="NumLayers">Number of layers in the soil profile</param>
        /// <returns>A 2D array containing the relative source strength for each crop and layer.</returns>
        private double[,] CalcRelSWLayerStrength(IEnumerable<DataRow> RootZones, double[] SWDep, int NumLayers)
        {
            RootZone zone = new RootZone();
            double[,] RelSWLayerStrength = new double[RootZones.Count(), NumLayers];
            for (int i = 0; i < RootZones.Count(); i++) //crops
            {
                double TotalSource = 0;
                zone = (RootZone)RootZones.ToArray()[i].ItemArray[4];
                int DeepestRoot = CalcMaxRootLayer(zone.RootDepth, zone.dlayer);
                for (int j = 0; j < NumLayers; j++)
                    if (j <= DeepestRoot)
                        TotalSource += zone.kl[j] * SWDep[j];
                for (int j = 0; j < NumLayers; j++)
                    if (j <= DeepestRoot && zone.kl[j] > 0)
                        RelSWLayerStrength[i, j] = zone.kl[j] * SWDep[j] / TotalSource;
                    else
                        RelSWLayerStrength[i, j] = 0;
            }
            return RelSWLayerStrength;
        }

        /// <summary>
        /// Calculate relative strength of crop kl for each layer.
        /// Will be 1 for a single crop.
        /// </summary>
        /// <param name="RootZones">The rootzones to process in current paddock</param>
        /// <returns>A 2D array containing the relative kl strength of each crop and layer.</returns>
        private double[,] CalcRelKLStrength(IEnumerable<DataRow> RootZones, double[] CropSWDemand)
        {
            double[][] KLArray = new double[RootZones.Count()][];
            int[] LowestRootLayer = new int[RootZones.Count()];
            for (int i = 0; i < KLArray.GetLength(0); i++) //extract the kl array from each zone
            {
                RootZone zone = (RootZone)RootZones.ToArray()[i].ItemArray[4];
                KLArray[i] = zone.kl;
                for (int j = 0; j < KLArray[i].Length; j++)
                    if (CropSWDemand[i] == 0)
                        KLArray[i][j] = 0;
                LowestRootLayer[i] = CalcMaxRootLayer(zone.RootDepth, zone.dlayer);
            }

            //calculate relative demand strength for each layer
            double[,] RelKLStrength = new double[KLArray[0].Length, KLArray.Length];
            for (int i = 0; i < KLArray[0].Length; i++) //layer
            {
                double KLSum = 0;
                for (int j = 0; j < KLArray.Length; j++) //for the current layer, sum the kl's of each crop in the layer
                {
                    if (i <= LowestRootLayer[j])
                        KLSum += KLArray[j][i];
                }
                for (int j = 0; j < KLArray.Length; j++) //use those summed kl's to calculate the relative kl strength for each crop in the layer
                {
                    if (i <= LowestRootLayer[j] && KLArray[j][i] > 0)
                        RelKLStrength[i, j] = KLArray[j][i] / KLSum;
                    else
                        RelKLStrength[i, j] = 0;
                }
            }
            return RelKLStrength;
        }

        /// <summary>
        /// Calculate the best paddocks to take water from when a crop is in multiple root zones.
        /// This method calculates the relative source strength of each paddock.
        /// </summary>
        /// <param name="RootData">A RootData structure provided by a crop.</param>
        /// <returns>A Dictionary containing the paddock names and relative strengths</returns>
        private Dictionary<string, double> CalcSWSourceStrength(RootSystem RootData)
        {
            Dictionary<string, double> SoilWaters = new Dictionary<string, double>();
            string[] ZoneNames = new string[RootData.Zones.Length];
            double[] SWDeps = new double[RootData.Zones.Length];
            double TotalSW;
            Model p;
            Soil Soil;

            for (int i = 0; i < ZoneNames.Length; i++)
            {
                double[] SWlayers;
                ZoneNames[i] = RootData.Zones[i].ZoneName;
                p = (Model)paddock.Scope.Find(ZoneNames[i]);
                Soil = (Soil)p.Scope.Find(typeof(Soil));
                SWlayers = (double[])Soil.SoilWater.Variables.Get("sw_dep");
                SWDeps[i] = (double)Utility.Math.Sum(SWlayers);
            }

            TotalSW = (double)Utility.Math.Sum(SWDeps);
            for (int i = 0; i < ZoneNames.Length; i++)
                SoilWaters.Add(ZoneNames[i], SWDeps[i] / TotalSW * RootData.Zones[i].ZoneArea);

            return SoilWaters;
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
    }
}