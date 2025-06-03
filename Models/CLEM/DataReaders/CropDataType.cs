using System;

namespace Models.CLEM
{
    /// <summary>
    /// A structure containing the commonly used crop input data.
    /// </summary>
    [Serializable]
    public class CropDataType
    {
        /// <summary>
        /// Soil Number
        /// </summary>
        public string SoilNum;

        /// <summary>
        /// Name of Crop
        /// </summary>
        public string CropName;

        /// <summary>
        /// Year (eg. 2017)
        /// </summary>
        public int Year;

        /// <summary>
        /// Month (eg. 1 is Jan, 2 is Feb)
        /// </summary>
        public int Month;

        /// <summary>
        /// Day if provided
        /// </summary>
        public int Day = 1;

        /// <summary>
        /// Amount in Kg (perHa or perTree) 
        /// </summary>
        public double AmtKg;

        /// <summary>
        /// Nitrogen Percentage of the Amount
        /// </summary>
        public double Npct;

        /// <summary>
        /// Crude Protein Percentage of the Amount
        /// </summary>
        public double CPpct;

        /// <summary>
        /// Dry Matter Digestibility Percentage of the Amount
        /// </summary>
        public double DMDpct;

        /// <summary>
        /// Dry Matter Digestibility Percentage of the Amount
        /// </summary>
        public double MD;

        /// <summary>
        /// Combine Year and Month to create a DateTime. 
        /// Day is set to the 1st of the month.
        /// </summary>
        public DateTime HarvestDate;

        /// <summary>
        /// Harvest type identifier
        /// </summary>
        public string HarvestType;
    }
}
