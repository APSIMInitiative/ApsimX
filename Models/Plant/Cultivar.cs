using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
namespace Models.PMF
{

    /// <summary>
    /// This class is only here so that the xml has a <Cultivars> node
    /// </summary>
    [Serializable]
    public class Cultivars: Model
    {
        [XmlElement("Cultivar")]
        public Cultivar[] CultivarDefinitions { get; set; }

        /// <summary>
        /// Find a cultivar definition for 'cultivar'. Returns null if not found.
        /// </summary>
        public Cultivar FindCultivar(string cultivar)
        {
            foreach (Cultivar cult in CultivarDefinitions)
                if (cult.Name.Equals(cultivar, StringComparison.CurrentCultureIgnoreCase))
                    return cult;
            return null;
        }
    }

    /// <summary>
    /// A cultivar overide class for holding a single variable override.
    /// </summary>
    [Serializable]
    public class Override
    {
        public string Name;
        public string Value;
        [XmlIgnore]
        public object OldValue;

        public void Apply(Model model)
        {
            OldValue = model.Get(Name);
            if (OldValue == null)
                throw new ApsimXException(model.FullPath, "In cultivar " + model.Name + ", cannot find variable " + Name);
            object objValue = Convert.ChangeType(Value, OldValue.GetType());
            model.Set(Name, objValue);
        }
        public void Unapply(Model model)
        {
            model.Set(Name, OldValue);
            if (OldValue == null)
                throw new ApsimXException(model.FullPath, "In cultivar " + model.Name + ", cannot find variable " + Name);
        }

    }
    /// <summary>
    /// Cultivar class for holding cultivar overrides.
    /// </summary>
    [Serializable]
    public class Cultivar
    {
        public string Name { get; set; }
        [XmlElement("Override")]
        public Override[] Overrides { get; set; }

        /// <summary>
        /// Apply override.
        /// </summary>
        public void ApplyOverrides(Model model)
        {
            if (Overrides != null)
                foreach (Override overrideDefinition in Overrides)
                    overrideDefinition.Apply(model);
        }

        /// <summary>
        /// Undo the override apply. i.e. apply the original values.
        /// </summary>
        public void UnapplyOverrides(Model model)
        {
            if (Overrides != null)
                foreach (Override overrideDefinition in Overrides)
                    overrideDefinition.Unapply(model);
        }
    }
}
