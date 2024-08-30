using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models;
using System.Linq;

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
        List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

        string memoText = (model as Memo).Text;

        newTags.Add(new Paragraph(memoText));
        return newTags;
    }
}
