namespace Models.DCAPST
{
    public static class WheatCropParameterGenerator
    {
        /// <summary>
        /// The name of this Crop.
        /// </summary>
        public const string CROP_NAME = "wheat";

        public static DCaPSTParameters Generate()
        {
            return new DCaPSTParameters();
        }
    }
}
