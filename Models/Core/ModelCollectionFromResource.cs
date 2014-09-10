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
    public class ModelCollectionFromResource : Model
    {
        public string ResourceName { get; set; }
        private List<Model> allModels;

        /// <summary>
        /// We're about to be serialised. Remove our 'ModelFromResource' model from the list
        /// of all models so that is isn't serialised. 
        /// </summary>
        [EventSubscribe("Serialising")]
        public void OnSerialising(bool xmlSerialisation)
        {
            if (xmlSerialisation && ResourceName != null)
            {
                allModels = new List<Model>();
                allModels.AddRange(Models);

                List<Model> visibleModels = new List<Model>();
                foreach (Model child in Models)
                {
                    if (!child.IsHidden)
                    {
                        visibleModels.Add(child);
                    }
                }

                Models = visibleModels;
            }
        }

        /// <summary>
        /// Serialisation has completed. Reinstate 'ModelFromResource' if necessary.
        /// </summary>
        [EventSubscribe("Serialised")]
        public void OnSerialised(bool xmlSerialisation)
        {
            if (xmlSerialisation && allModels != null)
            {
                Models = allModels;
            }
        }

        /// <summary>
        /// We have just been deserialised. If from XML then load our model
        /// from resource.
        /// </summary>
        [EventSubscribe("Deserialised")]
        public void OnDeserialised(bool xmlSerialisation)
        {
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
                        Model ModelFromResource = Utility.Xml.Deserialise(doc.DocumentElement) as Model;
                        Models.AddRange(ModelFromResource.Children.All);

                        foreach (Model child in ModelFromResource.Children.All)
                        {
                            child.IsHidden = true;
                        }
                    }
                }
            }
        }

    }
}
