using System;

namespace Models.Core
{
    /// <summary>
    /// A folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [ScopedModel]
    [Serializable]
    [ValidParent(DropAnywhere = true)]
    public class Folder : Model
    {
        /// <summary>Show in the documentation</summary>
        /// <remarks>
        /// Whether this fodler should show up in documentation or not.
        /// </remarks>
        public bool ShowInDocs { get; set; }
    }
}
