using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocClock : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocClock" /> class.
        /// </summary>
        public DocClock(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);

            tags.Add(new Section(model.Name, GetModelEventsInvoked(typeof(Clock), "OnDoCommence(object _, CommenceArgs e)", "CLEM", true)));
            
            return tags;
        }
    }
}
