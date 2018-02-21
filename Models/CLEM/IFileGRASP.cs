using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// GRASP reader interface
    /// </summary>
    public interface IFileGRASP
    {
        /// <summary>
        /// Queries the the GRASP SQLite database using the specified parameters.
        /// nb. Ignore ForageNo , it is a legacy column in the GRASP file that is not used anymore.
        /// </summary>
        /// <param name="Region"></param>
        /// <param name="Soil"></param>
        /// <param name="GrassBA"></param>
        /// <param name="LandCon"></param>
        /// <param name="StkRate"></param>
        /// <param name="EcolCalculationDate"></param>
        /// <param name="EcolCalculationInterval"></param>
        /// <returns></returns>
        List<PastureDataType> GetIntervalsPastureData(int Region, int Soil, int GrassBA, int LandCon, int StkRate,
                                         DateTime EcolCalculationDate, int EcolCalculationInterval);

    }
}
