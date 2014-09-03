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
        public Dictionary<string, double> SWStrength = new Dictionary<string,double>(); //SW source strength for each field/zone the crop is in
        public List<RootZone> RootZones;
    }

    public class RootZone
    {
        public Zone Zone;
        public Soil Soil;
        public double RootDepth;
        public double RootLengthDensity;
        public double[] PotSWUptake;
    }

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        [Link]
        Simulation paddock;

    /*    Soil Soil;
        RootSystem RootData;
        DataTable AllRootSystems;*/

        // Initialize IFormatProvider to print matrix/vector data (debug - allows matrices to be printed properly)
        CultureInfo formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();


        // The following event handler will be called once at the beginning of the simulation
        public override void OnSimulationCommencing()
        {
            formatProvider.TextInfo.ListSeparator = " ";
        }

        [EventSubscribe("DoWaterArbitration")]
        private void OnDoSoilArbitration(object sender, EventArgs e)
        {
            return; //TODO: figure out why there are plants; see below
            //set up data table
            int NumLayers = 0;
        /*    AllRootSystems.Rows.Clear();
            List<Model> models = paddock.Children.AllRecursively;*/

            //may not need this; we'll just do all fields
          /*  foreach (ICrop crop in paddock.Plants)
            {
                // only process if crop can grow (i.e is in ground).
                if (crop.PotentialEP > 0)
                {
                    crop.RootSystem.SWStrength = CalcSWSourceStrength(crop.RootSystem);
                    foreach (RootZone RootZone in crop.RootSystem.RootZones) //add each zone to the table
                        AllRootSystems.Rows.Add(RootZone.Zone.Name, crop.CropType, RootData.SWDemand, crop.RootSystem.SWStrength, RootZone, true);
                    NumLayers = RootData.RootZones[0].Soil.KL(crop.CropType).Length;
                }
            }*/
            //use LINQ to extract the paddocks for processing
       //     IEnumerable<string> zoneNames = AllRootSystems.AsEnumerable().Select<DataRow, string>(name => (string)name.ItemArray[0]).Distinct();

            //do water allocation for each paddock
            foreach (Zone zone in paddock.FindAll(typeof(Zone)))
            {
                string ZoneName = zone.Name;
                Dictionary<string, double> PaddockSWDemands = new Dictionary<string,double>();
                Dictionary<string, double> PaddockSWStrengths = new Dictionary<string,double>();
                List<RootZone> RootZones = new List<RootZone>(); //root zones in the current field/zone
                foreach (ICrop crop in paddock.Plants) //go through all crops and extract RootZones and crop SW demands in current field/zone ... why are there no plants?
                {
                    PaddockSWDemands.Add(crop.CropType, crop.RootSystem.SWDemand);
                    PaddockSWStrengths.Add(crop.CropType, crop.RootSystem.SWStrength[ZoneName]);
                    foreach (RootZone rz in crop.RootSystem.RootZones)
                        if (rz.Zone.Name == zone.Name)
                            RootZones.Add(rz);
                }

//                Model p = (Model)paddock.Find(ZoneName);
//                Model fieldProps = (Model)p.Find("FieldProps");
                double fieldArea = zone.Area;
//                if (fieldProps == null)
//                    throw new Exception("Could not find FieldProps component in field " + ZoneName);

                Soil Soil = RootZones[0].Soil; // since all RootZones are in the same field/zone they will all have the same soil.
                double[] SWDep = Utility.Math.Multiply(Soil.SW, Soil.Thickness);
                double[] CropSWDemand = new double[RootZones.Count()];
                for (int i = 0; i < RootZones.Count(); i++) //get demand for all crops in paddock using relative SW strength
                {
                     //2 swdemand, 3 strength
                    //sw demand for the crop = total demand for sw by all crops in this zone * relative strength for this crop in this zone
                    CropSWDemand[i] = PaddockSWDemands[zone.Name] * 1;//(double)RootZones[i].;
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
                        RootZone Zone = (RootZone)RootZones[i];
                        for (int j = 0; j < NumLayers; j++)
                        {
                            if (Utility.Math.Sum(CropSWDemand) < Utility.Math.Sum(SWSupply))
                            {
                                LayerUptake[i, j] = CropSWDemand[i] * RelSWLayerStrength[i, j];
                            }
                            else
                                LayerUptake[i, j] = SWSupply[i, j] * RelKLStrength[j, i] * RootProportion(j, Zone.RootDepth, Zone.Soil.Thickness);

                            if (LayerUptake[i, j] < 0)
                                throw new Exception("Layer uptake should not be negative");
                        }
                    }

                    DenseMatrix Uptake = DenseMatrix.OfArray(LayerUptake);

                    for (int i = 0; i < RootZones.Count(); i++) //subtract taken water from the supply and demand
                    {
                        CropSWDemand[i] -= Uptake.Row(i).Sum();

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

                   Soil.SoilWater.Set("sw_dep", SWDep);
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
        private double[,] CalcCropSWLayerUptake(double[] CropSWDemand, double[,] RelSWLayerStrength, int NumLayers, List<RootZone> RootZones, double[] dlayer)
        {
            double[,] CropSWLayerUptake = new double[CropSWDemand.Length, NumLayers];
            for (int i = 0; i < CropSWDemand.Length; i++)
            {
                RootZone Zone = (RootZone)RootZones[i];
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
        private double[,] CalcSWSupply(List<RootZone> RootZones, double[] SWDep, int NumLayers)
        {
            RootZone RootZone = new RootZone();
            double[,] SWSupply = new double[RootZones.Count(), NumLayers];
            for (int i = 0; i < RootZones.Count(); i++) //crops
            {
                RootZone = (RootZone)RootZones[i];
                for (int j = 0; j < NumLayers; j++)
                {
                    SWSupply[i, j] = RootZone.Soil.KL("crop")[j] * (SWDep[j] - RootZone.Soil.LL("crop")[j] * RootZone.Soil.Thickness[j]) * RootZone.Zone.Area;
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
        private double[,] CalcRelSWLayerStrength(List<RootZone> RootZones, double[] SWDep, int NumLayers)
        {
            RootZone RootZone = new RootZone();
            double[,] RelSWLayerStrength = new double[RootZones.Count(), NumLayers];
            for (int i = 0; i < RootZones.Count(); i++) //crops
            {
                double TotalSource = 0;
                RootZone = (RootZone)RootZones[i];
                int DeepestRoot = CalcMaxRootLayer(RootZone.RootDepth, RootZone.Soil.Thickness);
                for (int j = 0; j < NumLayers; j++)
                    if (j <= DeepestRoot)
                        TotalSource += RootZone.Soil.KL("crop")[j] * SWDep[j];
                for (int j = 0; j < NumLayers; j++)
                    if (j <= DeepestRoot && RootZone.Soil.KL("crop")[j] > 0)
                        RelSWLayerStrength[i, j] = RootZone.Soil.KL("crop")[j] * SWDep[j] / TotalSource;
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
        private double[,] CalcRelKLStrength(List<RootZone> RootZones, double[] CropSWDemand)
        {
            double[][] KLArray = new double[RootZones.Count()][];
            int[] LowestRootLayer = new int[RootZones.Count()];
            for (int i = 0; i < KLArray.GetLength(0); i++) //extract the kl array from each zone
            {
                RootZone RootZone = (RootZone)RootZones[i];
                KLArray[i] = RootZone.Soil.KL("crop");
                for (int j = 0; j < KLArray[i].Length; j++)
                    if (CropSWDemand[i] == 0)
                        KLArray[i][j] = 0;
                LowestRootLayer[i] = CalcMaxRootLayer(RootZone.RootDepth, RootZone.Soil.Thickness);
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
            string[] ZoneNames = new string[RootData.RootZones.Count];
            double[] SWDeps = new double[RootData.RootZones.Count];
            double TotalSW;
            Model p;
            Soil Soil;

            for (int i = 0; i < ZoneNames.Length; i++)
            {
                double[] SWlayers;
                ZoneNames[i] = RootData.RootZones[i].Zone.Name;
                p = (Model)paddock.Find(ZoneNames[i]);
                Soil = (Soil)p.Find(typeof(Soil));
                SWlayers = (double[])Soil.SoilWater.Get("sw_dep");
                SWDeps[i] = (double)Utility.Math.Sum(SWlayers);
            }

            TotalSW = (double)Utility.Math.Sum(SWDeps);
            for (int i = 0; i < ZoneNames.Length; i++)
                SoilWaters.Add(ZoneNames[i], SWDeps[i] / TotalSW * RootData.RootZones[i].Zone.Area);

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