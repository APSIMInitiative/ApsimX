using System;


namespace Models.Core
{
    /// <summary>
    /// A section model
    /// </summary>
    [ViewName("UserInterface.Views.MarkdownView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    [Serializable]
    [ValidParent(DropAnywhere = true)]
    public class Group : Model
    {


    }
}
