using Models.Core;
using Models.PMF;

namespace Models.DCAPST
{
    /// <summary>
    /// A helper class for extracting the Cultivar, if present, for the DCaPST model.
    /// </summary>
    public static class SowingParametersParser
    {
        /// <summary>
        /// The name of the folder that is used to store the cultivar parameters.
        /// </summary>
        public const string CULTIVAR_PARAMETERS_FOLDER_NAME = "CultivarParameters";

        /// <summary>
        /// Given the model and the sowing parameters, extract any configured cultivars.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="sowingParameters"></param>
        /// <returns>The configured cultivar, or null.</returns>
        public static Cultivar GetCultivarFromSowingParameters(DCaPSTModelNG model, SowingParameters sowingParameters)
        {
            if (model is null) return null;
            if (sowingParameters is null) return null;
            if (sowingParameters.Plant is null) return null;
            if (string.IsNullOrEmpty(sowingParameters.Cultivar)) return null;

            var cultivar = model.Structure.FindChild<Cultivar>(CULTIVAR_PARAMETERS_FOLDER_NAME)
                           ?? model.Structure.FindChild<Cultivar>(sowingParameters.Plant.Name)
                           ?? model.Structure.FindChild<Cultivar>(sowingParameters.Cultivar);

            return cultivar;
        }
    }
}
