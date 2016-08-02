using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Factorial;
using Models.PMF;
using APSIM.Shared.Utilities;
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
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write memos
            foreach (IModel childFolder in Apsim.Children(this, typeof(Memo)))
                childFolder.Document(tags, headingLevel + 1, indent);

            // write a sorted list of cultivar names.
            List<string> cultivarNames = new List<string>();
            foreach (Cultivar cultivar in Apsim.Children(this, typeof(Cultivar)))
            {
                cultivarNames.Add(cultivar.Name);
                cultivarNames.AddRange(cultivar.Aliases);
            }
            cultivarNames.Sort();

            string text = StringUtilities.BuildString(cultivarNames.ToArray(), ", ");
            if (text != string.Empty)
                tags.Add(new AutoDocumentation.Paragraph(text, indent));

            // write child folders.
            foreach (IModel childFolder in Apsim.Children(this, typeof(CultivarFolder)))
                childFolder.Document(tags, headingLevel + 1, indent);
        }
    }
}
