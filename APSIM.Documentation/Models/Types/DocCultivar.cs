using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using DocumentFormat.OpenXml.Office2010.CustomUI;
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
    public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
    {
        if (tags == null)
            tags = new List<ITag>();

        List<ITag> subTags = new();

        if ((model as Cultivar).GetNames().Any())
        {
            subTags.Add(
                new Section(
                    "Aliases",
                    new Paragraph(string.Join(',', (model as Cultivar).GetNames())))
            );
        }

        string parameterOverwritesText = "";
        foreach (string command in (model as Cultivar).Command)
        {
            parameterOverwritesText += "- " + command + Environment.NewLine;
        };

        Paragraph parameterOverwrites = new(parameterOverwritesText);

        subTags.Add(parameterOverwrites);
        tags.Add(new Section(model.Name, subTags));
        return tags;
    }
}
