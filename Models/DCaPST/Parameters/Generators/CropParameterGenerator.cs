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
        public DCaPSTParameters Generate(string cropName)
        {
            DCaPSTParameters dcapstParameters = null;

            if (!string.IsNullOrEmpty(cropName))
            {
                if (cropParameterMapper.TryGetValue(cropName.ToUpper(), out var generatorFunc))
                {
                    dcapstParameters = generatorFunc();
                }
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
    }
}
