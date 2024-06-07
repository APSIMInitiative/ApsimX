using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class PlantDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlantDoc" /> class.
        /// </summary>
        public PlantDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Section($"The APSIM {model.Name} Model", GetTags());
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        private IEnumerable<ITag> GetTags()
        {
            
            // If first child is a memo, document it first.
            Memo introduction = this.model.Children?.FirstOrDefault() as Memo;
            if (introduction != null)
                foreach (ITag tag in introduction.Document())
                    yield return tag;

            foreach (var tag in GetModelDescription())
                yield return tag;

            yield return new Paragraph($"The model is constructed from the following list of software components. Details of the implementation and model parameterisation are provided in the following sections.");

            // Write Plant Model Table
            yield return new Paragraph("**List of Plant Model Components.**");
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Component Name", typeof(string));
            tableData.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() != typeof(Memo) && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(Folder) && child.GetType() != typeof(CompositeBiomass))
                {
                    DataRow row = tableData.NewRow();
                    row[0] = child.Name;
                    row[1] = child.GetType().ToString();
                    tableData.Rows.Add(row);
                }
            }
            yield return new Table(tableData);

            // Document children.
            foreach (IModel child in this.model.Children)
                if (child != introduction)
                    yield return new Section(child.Name, child.Document());
        }
    }
}
