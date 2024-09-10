using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.PMF.Phen;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for phase models.
    /// Reduces need for repeated functionality for documentation by phase models.
    /// </summary>
    public class DocPhase : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPhase" /> class.
        /// </summary>
        public DocPhase(IModel model) : base(model) { }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

            // Removes the summary tag in each is phase is different.
            Section section = newTags.ElementAt(0) as Section;
            List<ITag> sectionChildren = section.Children.ToList();
            sectionChildren.RemoveAt(0);
            Section newSection = new(section.Title, sectionChildren);
            newTags.RemoveAt(0);
            newTags.Add(newSection);

            List<ITag> subTags = new List<ITag>();

            string text = GetPhaseText();
            if (text.Length > 0)
                newTags.Add(new Paragraph(text));

            return newTags;
        }

        /// <summary>
        /// Gets the text for a specific type of Phase model.
        /// </summary>
        public string GetPhaseText()
        {
            if (model is StartPhase startPhase)
            {
                return $"This phase goes from {startPhase.Start.ToLower()} to {startPhase.End.ToLower()}." +
                    $"It has no length but sets plant status to emerged once progressed.";
            }
            else if (model is PhotoperiodPhase photoperiodPhase)
            {
                return $"This phase goes from {photoperiodPhase.Start} to {photoperiodPhase.End}. " +
                    $"The phase ends when photoperiod has a reaches a critical photoperiod with a given direction (Increasing/Decreasing). " +
                    $"The base model uses a critical photoperiod of {photoperiodPhase.CricialPhotoperiod} hours ({photoperiodPhase.PPDirection}).";
            }
            else if (model is VernalisationPhase vernalisationPhase)
            {
                return $"The {model.Name} phase goes from the {vernalisationPhase.Start} stage to the {vernalisationPhase.End}" +
                $" stage and reaches {vernalisationPhase.End} when vernalisation saturation occurs.";
            }
            else if (model is NodeNumberPhase nodeNumberPhase)
            {
                return $"This phase goes from {nodeNumberPhase.Start.ToLower()} to {nodeNumberPhase.End.ToLower()} and extends from the end of the previous phase until the *CompletionNodeNumber* is achieved." +
                    $"The duration of this phase is determined by leaf appearance rate and the *CompletionNodeNumber* target";
            }
            else if (model is LeafDeathPhase leafDeafPhase)
            {
                return $"The *{leafDeafPhase.Name}* phase goes from the *{leafDeafPhase.Start}* stage to the *{leafDeafPhase.End}* stage, which occurs when all leaves have fully senesced.";
            }
            else if (model is LeafAppearancePhase leafAppearancePhase)
            {
                return $"This phase goes from {leafAppearancePhase.Start.ToLower()} to {leafAppearancePhase.End.ToLower()} and it continues until the final main-stem leaf has finished expansion." +
                    $"The duration of this phase is determined by leaf appearance rate (Structure.Phyllochron) and the number of leaves produced on the mainstem (Structure.FinalLeafNumber)";
            }
            else if (model is GrazeAndRewind grazeAndRewind)
            {
                return $"When the {grazeAndRewind.Start} phase is reached, phenology is rewound to the {grazeAndRewind.PhaseNameToGoto} phase.";
            }
            else if (model is GotoPhase gotoPhase)
            {
                return $"When the {gotoPhase.Start} phase is reached, phenology is rewound to the {gotoPhase.PhaseNameToGoto} phase.";
            }
            else if (model is GerminatingPhase germinatingPhase)
            {
                return $"The phase goes from {germinatingPhase.Start.ToLower()} to {germinatingPhase.End.ToLower()} and assumes {germinatingPhase.End.ToLower()} will be reached on the day after " +
                    $"sowing or the first day thereafter when the extractable soil water at sowing depth is greater than zero.";
            }
            // TODO: Needs work. See if Function documentation can be used here instead.
            else if (model is GenericPhase genericPhase)
            {
                var targetChild = genericPhase.FindChild("Target");
                var progressionChild = genericPhase.FindChild<VariableReference>("Progression");
                if (targetChild != null && progressionChild != null)
                {
                    string units = "";
                    if (targetChild is AddFunction)
                    {
                        List<string> targetChildrenNames = new();
                        foreach (IFunction function in targetChild.FindAllChildren<IFunction>())
                            targetChildrenNames.Add(function.Name);
                        return $"This phase goes from {genericPhase.Start.ToLower()} to {genericPhase.End.ToLower()}.\n\n" +
                            $"The *Target* for completion is calculated as the :\n\n" +
                            $"{string.Join(" + ", targetChildrenNames)}\n\n" +
                            $"*Progression* through phase is calculated daily and accumulated until the *Target* is reached.\n\n" +
                            $"Variable Reference: {(progressionChild as VariableReference).VariableName}";
                    }
                    else
                    {
                        units = (targetChild as Constant).Units ?? "";
                        return $"This phase goes from {genericPhase.Start.ToLower()} to {genericPhase.End.ToLower()}.\n\n" +
                        $"The *Target* for completion is calculated as:\n\n" +
                        $"{(targetChild as Constant).Value()} {units}\n\n" +
                        $"*Progression* through phase is calculated daily and accumulated until the *Target* is reached.\n\n" +
                        $"Variable Reference: {(progressionChild as VariableReference).VariableName}";
                    }
                }
                else return "";
            }
            else return "The {model.Name} does not specify stages.";

        }
    }
}
