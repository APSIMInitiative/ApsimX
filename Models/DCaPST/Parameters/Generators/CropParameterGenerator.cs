using System;
using System.Collections.Generic;

namespace Models.DCAPST
{
    // Typedef this collection for convenience.
    using CropParameterMapper = Dictionary<string, Func<DCaPSTParameters>>;

    /// <summary>
    /// A class that can be used to generate crop parameters.
    /// </summary>
    public class CropParameterGenerator : ICropParameterGenerator
    {
        /// <summary>
        /// A mapping of the known crop name, to DCaPST parameter generators.
        /// </summary>
        private readonly CropParameterMapper cropParameterMapper = CreateCropParameterMapper();
        
        /// <inheritdoc/>
        public DCaPSTParameters Generate(
            string cropName, 
            double rubiscoLimitedModifier, 
            double electronTransportLimitedModifier
        )
        {
            if (string.IsNullOrEmpty(cropName)) return null;

            DCaPSTParameters dcapstParameters = null;

            if (cropParameterMapper.TryGetValue(cropName.ToUpper(), out var generatorFunc))
            {
                dcapstParameters = generatorFunc();
                ApplyLimitedModifiers(dcapstParameters, rubiscoLimitedModifier, electronTransportLimitedModifier);
            }

            return dcapstParameters;
        }

        /// <summary>
        /// Creates a collection containing a mapping between the crop name and a DCaPST parameter 
        /// generator specific to that crop.
        /// </summary>
        /// <returns>Crop name to parameter generator mapper.</returns>
        private static CropParameterMapper CreateCropParameterMapper()
        {
            return new CropParameterMapper()
            {
                {
                    // Sorghum
                    SorghumCropParameterGenerator.CROP_NAME.ToUpper(),
                    SorghumCropParameterGenerator.Generate
                },
                {
                    // Wheat
                    WheatCropParameterGenerator.CROP_NAME.ToUpper(),
                    WheatCropParameterGenerator.Generate
                }
            };
        }

        /// <summary>
        /// Allows us to lift the AC/AJ curve.
        /// </summary>
        /// <param name="dcapstParameters"></param>
        /// <param name="rubiscoLimitedModifier"></param>
        /// <param name="electronTransportLimitedModifier"></param>
        private static void ApplyLimitedModifiers(
            DCaPSTParameters dcapstParameters,
            double rubiscoLimitedModifier,
            double electronTransportLimitedModifier
        )
        {
            if (dcapstParameters == null || dcapstParameters.Pathway == null) return;

            dcapstParameters.Pathway.MaxRubiscoActivitySLNRatio *= rubiscoLimitedModifier;
            dcapstParameters.Pathway.MaxPEPcActivitySLNRatio *= rubiscoLimitedModifier;
            dcapstParameters.Pathway.MesophyllCO2ConductanceSLNRatio *= rubiscoLimitedModifier;
            dcapstParameters.Pathway.MaxElectronTransportSLNRatio *= electronTransportLimitedModifier;

            // Epsilon is a key variable and its changed through interactions with the
            // SpectralCorrectionFactor calculated as follows:
            dcapstParameters.Pathway.SpectralCorrectionFactor = 
                1 + 
                (electronTransportLimitedModifier * dcapstParameters.Pathway.SpectralCorrectionFactor) - 
                electronTransportLimitedModifier;
        }
    }
}
