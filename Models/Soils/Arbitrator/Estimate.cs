namespace Models.Soils.Arbitrator
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Interfaces;

    /// <summary>
    /// Contains an estimate of uptakes (either water or nitrogen)
    /// </summary>
    public class Estimate
    {
        /// <summary>The parent model.</summary>
        private IModel Parent;

        /// <summary>List of models that perform uptake of water and N.</summary>
        private IEnumerable<IModel> uptakeModels;

        /// <summary>
        /// An enumeration describing whether the estimate is for water or nitrogen.
        /// </summary>
        public enum CalcType 
        {
            /// <summary>Indicates this estimate is for water.</summary>
            Water,

            /// <summary>Indicates this estimate is for nitrogen.</summary>
            Nitrogen 
        };

        /// <summary>Constructor.</summary>
        /// <param name="parent">The parent.</param>
        /// <param name="modelsThatPerformUptake">Models that peform water and N uptake.</param>
        public Estimate(IModel parent, IEnumerable<IModel> modelsThatPerformUptake)
        {
            Values = new List<CropUptakes>();
            Parent = parent;
            uptakeModels = modelsThatPerformUptake;
        }

        /// <summary>Initializes a new instance of the <see cref="Estimate"/> class.</summary>
        /// <param name="Type">The type of estimate</param>
        /// <param name="soilstate">The state of the soil</param>
        public void PerformEstimate(CalcType Type, SoilState soilstate)
        {
            Values.Clear();

            foreach (IUptake crop in uptakeModels)
            {
                List<ZoneWaterAndN> uptake;
                if (Type == CalcType.Water)
                    uptake = crop.GetWaterUptakeEstimates(soilstate);
                else
                    uptake = crop.GetNitrogenUptakeEstimates(soilstate);

                if (uptake != null)
                {
                    CropUptakes Uptake = new CropUptakes();
                    Uptake.Crop = crop;
                    Uptake.Zones = uptake;
                    Values.Add(Uptake);
                }
            }
        }

        /// <summary>Gets the estimate values.</summary>
        public List<CropUptakes> Values { get; private set; }

        /// <summary>Gets uptakes for the specified crop and zone. Will throw if not found.</summary>
        /// <param name="crop">Name of the crop.</param>
        /// <param name="ZoneName">Name of the zone.</param>
        /// <returns>The uptakes.</returns>
        public ZoneWaterAndN UptakeZone(IUptake crop, string ZoneName)
        {
            foreach (CropUptakes U in Values)
                if (U.Crop == crop)
                    foreach (ZoneWaterAndN Z in U.Zones)
                        if (Z.Zone.Name == ZoneName)
                            return Z;

            throw (new Exception("Cannot find uptake for" + (crop as IModel).Name + " " + ZoneName));
        }

        /// <summary>Gets water uptakes for all crops for the specified zone.</summary>
        /// <param name="zoneName">Name of the zone.</param>
        public double[] CalculateWaterUptakeFromZone(string zoneName)
        {
            double[] returnValues = null;
            foreach (CropUptakes U in Values)
                foreach (ZoneWaterAndN Z in U.Zones)
                    if (Z.Zone.Name == zoneName)
                    {
                        if (returnValues == null)
                        {
                            returnValues = new double[Z.Water.Length];
                            Array.Copy(Z.Water, returnValues, Z.Water.Length);
                        }
                        else
                        {
                            for (int i = 0; i < Z.Water.Length; i++)
                                returnValues[i] += Z.Water[i];
                        }
                    }

            return returnValues;
        }

        /// <summary>Gets NO3 uptakes for all crops for the specified zone.</summary>
        /// <param name="zoneName">Name of the zone.</param>
        public double[] CalculateNO3UptakeFromZone(string zoneName)
        {
            double[] returnValues = null;
            foreach (CropUptakes U in Values)
                foreach (ZoneWaterAndN Z in U.Zones)
                    if (Z.Zone.Name == zoneName)
                    {
                        if (returnValues == null)
                        {
                            returnValues = new double[Z.NO3N.Length];
                            Array.Copy(Z.NO3N, returnValues, Z.NO3N.Length);
                        }
                        else
                        {
                            for (int i = 0; i < Z.NO3N.Length; i++)
                                returnValues[i] += Z.NO3N[i];
                        }
                    }

            return returnValues;
        }

        /// <summary>Gets NH4 uptakes for all crops for the specified zone.</summary>
        /// <param name="zoneName">Name of the zone.</param>
        public double[] CalculateNH4UptakeFromZone(string zoneName)
        {
            double[] returnValues = null;
            foreach (CropUptakes U in Values)
                foreach (ZoneWaterAndN Z in U.Zones)
                    if (Z.Zone.Name == zoneName)
                    {
                        if (returnValues == null)
                        {
                            returnValues = new double[Z.NH4N.Length];
                            Array.Copy(Z.NH4N, returnValues, Z.NH4N.Length);
                        }
                        else
                        {
                            for (int i = 0; i < Z.NH4N.Length; i++)
                                returnValues[i] += Z.NH4N[i];
                        }
                    }

            return returnValues;
        }
    }
}
