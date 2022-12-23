using Models.DCAPST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.DCaPST.Parameters
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
