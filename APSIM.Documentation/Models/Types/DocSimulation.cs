using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{
    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocSimulation : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSimulation" /> class.
        /// </summary>
        public DocSimulation(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);
            
            foreach (IModel child in model.Children)
                tags = AutoDocumentation.Document(child, heading+1);

            return tags;
        }
    }
}
