using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models;

namespace APSIM.Documentation.Models.Types;

/// <summary>
/// Documentation for the <see cref="Memo"/> class.
/// </summary>
public class DocMemo : DocGeneric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocMemo" /> class.
    /// </summary>
    public DocMemo(IModel model) : base(model) { }

    /// <summary>
    /// Document the model.
    /// </summary>
    public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
    {
        if (tags == null)
            tags = new List<ITag>();

        string memoText = (model as Memo).Text;

        tags.Add(new Paragraph(memoText));
        return tags;
    }
}
