namespace Models.Soils.Arbitrator
{
    using System;
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

        /// <summary>Initializes a new instance of the <see cref="Estimate"/> class.</summary>
        /// <param name="parent">The parent.</param>
        public Estimate(IModel parent)
        {
            Values = new List<CropUptakes>();
            Parent = parent;
        }

        /// <summary>Initializes a new instance of the <see cref="Estimate"/> class.</summary>
        /// <param name="parent">The parent model</param>
        /// <param name="Type">The type of estimate</param>
        /// <param name="soilstate">The state of the soil</param>
        /// <param name="uptakeModels">A list of models that do uptake.</param>
        public Estimate(IModel parent, CalcType Type, SoilState soilstate, List<IModel> uptakeModels)
        {
            Values = new List<CropUptakes>();

            Parent = parent;
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

        /// <summary>Implements the operator *.</summary>
        /// <param name="E">The estimate</param>
        /// <param name="value">The value to multiply the estimate by.</param>
        /// <returns>The resulting estimate</returns>
        public static Estimate operator *(Estimate E, double value)
        {
            Estimate NewE = new Estimate(E.Parent);
            foreach (CropUptakes U in E.Values)
            {
                CropUptakes NewU = new CropUptakes();
                NewE.Values.Add(NewU);
                foreach (ZoneWaterAndN Z in U.Zones)
                {
                    ZoneWaterAndN NewZ = Z * value;
                    NewU.Zones.Add(NewZ);
                }
            }

            return NewE;
        }
    }
}
