// -----------------------------------------------------------------------
// <copyright file="Model.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using Storage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Base class for all models
    /// </summary>
    [Serializable]
    public class Model : IModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Model" /> class.
        /// </summary>
        public Model()
        {
            this.Name = GetType().Name;
            this.IsHidden = false;
            this.Children = new List<Model>();
            IncludeInDocumentation = true;
            Enabled = true;
        }

        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a list of child models.   
        /// </summary>
        public List<Model> Children { get; set; }

        /// <summary>
        /// Gets or sets the parent of the model.
        /// </summary>
        [XmlIgnore]
        public IModel Parent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        [XmlIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in the auto-doc documentation.
        /// </summary>
        public bool IncludeInDocumentation { get; set; }

        /// <summary>
        /// A cleanup routine, in which we clear our child list recursively
        /// </summary>
        public void ClearChildLists()
        {
            foreach (Model child in Children)
                child.ClearChildLists();
            Children.Clear();
        }

        /// <summary>
        /// Gets or sets whether the model is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Return the current APSIM version number.
        /// </summary>
        public string ApsimVersion
        {
            get
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                FileInfo info = new FileInfo(Assembly.GetExecutingAssembly().Location);
                string buildDate = info.LastWriteTime.ToString("yyyy-MM-dd");
                return "Version " + version + ", built " + buildDate;
            }
        }

        /// <summary>
        /// Controls whether the model can be modified.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        public virtual void OnCreated() { }
    }
}
