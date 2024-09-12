using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{
    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocManager : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocManager" /> class.
        /// </summary>
        public DocManager(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);
            
            List<ITag> subTags = new List<ITag>();
            foreach (KeyValuePair<string, string> pair in (model as Manager).Parameters)
                subTags.Add(new Paragraph(pair.Key + " = " + pair.Value));

            (tags[0] as Section).Children.Add(new Section("Parameters", subTags));

            return tags;
        }
    }
}