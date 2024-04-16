using Models.Core;
using System;

namespace Models.CLEM
{
    /// <summary>
    /// A CLEM specific folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [ScopedModel]
    [Serializable]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    public class CLEMFolder: Folder
    {
    }
}
