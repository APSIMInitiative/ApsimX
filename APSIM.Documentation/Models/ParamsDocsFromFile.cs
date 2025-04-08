using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Documentation;
using System.Linq;
using MigraDocCore.DocumentObjectModel;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A params/inputs/outputs pdf file.
    /// </summary>
    internal class ParamsDocsFromFile : DocsFromFile
    {
        /// <summary>
        /// Optional path to the model to be documented.
        /// </summary>
        private string path;

        /// <summary>
        /// Create a new <see cref="ParamsDocsFromFile"/> instance for the given input file.
        /// </summary>
        /// <param name="input">The input file.</param>
        /// <param name="output">Name of the file which will be generated.</param>
        /// <param name="path">(Optional) path to model to be documented.</param>
        public ParamsDocsFromFile(string input, string output, string path = null) : base("Interface", input, output)
        { 
            this.path = path;
        }

        protected override IEnumerable<ITag> DocumentModel(IModel model)
        {
            if (!string.IsNullOrEmpty(path))
            {
                IVariable variable = model.FindByPath(path);
                if (variable != null && variable.Value is IModel value)
                    model = value;
                else
                    model = model.FindDescendant(path);
            }
            else if (model is Simulations && model.Children.Any())
                model = model.Children[0];

            return InterfaceDocumentation.Document(model);
        }

        protected override Document CreateDocument()
        {
            return null;
        }
    }
}
