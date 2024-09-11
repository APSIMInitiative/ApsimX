using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types;

/// <summary>
/// Documentation for the <see cref="Cultivar"/> class.
/// </summary>
public class DocCultivar : DocGeneric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocPlant" /> class.
    /// </summary>
    public DocCultivar(IModel model) : base(model) { }

    /// <summary>
    /// Document the model.
    /// </summary>
    public override List<ITag> Document(int heading = 0)
    {
        List<ITag> tags = base.Document(heading);

        // Get table of Parameter overrides.
        DataTable overridesTable = new();
        overridesTable.Columns.Add("Parameter overrides");
        foreach(string paramOverride in (model as Cultivar).Command)
            overridesTable.Rows.Add(paramOverride);
        Table paramOverridesDisplayTable = new(overridesTable);

        ITag aliasTag = (model as Cultivar).GetNames().Any() ? 
            new Section("Aliases", new Paragraph(string.Join(',', (model as Cultivar).GetNames()))) : 
            new Paragraph("");

        List<ITag> subTags = new()
        {
            aliasTag,
            paramOverridesDisplayTable
        };

        tags.Add(new Section(model.Name, subTags));
        return tags;
    }
}
