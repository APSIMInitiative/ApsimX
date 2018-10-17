namespace Models.Core
{
    using Models.Core.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>This class loads a model from a resource</summary>
    [Serializable]
    public class ModelCollectionFromResource : Model, IOptionallySerialiseChildren
    {
        /// <summary>Gets or sets the name of the resource.</summary>
        public string ResourceName { get; set; }

        /// <summary>Allow children to be serialised?</summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool DoSerialiseChildren { get; private set; } = true;

        /// <summary>
        /// We have just been deserialised. If from XML then load our model
        /// from resource.
        /// </summary>
        public override void OnCreated()
        {
            // lookup the resource get the xml and then deserialise to a model.
            if (ResourceName != null && ResourceName != "")
            {
                string contents = Properties.Resources.ResourceManager.GetString(ResourceName);
                if (contents != null)
                {
                    List<Exception> creationExceptions;
                    Model ModelFromResource = ApsimFile.FileFormat.ReadFromString<Model>(contents, out creationExceptions);
                    Children.Clear();
                    Children.AddRange(ModelFromResource.Children);
                    CopyPropertiesFrom(ModelFromResource);
                    SetNotVisible(ModelFromResource);
                    Apsim.ParentAllChildren(this);
                    DoSerialiseChildren = false;
                }
            }
        }

        /// <summary>
        /// Copy all properties from the specified resource.
        /// </summary>
        /// <param name="from">Model to copy from</param>
        private void CopyPropertiesFrom(Model from)
        {
            foreach (PropertyInfo property in from.GetType().GetProperties())
            {
                if (property.CanWrite &&
                    property.Name != "Name" &&
                    property.Name != "Parent" &&
                    property.Name != "Children" &&
                    property.Name != "IncludeInDocumentation" &&
                    property.Name != "ResourceName")
                {
                    object fromValue = property.GetValue(from);
                    bool doSetPropertyValue;
                    if (fromValue is double)
                        doSetPropertyValue = Convert.ToDouble(fromValue) != 0;
                    else
                        doSetPropertyValue = fromValue != null;

                    if (doSetPropertyValue)
                        property.SetValue(this, fromValue);
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
                child.ReadOnly = true;
                SetNotVisible(child);
            }
        }

    }
}
