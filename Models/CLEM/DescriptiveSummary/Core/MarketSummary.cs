using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for CLEM Market component
/// </summary>
public class MarketSummary : DescriptiveSummaryProviderBase<Market>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        // nothing to report
    }
}
