using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;

namespace Models.Soils.Arbitrator
{

    /// <summary>
    /// Contains an estimate of uptakes (either water or nitrogen)
    /// </summary>
    public class Estimate
    {
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
        /// <param name="parent">The parent model</param>
        /// <param name="Type">The type of estimate</param>
        /// <param name="soilstate">The state of the soil</param>
        /// <param name="uptakeModels">A list of models that do uptake.</param>
        public Estimate(IModel parent, CalcType Type, SoilState soilstate, IEnumerable<IUptake> uptakeModels)
        {
            Values = new List<CropUptakes>();

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
        public ZoneWaterAndN GetUptakeForCropAndZone(IUptake crop, string ZoneName)
        {
            foreach (CropUptakes U in Values)
                if (U.Crop == crop)
                    foreach (ZoneWaterAndN Z in U.Zones)
                        if (Z.Zone.Name == ZoneName)
                            return Z;

            throw (new Exception("Cannot find uptake for" + (crop as IModel).Name + " " + ZoneName));
        }
    }
}
