using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocGeneric
    {
        /// <summary>
        /// The model that the documentation should be generated for
        /// </summary>
        protected IModel model = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocGeneric" /> class.
        /// </summary>
        public DocGeneric(IModel model)
        {
            this.model = model;
        }

        /// <summary>
        /// Document the model
        /// </summary>
        public virtual IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();

            List<ITag> subTags = new List<ITag>();
            subTags.Add(new Heading(this.model.Name, headingLevel));
            subTags.Add(new Paragraph(CodeDocumentation.GetSummary(model.GetType())));
            subTags.Add(new Paragraph(CodeDocumentation.GetRemarks(model.GetType())));

            // write children.
            foreach (IModel child in model.FindAllChildren<Memo>())
                AutoDocumentation.Document(child, subTags, headingLevel + 1, indent);

            tags.Add(new Section(model.Name, subTags));
            return tags;
        }

        /// <summary>
        /// Gets a list of Event Handles that are Invoked in the provided function
        /// </summary>
        /// <remarks>
        /// Model source file must be included as embedded resource in project xml
        /// </remarks>
        protected IEnumerable<ITag> GetModelEventsInvoked(Type type, string functionName, string filter = "", bool filterOut = false)
        {
            List<string[]> eventNames = CodeDocumentation.GetEventsInvokedInOrder(type, functionName);

            List<string[]> eventNamesFiltered = new List<string[]>();
            if (filter.Length > 0)
            {
                foreach (string[] name in eventNames)
                    if (name[0].Contains(filter) == !filterOut)
                    { 
                        eventNamesFiltered.Add(name); 
                    }           
            }
            yield return new Paragraph($"Function {functionName} of Model {model.Name} contains the following Events in the given order.\n");

            DataTable data = new DataTable();
            data.Columns.Add("Event Handle", typeof(string));
            data.Columns.Add("Summary", typeof(string));

            for (int i = 1; i < eventNamesFiltered.Count; i++)
            {
                string[] parts = eventNamesFiltered[i];

                DataRow row = data.NewRow();
                data.Rows.Add(row);
                row["Event Handle"] = parts[0];
                row["Summary"] = parts[1];
            }
            yield return new Table(data);
        }

        /// <summary>
        /// Document all child models of a given type.
        /// </summary>
        /// <param name="withHeadings">If true, each child to be documented will be given its own section/heading.</param>
        /// <typeparam name="T">The type of models to be documented.</typeparam>
        protected IEnumerable<ITag> DocumentChildren<T>(bool withHeadings = false) where T : IModel
        {
            if (withHeadings)
                return model.FindAllChildren<T>().Select(m => new Section(m.Name, m.Document()));
            else
                return model.FindAllChildren<T>().SelectMany(m => m.Document());
        }
    }
}
