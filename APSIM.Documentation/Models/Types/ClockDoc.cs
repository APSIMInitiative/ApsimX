using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class ClockDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDoc" /> class.
        /// </summary>
        public ClockDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Section(model.Name, GetModelDescription());
            yield return new Section(model.Name, GetModelEventsInvoked(typeof(Clock), "OnDoCommence(object _, CommenceArgs e)", "CLEM", true));
        }
    }
}
