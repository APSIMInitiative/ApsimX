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
    [ViewName("UserInterface.Views.FactorView")]
    [PresenterName("UserInterface.Presenters.FactorPresenter")]
    [ValidParent(ParentModels = new Type[] { typeof(Factorial.Factors) })]
    public class Factor : Model
    {
        public List<string> Paths { get; set; }

        [XmlIgnore]
        public List<FactorValue> FactorValues { get; private set; }

        public override void OnLoaded()
        {
            FactorValues = new List<FactorValue>();
            foreach (FactorValue factorValue in Children.MatchingMultiple(typeof(FactorValue)))
                FactorValues.Add(factorValue);
        }
    }
}
