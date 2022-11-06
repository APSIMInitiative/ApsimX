using System;
using System.Collections.Generic;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Pasture datacube reader interface
    /// </summary>
    public interface IFilePasture
    {
        /// <summary>
        /// Queries the the SQLite pasture database using the specified parameters.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="soil"></param>
        /// <param name="grassBasalArea"></param>
        /// <param name="landCondition"></param>
        /// <param name="stockingRate"></param>
        /// <param name="ecolCalculationDate"></param>
        /// <param name="ecolCalculationInterval"></param>
        /// <returns>List of pasture data types</returns>
        List<PastureDataType> GetIntervalsPastureData(int region, string soil, double grassBasalArea, double landCondition, double stockingRate,
                                         DateTime ecolCalculationDate, int ecolCalculationInterval);

        /// <summary>
        /// Check that records exist in database
        /// </summary>
        /// <param name="table"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        int RecordsFound(string table, object value);
    }
}
