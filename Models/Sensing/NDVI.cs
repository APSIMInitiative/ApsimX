using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Models.Core;
using Models.Interfaces;
using Models.WaterModel;

namespace Models.Sensing
{
    /// <summary>
    /// This model takes infromation from residues, soil and crop to give an estimate of NDVI
    /// </summary>
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ScopedModel]
    public class NDVI : Model
    {
        /// <summary> link to the canopy model</summary>
        /// <summary>Models in the simulation that implement ICanopy.</summary>
        private List<ICanopy> canopyModels = null;

        /// <summary> link to the evaporation model</summary>
        [Link]
        private EvaporationModel evaporationModel = null;

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
        public double Value { get; set; }

        [EventSubscribe("StartOfSimulation")]
        private void DoStartOfSimulation(object sender, EventArgs e)
        {
            Zone zone = this.Parent as Zone;
            canopyModels = zone.FindAllDescendants<ICanopy>().ToList();
        }

        [EventSubscribe("DoManagement")]
        private void DoDailyCalculations(object sender, EventArgs e)
        {
            double SoilNDVI = WetSoilNDVI;
            if (evaporationModel.t > 1)
                SoilNDVI = DrySoilNDVI;

            if (canopyModels.Count > 1)
                throw new Exception("NDVI not currently programmed to work with more than one canopy");
            ICanopy canopy = canopyModels[0];
                double CropNDVI = 0;
            if (canopy.CoverTotal > 0)
                CropNDVI = (canopy.CoverGreen / canopy.CoverTotal) * GreenCropNDVI + (1.0 - canopy.CoverGreen / canopy.CoverTotal) * DeadCropNDVI;
            Value = SoilNDVI + (CropNDVI - SoilNDVI) * Math.Pow(canopy.CoverTotal, (1.0 - SoilNDVI));
        }


    }
}
