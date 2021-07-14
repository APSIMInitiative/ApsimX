namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using APSIM.Services.Documentation;

    /// <summary>
    /// A replacements model
    /// </summary>
    [ValidParent(ParentType=typeof(Simulations))]
    [Serializable]
    [ScopedModel]
    public class Replacements : Model
    {
        /// <summary>
        /// This model is not documented in autodocs.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield break;
        }
    }
}
