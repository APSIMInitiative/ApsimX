using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Stores specified child components with descriptive summary details for handling display
    /// </summary>
    public class ChildComponentGroup
    {
        /// <summary>
        /// List of selected models
        /// </summary>
        public IEnumerable<IModel> SelectedModels { get; set; }

        /// <summary>
        /// The type of children in the group
        /// </summary>
        public Type ChildType { get; set; } = null;

        /// <summary>
        /// Switch to determine if the group is included or ignored
        /// </summary>
        public bool Include { get; set; } = true;

        /// <summary>
        /// Text to display at start of group
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// Text to display if group is empty
        /// </summary>
        public string Missing { get; set; }

        /// <summary>
        /// Name of border class to use being ignored if empty string
        /// </summary>
        public string BorderCssClass { get; set; }

        /// <summary>
        /// Label to identify this group
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ChildComponentGroup()
        {
                
        }

        /// <summary>
        /// Constructor to set required and optional properties
        /// </summary>
        /// <param name="id">The label used to identify the group</param>
        /// <param name="model">Parent CLEM model</param>
        /// <param name="childType">Type of child if needed for display</param>
        /// <param name="include">Switch to determine if this group is included in display</param>
        /// <param name="introduction">Introduction text to place at top of group (html formatted)</param>
        /// <param name="missing">Text to display in error banner if no models found</param>
        /// <param name="borderClass">The name of the css border to display around group</param>
        public ChildComponentGroup(string id, CLEMModel model, Type childType, bool include = true, string introduction = "", string missing = "", string borderClass = "")
        {
            Id = id;
            Include = include;
            Introduction = introduction;
            ChildType = childType;

            SelectedModels = model.Structure.FindChildren<IModel>().Where(a => a.GetType() == ChildType);

            if (missing == "default")
                Missing = GetMissingChildType();
            else
                Missing = missing;

            BorderCssClass = borderClass;
        }

        /// <summary>
        /// Constructor to set required and optional properties
        /// </summary>
        /// <param name="id">The label used to identify the group</param>
        /// <param name="models">A list of models in the group</param>
        /// <param name="include">Switch to determine if this group is included in display</param>
        /// <param name="introduction">Introduction text to place at top of group (html formatted)</param>
        /// <param name="missing">Text to display in error banner if no models found</param>
        /// <param name="borderClass">The name of the css border to display around group</param>
        /// <param name="childType">Type of child if needed for display</param>
        public ChildComponentGroup(string id, IEnumerable<IModel> models, bool include = true, string introduction = "", string missing = "", string borderClass = "", Type childType = null)
        {
            Id = id;
            SelectedModels = models;
            Include = include;
            Introduction = introduction;
            ChildType = childType;

            if (missing == "default")
                Missing = GetMissingChildType();
            else

                Missing = missing;

            BorderCssClass = borderClass;
        }

        /// <summary>
        /// Generate missing text based on standard "not provided" using child type
        /// </summary>
        public string GetMissingChildType()
        {
            if (ChildType is null)
                return "";
            HTMLSummaryStyle entryStyle = HTMLSummaryStyle.Default;
            if (ChildType is IResourceType)
                entryStyle = HTMLSummaryStyle.Resource;

            return $"No {CLEMModel.DisplaySummaryValueSnippet((ChildType != null ? ChildType.Name : "component"), entryStyle: entryStyle)} provided!";
        }

    }
}
