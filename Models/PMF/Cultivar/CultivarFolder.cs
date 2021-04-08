using System;
using APSIM.Services.Documentation;
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
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos
                foreach (IModel childFolder in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(childFolder, tags, headingLevel + 1, indent);

                // write a sorted list of cultivar names.
                List<string> cultivarNames = new List<string>();
                foreach (Cultivar cultivar in this.FindAllChildren<Cultivar>())
                {
                    cultivarNames.Add(cultivar.Name);
                    cultivarNames.AddRange(cultivar.Alias);
                }
                cultivarNames.Sort();

                string text = StringUtilities.BuildString(cultivarNames.ToArray(), ", ");
                if (text != string.Empty)
                    tags.Add(new AutoDocumentation.Paragraph(text, indent));

                // write child cultivars.
                foreach (IModel childCultivar in this.FindAllChildren<Cultivar>())
                    AutoDocumentation.DocumentModel(childCultivar, tags, headingLevel + 1, indent);

                // write child folders.
                foreach (IModel childFolder in this.FindAllChildren<CultivarFolder>())
                    AutoDocumentation.DocumentModel(childFolder, tags, headingLevel + 1, indent);
            }
        }
    }
}
