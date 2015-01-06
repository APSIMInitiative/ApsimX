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
    /// <summary>This class loads a model from a resource</summary>
    [Serializable]
    public class ModelCollectionFromResource : Model
    {
        /// <summary>Gets or sets the name of the resource.</summary>
        /// <value>The name of the resource.</value>
        public string ResourceName { get; set; }

        /// <summary>All models</summary>
        private List<Model> allModels;

        /// <summary>
        /// We're about to be serialised. Remove our 'ModelFromResource' model from the list
        /// of all models so that is isn't serialised.
        /// </summary>
        /// <param name="xmlSerialisation">if set to <c>true</c> [XML serialisation].</param>
        [EventSubscribe("Serialising")]
        protected void OnSerialising(bool xmlSerialisation)
        {
            if (xmlSerialisation && ResourceName != null)
            {
                allModels = new List<Model>();
                allModels.AddRange(Children);

                List<Model> visibleModels = new List<Model>();
                foreach (Model child in Children)
                {
                    if (!child.IsHidden)
                    {
                        visibleModels.Add(child);
                    }
                }

                Children = visibleModels;
            }
        }

        /// <summary>Serialisation has completed. Reinstate 'ModelFromResource' if necessary.</summary>
        /// <param name="xmlSerialisation">if set to <c>true</c> [XML serialisation].</param>
        [EventSubscribe("Serialised")]
        protected void OnSerialised(bool xmlSerialisation)
        {
            if (xmlSerialisation && allModels != null)
            {
                Children = allModels;
            }
        }

        /// <summary>
        /// We have just been deserialised. If from XML then load our model
        /// from resource.
        /// </summary>
        /// <param name="xmlSerialisation">if set to <c>true</c> [XML serialisation].</param>
        [EventSubscribe("Deserialised")]
        protected void OnDeserialised(bool xmlSerialisation)
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
                        Children.AddRange(ModelFromResource.Children);

                        SetNotVisible(ModelFromResource);
                    }
                }
            }
        }

        /// <summary>Sets the not visible.</summary>
        /// <param name="ModelFromResource">The model from resource.</param>
        private static void SetNotVisible(Model ModelFromResource)
        {
            foreach (Model child in ModelFromResource.Children)
            {
                child.IsHidden = true;
                SetNotVisible(child);
            }
        }

    }
}
