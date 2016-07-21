using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LabourItem
    {
        /// <summary>
        /// Age in years.
        /// </summary>
        public double Age;

        /// <summary>
        /// Male or Female
        /// </summary>
        public string Gender;

        /// <summary>
        /// Available Labour (in days) in the current month. 
        /// </summary>
        public double AvailableDays;

        /// <summary>
        /// Store this Maximum Labour Supply (in days) for each month.
        /// so that I don't need to keep reading it in each month.
        /// </summary>
        internal double[] MaxLabourSupply;

    }




}

