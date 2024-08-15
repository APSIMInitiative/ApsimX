using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Documentation;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Phen;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocPhenology : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPhenology" /> class.
        /// </summary>
        public DocPhenology(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            return tags;
        }
    }
}