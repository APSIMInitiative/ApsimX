using System.Collections.Generic;

namespace Models.Interfaces
{
    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IInterpolationMethod
    {
        /// <summary>Calculate temperature at specified periods during the day.</summary>
        List<double> SubDailyValues();
        /// <summary>The type of variable for sub-daily values</summary>
        string OutputValueType { get; set; }
    }

}
