using System;
using System.Text;
using Models.Core;

namespace Models
{
    /// <summary>
    /// This is a memo/text component that stores user entered text information.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MemoView")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    public class Memo : Model
    {
        public Memo()
        {

        }

        [Description("Text of the memo")]
        public string MemoText { get; set; }
    }
}
