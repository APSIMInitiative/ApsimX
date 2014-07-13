using System;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using System.Xml;

namespace Models
{
    /// <summary>
    /// This is a memo/text component that stores user entered text information.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.HTMLView")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    public class Memo : Model
    {
        public Memo()
        {

        }

        [XmlIgnore]
        [Description("Text of the memo")]
        public string MemoText { get; set; }

        [XmlElement("MemoText")]
        public XmlNode CodeCData
        {
            get
            {
                XmlDocument dummy = new XmlDocument();
                return dummy.CreateCDataSection(MemoText);
            }
            set
            {
                if (value != null && value.InnerText.StartsWith("<"))
                    MemoText = value.InnerText;
            }
        }
    }
}
