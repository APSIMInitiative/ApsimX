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
            DCaPSTParameters dcapstParameters,
            double rubiscoLimitedModifier
        )
        {
            if (!IsValidCropParameters(cropName, dcapstParameters)) return;

            var defaultParameters = Generate(cropName);

            dcapstParameters.Pathway.MaxRubiscoActivitySLNRatio =
                defaultParameters.Pathway.MaxRubiscoActivitySLNRatio * rubiscoLimitedModifier;

            dcapstParameters.Pathway.MaxPEPcActivitySLNRatio =
                defaultParameters.Pathway.MaxPEPcActivitySLNRatio * rubiscoLimitedModifier;

            dcapstParameters.Pathway.MesophyllCO2ConductanceSLNRatio =
                defaultParameters.Pathway.MesophyllCO2ConductanceSLNRatio * rubiscoLimitedModifier;
        }

        /// <inheritdoc/>
        public void ApplyElectronTransportLimitedModifier(
            string cropName,
            DCaPSTParameters dcapstParameters,
            double electronTransportLimitedModifier
        )
        {
            if (!IsValidCropParameters(cropName, dcapstParameters)) return;

            var defaultParameters = Generate(cropName);

            dcapstParameters.Pathway.MaxElectronTransportSLNRatio =
                defaultParameters.Pathway.MaxElectronTransportSLNRatio * electronTransportLimitedModifier;

            dcapstParameters.Pathway.SpectralCorrectionFactor =
                1 +
                (electronTransportLimitedModifier * defaultParameters.Pathway.SpectralCorrectionFactor) -
                electronTransportLimitedModifier;
        }

        private static bool IsValidCropParameters(string cropName, DCaPSTParameters dcapstParameters)
        {
            return !string.IsNullOrEmpty(cropName) && dcapstParameters?.Pathway != null;
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
