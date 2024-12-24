using System;
using System.Collections.Generic;

namespace Models.DCAPST
{
    using CropParameterMapper = Dictionary<string, Func<DCaPSTParameters>>;

    /// <summary>
    /// A class that generates crop parameters and applies limited modifiers.
    /// </summary>
    public class CropParameterGenerator : ICropParameterGenerator
    {
        private readonly CropParameterMapper cropParameterMapper = CreateCropParameterMapper();

        /// <inheritdoc/>
        public DCaPSTParameters Generate(string cropName)
        {
            if (!string.IsNullOrEmpty(cropName) && 
                cropParameterMapper.TryGetValue(cropName.ToUpper(), out var generatorFunc)
            )
            {
                return generatorFunc();
            }

            throw new Exception($"Cannot generate DCaPST Parameters as crop name specified is invalid: {cropName ?? "''"}");
        }

        /// <inheritdoc/>
        public void ApplyRubiscoLimitedModifier(
            string cropName,
            ref DCaPSTParameters dcapstParameters,
            double rubiscoLimitedModifier
        )
        {
            if (!IsValidCrop(cropName)) return;

            var defaultParameters = Generate(cropName);

            // Assign the Pathway property to a temporary variable
            var pathway = dcapstParameters.Pathway;

            // Modify the fields of the struct
            pathway.MaxRubiscoActivitySLNRatio =
                defaultParameters.Pathway.MaxRubiscoActivitySLNRatio * rubiscoLimitedModifier;

            pathway.MaxPEPcActivitySLNRatio =
                defaultParameters.Pathway.MaxPEPcActivitySLNRatio * rubiscoLimitedModifier;

            pathway.MesophyllCO2ConductanceSLNRatio =
                defaultParameters.Pathway.MesophyllCO2ConductanceSLNRatio * rubiscoLimitedModifier;

            dcapstParameters.Pathway = pathway;
        }

        /// <inheritdoc/>
        public void ApplyElectronTransportLimitedModifier(
            string cropName,
            ref DCaPSTParameters dcapstParameters,
            double electronTransportLimitedModifier
        )
        {
            if (!IsValidCrop(cropName)) return;

            var defaultParameters = Generate(cropName);

            var pathway = dcapstParameters.Pathway;

            pathway.MaxElectronTransportSLNRatio =
                defaultParameters.Pathway.MaxElectronTransportSLNRatio * electronTransportLimitedModifier;

            pathway.SpectralCorrectionFactor =
                1 +
                (electronTransportLimitedModifier * defaultParameters.Pathway.SpectralCorrectionFactor) -
                electronTransportLimitedModifier;

            dcapstParameters.Pathway = pathway;
        }

        private static bool IsValidCrop(string cropName)
        {
            return !string.IsNullOrEmpty(cropName);
        }

        private static CropParameterMapper CreateCropParameterMapper()
        {
            return new CropParameterMapper
            {
                { SorghumCropParameterGenerator.CROP_NAME.ToUpper(), SorghumCropParameterGenerator.Generate },
                { WheatCropParameterGenerator.CROP_NAME.ToUpper(), WheatCropParameterGenerator.Generate }
            };
        }
    }
}
