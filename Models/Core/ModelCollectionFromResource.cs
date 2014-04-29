using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Models.Core
{
    /// <summary>
    /// This class loads a model from a resource
    /// </summary>
    [Serializable]
    public class ModelCollectionFromResource : ModelCollection
    {
        public string ResourceName { get; set; }
        private List<Model> SavedModels;

        /// <summary>
        /// We're about to be serialised. Remove our 'ModelFromResource' model from the list
        /// of all models so that is isn't serialised. 
        /// </summary>
        public override void OnSerialising(bool xmlSerialisation)
        {
            if (xmlSerialisation && ResourceName != null)
            {
                SavedModels = new List<Model>();
                SavedModels.AddRange(Models);
                Models.Clear();
            }
        }

        /// <summary>
        /// Serialisation has completed. Reinstate 'ModelFromResource' if necessary.
        /// </summary>
        public override void OnSerialised(bool xmlSerialisation)
        {
            if (xmlSerialisation && SavedModels != null)
                Models.AddRange(SavedModels);
        }

        /// <summary>
        /// We have just been deserialised. If from XML then load our model
        /// from resource.
        /// </summary>
        public override void OnDeserialised(bool xmlSerialisation)
        {
            base.OnDeserialised(xmlSerialisation);
            if (xmlSerialisation)
            {
                // lookup the resource get the xml and then deserialise to a model.
                if (ResourceName != null && ResourceName != "")
                {
                    string xml = Properties.Resources.ResourceManager.GetString(ResourceName);
                    if (xml != null)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xml);
                        ModelCollection ModelFromResource = Utility.Xml.Deserialise(doc.DocumentElement) as ModelCollection;
                        Models.AddRange(ModelFromResource.Models);
                    }
                }
            }
        }

    }
}
