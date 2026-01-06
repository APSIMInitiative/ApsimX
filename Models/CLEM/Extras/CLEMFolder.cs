using Models.Core;
using System;
using APSIM.Core;

namespace Models.CLEM
{
    /// <summary>
    /// A CLEM specific folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    public class CLEMFolder: Folder, IScopedModel
    {
    }
}
