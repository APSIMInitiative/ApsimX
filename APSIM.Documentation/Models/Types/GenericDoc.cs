using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class GenericDoc
    {
        /// <summary>
        /// The model that the documentation should be generated for
        /// </summary>
        protected IModel model = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDoc" /> class.
        /// </summary>
        public GenericDoc(IModel model)
        {
            this.model = model;
        }

        /// <summary>
        /// Document the model, and any child models which should be documented.
        /// </summary>
        /// <remarks>
        /// It is a mistake to call this method without first resolving links.
        /// </remarks>
        public virtual IEnumerable<ITag> Document()
        {
            return DocumentSubSection(new List<ITag>(), 0, 0);
        }

        /// <summary>
        /// Document the model as a subsection of another document
        /// </summary>
        public virtual IEnumerable<ITag> DocumentSubSection(List<ITag> tags, int headingLevel, int indent)
        {
            yield return new Section(model.Name, GetModelDescription());
        }

        /// <summary>
        /// Get a description of the model from the summary and remarks
        /// xml documentation comments in the source code.
        /// </summary>
        /// <remarks>
        /// Note that the returned tags are not inside a section.
        /// </remarks>
        protected IEnumerable<ITag> GetModelDescription()
        {
            yield return new Paragraph(CodeDocumentation.GetSummary(GetType()));
            yield return new Paragraph(CodeDocumentation.GetRemarks(GetType()));
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
