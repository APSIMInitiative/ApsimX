using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using System.Xml;

namespace Models.Factorial
{
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.FactorPresenter")]
    [ValidParent(ParentModels = new Type[] { typeof(Factorial.Factors) })]
    public class Factor : Model
    {
        public List<string> Paths { get; set; }

        [XmlIgnore]
        public List<IModel> FactorValues { get { return Apsim.Children(this, typeof(FactorValue)); } }
    }
}
