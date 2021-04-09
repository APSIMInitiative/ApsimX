using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using Models.Core;

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
        public override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            yield return new Heading(Name, headingLevel);

            // Write a sorted list of cultivar names.
            List<string> cultivarNames = new List<string>();
            foreach (Cultivar cultivar in this.FindAllChildren<Cultivar>())
            {
                cultivarNames.Add(cultivar.Name);
                cultivarNames.AddRange(cultivar.Alias);
            }
            cultivarNames.Sort();

            string text = string.Join(", ", cultivarNames);
            if (!string.IsNullOrEmpty(text))
                yield return new Paragraph(text, indent);
        }
    }
}
