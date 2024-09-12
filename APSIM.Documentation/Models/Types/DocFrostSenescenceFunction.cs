using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocFrostSenescenceFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocFrostSenescenceFunction" /> class.
        /// </summary>
        public DocFrostSenescenceFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);
            
            List<ITag> subTags = new List<ITag>();
            FrostSenescenceFunction frost = model as FrostSenescenceFunction;
            IFunction frostKill = model.FindChild<IFunction>("frostKill");
            IFunction frostKillSevere = model.FindChild<IFunction>("frostKillSevere");

            subTags.Add(new Paragraph($"FrostKill: {frostKill.Value()} °C"));
            subTags.Add(new Paragraph($"FrostKillSevere: {frostKillSevere.Value()} °C"));

            subTags.Add(new Paragraph($"If minimum temperature falls below FrostKillSevere then all LAI is removed causing plant death"));
            subTags.Add(new Paragraph($"If the minimum temperature is above FrostKillSevere, but below FrostKill, then the effect on the plant will depend on which phenologiacl stage the plant is in:"));
            subTags.Add(new Paragraph($"  Before Floral Initiation: Nearly all of the LAI will be removed, but if not under any other stress, the plant can survive."));
            subTags.Add(new Paragraph($"  Before Flowering: All of the LAI will be removed, casuing plant death."));
            subTags.Add(new Paragraph($"  After Flowering: The leaf is not damaged."));

            (tags[0] as Section).Children.AddRange(subTags);

            return tags;
        }
    }
}
