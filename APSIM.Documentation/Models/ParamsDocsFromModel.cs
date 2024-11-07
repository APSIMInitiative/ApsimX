using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Documentation;
using MigraDocCore.DocumentObjectModel;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A params/inputs/outputs pdf file.
    /// </summary>
    internal class ParamsDocsFromModel<T> : DocsFromModel<T> where T : IModel
    {
        /// <summary>
        /// Create a new <see cref="ParamsDocsFromFile"/> instance for the given input file.
        /// </summary>
        /// <param name="output">Name of the file which will be generated.</param>
        public ParamsDocsFromModel(string output) : base(output)
        {
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="model">Model to be documented.</param>
        protected override IEnumerable<ITag> DocumentModel(IModel model)
        {
            return InterfaceDocumentation.Document(model);
        }

        /// <summary>
        /// Create a standard document (overriding to make it landscape).
        /// </summary>
        protected override Document CreateDocument()
        {
            return null;
        }
    }
}
