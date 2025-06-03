using System;

namespace Models.CLEM
{
    /// <summary>
    /// A structure containing the commonly used weather data.
    /// </summary>
    [Serializable]
    public struct PastureDataType
    {
        /// <summary>
        /// Climatic Region Number
        /// </summary>
        public int Region;

        /// <summary>
        /// Soil Number
        /// </summary>
        public int Soil;

        /// <summary>
        /// Forage Number
        /// nb. This column is to be ignored.
        /// </summary>
        public int ForageNo;

        /// <summary>
        /// Grass Basal Area
        /// </summary>
        public int GrassBA;

        /// <summary>
        /// Land Condition
        /// </summary>
        public int LandCon;

        /// <summary>
        /// Stocking Rate
        /// </summary>
        public int StkRate;

        /// <summary>
        /// Year Number (counting from start of simulation ?)
        /// </summary>
        public int YearNum;

        /// <summary>
        /// Year (eg. 2017)
        /// </summary>
        public int Year;

        /// <summary>
        /// Cut Number in this year
        /// </summary>
        public int CutNum;

        /// <summary>
        /// Month (eg. 1 is Jan, 2 is Feb)
        /// </summary>
        public int Month;

        /// <summary>
        /// Amout in Kg of Biomass of the pasture
        /// </summary>
        public double Growth;

        /// <summary>
        /// Amount in Kg of By Product 1 of the production of this pasture
        /// </summary>
        public double BP1;

        /// <summary>
        /// Amount in Kg of By Product 2 of the production of this pasture
        /// </summary>
        public double BP2;

        /// <summary>
        /// Utilisation
        /// </summary>
        public double Utilisn;

        /// <summary>
        /// Soil Loss
        /// </summary>
        public double SoilLoss;

        /// <summary>
        /// Cover
        /// </summary>
        public double Cover;

        /// <summary>
        /// Tree Basal Area
        /// </summary>
        public double TreeBA;

        /// <summary>
        /// Rainfall
        /// </summary>
        public double Rainfall;

        /// <summary>
        /// Runoff
        /// </summary>
        public double Runoff;

        /// <summary>
        /// Combine Year and Month to create a DateTime.
        /// Day is set to the 1st of the month.
        /// </summary>
        public DateTime CutDate;

    }
}