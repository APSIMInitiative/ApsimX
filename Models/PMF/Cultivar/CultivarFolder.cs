using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using Models.Core;
using System.Linq;

namespace Models.PMF
{
    /// <summary>
    /// A folder of cultivars
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(CultivarFolder))]
    public class CultivarFolder : Model
    {
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        public override IEnumerable<ITag> Document(uint indent, uint headingLevel)
        {
            yield return new Heading(Name, headingLevel);

            // Write a sorted list of cultivar names.
            List<string> cultivarNames = FindAllChildren<Cultivar>().SelectMany(c => c.GetNames()).ToList();
            cultivarNames.Sort();

            string text = string.Join(", ", cultivarNames);
            if (!string.IsNullOrEmpty(text))
                yield return new Paragraph(text, indent);
        }
    }
}
