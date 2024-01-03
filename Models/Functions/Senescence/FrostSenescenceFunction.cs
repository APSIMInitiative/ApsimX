using System;
using System.Collections.Generic;
using System.Text;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Organs;

namespace Models.Functions
{
    /// <summary> Frost Senescense</summary>
    [Serializable]
    [Description("Frost Senescence")]
    public class FrostSenescenceFunction : Model, IFunction
    {
        [Link(Type = LinkType.Ancestor)]
        private SorghumLeaf leaf = null;

        /// <summary>
        /// Linke to weather, used for frost senescence calcs.
        /// </summary>
        [Link]
        private IWeather weather = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather metData = null;

        [Link]
        private ISummary summary = null;

        /// <summary> Temperature threshold for leaf death, when plant is between floral init and flowering. </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frostKill = null;

        /// <summary>Temperature threshold for leaf death.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frostKillSevere = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            var frostEventThreshold = frostKill.Value();

            if (weather.MinT > frostEventThreshold)
                return 0;

            var frostKillThreshold = frostKillSevere.Value();

            if (MathUtilities.IsLessThanOrEqual(weather.MinT, frostKillSevere.Value()))
            {
                // Temperature is below frostKillSevere parameter, senesce all LAI.
                summary.WriteMessage(this, FrostSenescenceMessage(fatal: true), MessageType.Diagnostic);
                return leaf.LAI;
            }

            // Temperature is warmer than frostKillSevere, but cooler than frostKill.
            // So the plant will only die if between floral init - flowering.
            if (leaf.phenology.Between("Germination", "FloralInitiation"))
            {
                // The plant will survive but all of the leaf area is removed except a fraction.
                // 3 degrees is a default for now - extract to a parameter to customise it.
                summary.WriteMessage(this, FrostSenescenceMessage(fatal: false), MessageType.Diagnostic);
                return Math.Max(0, leaf.LAI - 0.1);
            }

            if (leaf.phenology.Between("FloralInitiation", "Flowering"))
            {
                // Plant is between floral init and flowering - time to die.
                summary.WriteMessage(this, FrostSenescenceMessage(fatal: true), MessageType.Diagnostic);
                return leaf.LAI; // rip
            }

            // After flowering it takes a severe frost to kill the plant
            // (which didn't happen today).
            //there should probably be some leaf damage?
            return 0;
        }

        /// <summary>
        /// Generates a message to be displayed when senescence due to frost
        /// occurs. Putting this in a method for now so we don't have the same
        /// code twice, but if frost senescence is tweaked in the future it might
        /// just be easier to do away with the method and hardcode similar
        /// messages multiple times.
        /// </summary>
        /// <param name="fatal">Was the frost event fatal?</param>
        private string FrostSenescenceMessage(bool fatal)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine($"Frost Event: ({(fatal ? "Fatal" : "Non Fatal")})");
            message.AppendLine($"\tMin Temp     = {weather.MinT}");
            message.AppendLine($"\tSenesced LAI = {leaf.LAI - 0.01}");
            return message.ToString();
        }

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            List<ITag> senescenceTags = new List<ITag>();

            senescenceTags.Add(new Paragraph($"FrostKill: {frostKill.Value()} °C"));
            senescenceTags.Add(new Paragraph($"FrostKillSevere: {frostKillSevere.Value()} °C"));

            senescenceTags.Add(new Paragraph($"If minimum temperature falls below FrostKillSevere then all LAI is removed causing plant death"));
            senescenceTags.Add(new Paragraph($"If the minimum temperature is above FrostKillSevere, but below FrostKill, then the effect on the plant will depend on which phenologiacl stage the plant is in:"));
            senescenceTags.Add(new Paragraph($"  Before Floral Initiation: Nearly all of the LAI will be removed, but if not under any other stress, the plant can survive."));
            senescenceTags.Add(new Paragraph($"  Before Flowering: All of the LAI will be removed, casuing plant death."));
            senescenceTags.Add(new Paragraph($"  After Flowering: The leaf is not damaged."));

            yield return new Section(Name, senescenceTags);
        }
    }
}


