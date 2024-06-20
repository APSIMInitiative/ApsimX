using System;
using System.Collections.Generic;
using Models.PMF;
using APSIM.Shared.Documentation;

using static Models.Core.AutoDocumentation;

namespace Models.Core
{
    /// <summary>
    /// An alias model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Cultivar))]
    public class Alias : Model
    {
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="headingLevel"></param>
        /// <param name="indent"></param>
        public void Document(List<ITag> tags, int headingLevel, int indent)
        {
            tags.Add(new Heading(Name, headingLevel + 1));
            tags.Add(new Paragraph($"An alias for {Parent?.Name}", indent));
        }
    }
}
