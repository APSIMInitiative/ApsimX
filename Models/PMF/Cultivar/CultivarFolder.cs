using System;
using APSIM.Shared.Documentation;
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
        public override IEnumerable<ITag> Document()
        {
            foreach (ITag tag in DocumentChildren<Cultivar>(true))
                yield return tag;

            foreach (ITag tag in DocumentChildren<CultivarFolder>(true))
                yield return tag;
        }
    }
}
