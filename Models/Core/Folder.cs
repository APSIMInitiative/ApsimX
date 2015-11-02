using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Models.Core
{
    /// <summary>
    /// A folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Folder : Model
    {

    }
}
