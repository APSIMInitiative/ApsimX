using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using APSIM.Numerics;
using Mapsui.Extensions;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.WaterModel;
using APSIM.Core;

namespace Models.Sensor
{
    /// <summary>
    /// This model takes infromation from residues, soil and crop to give an estimate of NDVI
    /// </summary>
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    public class Spectral : Model, IScopedModel, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        /// <summary> link to the canopy model</summary>
        /// <summary>Models in the simulation that implement ICanopy.</summary>
        private List<ICanopy> canopyModels = null;

        /// <summary> The NDVI of dry soil, should be less than wet soil</summary>
        [Description("Dry soil NDVI")]
        public double DrySoilNDVI { get; set; }

        /// <summary> The NDVI of wet soil, should be more than dry soil</summary>
        [Description("Wet soil NDVI")]
        public double WetSoilNDVI { get; set; }

        /// <summary> The NDVI of the crop canopy when it is green</summary>
        [Description("Green Crop NDVI")]
        public double GreenCropNDVI { get; set; }

        /// <summary> The NDVI of the crop canopy when it is full senesced</summary>
        [Description("Dead Crop NDVI")]
        public double DeadCropNDVI { get; set; }

        /// <summary> The NDVI of the zone</summary>
        [JsonIgnore]
        public double NDVI { get; set; }

        [EventSubscribe("StartOfSimulation")]
        private void DoStartOfSimulation(object sender, EventArgs e)
        {
            Zone zone = this.Parent as Zone;
            canopyModels = Structure.FindChildren<ICanopy>(relativeTo: zone, recurse: true).ToList();

            SurfaceDUlmm = soilPhysical.DUL[0] * SurfaceLayerDepth;
            SurfaceAirDrymm = soilPhysical.AirDry[0] * SurfaceLayerDepth;

            SurfaceSWmm = (water.InitialValuesMM[0] / soilPhysical.DULmm[0]) * SurfaceDUlmm;

            irrigations = new List<IrrigationApplicationType>();
        }

        [EventSubscribe("DoManagementCalculations")]
        private void DoDailyCalculations(object sender, EventArgs e)
        {
            double SoilNDVI = DrySoilNDVI + (WetSoilNDVI - DrySoilNDVI) * SurfaceRWC;

            if (canopyModels.Count > 1)
                throw new Exception("NDVI not currently programmed to work with more than one canopy");
            ICanopy canopy = canopyModels[0];
            double CropNDVI = 0;
            if (canopy.CoverTotal > 0)
                CropNDVI = (canopy.CoverGreen / canopy.CoverTotal) * GreenCropNDVI + (1.0 - canopy.CoverGreen / canopy.CoverTotal) * DeadCropNDVI;
            NDVI = SoilNDVI + (CropNDVI - SoilNDVI) * Math.Pow(canopy.CoverTotal, (1.0 - SoilNDVI));
        }

        [Units("mm")]
        private const double SurfaceLayerDepth = 20;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        Water water = null;

        [Link]
        ISoilWater waterBalance = null;

        [NonSerialized]
        private List<IrrigationApplicationType> irrigations;

        private double SurfaceDUlmm { get; set; }
        private double SurfaceAirDrymm { get; set; }
        private double SurfaceSWmm { get; set; }
        private double SurfaceRWC
        {
            get
            {
                return (SurfaceSWmm-SurfaceAirDrymm) / (SurfaceDUlmm-SurfaceAirDrymm);
            }
        }
        private double topRWC
        {
            get
            {
                double rwc = (waterBalance.SW[0] - soilPhysical.AirDry[0]) / (soilPhysical.DUL[0] - soilPhysical.AirDry[0]);
                return MathUtilities.Bound(rwc, 0, 1);
            }
        }

        [EventSubscribe("DoUpdate")]
        private void afterMainSoilWater(object sender, EventArgs e)
        {
            double irrigation = 0;
            foreach (var irrig in irrigations)
                irrigation += irrig.Amount;
            double infiltration = waterBalance.PotentialInfiltration + irrigation;
            double evaporation = waterBalance.Eos;
            //First add irrigation and remove evaporation from soil surface.  Leaf transpiration out as assum few roots in the top few cm of soil
            SurfaceSWmm = MathUtilities.Bound(SurfaceSWmm + infiltration - evaporation, SurfaceAirDrymm, SurfaceDUlmm);

            //Next calculate diffusion from the top soil layer specified in the soil water model to the surface layer used to influence NDVI calculatons
            double rconst = 3.0;
            double gradient = topRWC - SurfaceRWC;  //Gradient is the difference in relative water content of the surface 20 mm and the top layer
            double rate = (Math.Exp(topRWC * rconst) - 1) / (Math.Exp(rconst) - 1); //Rate of diffusion is calculated as a normalised exponential that give a rate of 1 when top soil water content is at DUL and declines exponentially as the top soil dries
            double diffusionflux = gradient * rate * (SurfaceDUlmm - SurfaceAirDrymm); //Diffusion is calculated from the size of the gradient between the surface and the top layer, the relative diffusivity at the top layer water content and the amount of water the surface layer can hold
            diffusionflux = Math.Max(diffusionflux, SurfaceSWmm - SurfaceAirDrymm);
            SurfaceSWmm = MathUtilities.Bound(SurfaceSWmm + diffusionflux, SurfaceAirDrymm, SurfaceDUlmm);
        }

        /// <summary>Called on start of day.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            irrigations.Clear();
        }

        /// <summary>Called when an irrigation occurs.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, IrrigationApplicationType e)
        {
            irrigations.Add(e);
        }
    }
}
