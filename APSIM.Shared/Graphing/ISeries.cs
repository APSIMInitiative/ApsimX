using System;
using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Contains options common to all graph series.
    /// </summary>
    /// <remarks>
    /// Double vs datetime
    /// line vs area vs bar vs box/whisker
    /// </remarks>
    public interface ISeries
    {
        /// <summary>Name of the series.</summary>
        string Title { get; }

        /// <summary>Colour of the series.</summary>
        Color Colour { get; }

        /// <summary>Should this series appear in the legend?</summary>
        bool ShowOnLegend { get; }

        /// <summary>X-axis data.</summary>
        IEnumerable<object> X { get; }

        /// <summary>Y-axis data.</summary>
        IEnumerable<object> Y { get; }

        /// <summary>Name of the x-axis field displayed by this series.</summary>
        string XFieldName { get; }

        /// <summary>Name of the y-axis field displayed by this series.</summary>
        string YFieldName { get; }
    }
}
