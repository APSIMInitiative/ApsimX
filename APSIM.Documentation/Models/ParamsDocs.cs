using System.Collections.Generic;
using APSIM.Interop.Documentation;
using Models.Core;
using APSIM.Services.Documentation;
using APSIM.Interop.Documentation.Formats;
using System.Linq;
using MigraDocCore.DocumentObjectModel;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A params/inputs/outputs pdf file.
    /// </summary>
    internal class ParamsDocs : DocsFromFile
    {
        /// <summary>
        /// Create a new <see cref="ParamsDocs"/> instance for the given input file.
        /// </summary>
        /// <param name="input">The input file.</param>
        /// <param name="output">Name of the file which will be generated.</param>
        /// <param name="options">Pdf generation options.</param>
        public ParamsDocs(string input, string output, PdfOptions options) : base(input, output, options)
        {
        }

        protected override IEnumerable<ITag> DocumentModel(IModel model)
        {
            if (model is Simulations && model.Children.Any())
                model = model.Children[0];
            ParamsInputsOutputs doco = new ParamsInputsOutputs(model);
            return doco.Document();
        }

        protected override Document CreateDocument()
        {
            return PdfWriter.CreateStandardDocument(false);
        }
    }
}
