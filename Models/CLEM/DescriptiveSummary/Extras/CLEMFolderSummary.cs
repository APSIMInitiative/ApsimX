using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for CLEM Folder component
/// </summary>
public class CLEMFolderSummary : DescriptiveSummaryProviderBase<CLEMFolder>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    ///// <inheritdoc/>
    //public override void CreateSummaryOpeningBlocks()
    //{
    //    Generator.OpenBlock("resource", styleString: $"opacity: {SummaryOpacity()};", id: $"{ModelTyped.Name}_main");
    //}

    ///// <inheritdoc/>
    //public override void CreateSummaryClosingBlocks()
    //{
    //    generator.CloseMostRecentBlock(id: $"{ModelTyped.Name}_main");
    //}

}
