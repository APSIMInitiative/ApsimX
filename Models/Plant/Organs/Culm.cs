using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Data passed to sorghumLeaf to initialise Culms.
    /// </summary>
    public class CulmParameters
    {
        /// <summary>The numeric rank of the cohort appearing</summary>
        public int CulmNumber { get; set; }

        /// <summary>The Leaf Number when the Tiller was added</summary>
        public double LeafAtAppearance { get; set; }

        /// <summary>The proportion of a whole tiller</summary>
        public double Proportion { get; set; }

        /// <summary>The area calcs for subsequent tillers are the same shape but not as tall</summary>
        public double VerticalAdjustment { get; set; }
    }

    ///<summary>
    /// A Culm represents a collection of leaves
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Culm : Model, ICustomDocumentation
    {
        /// <summary>The numeric rank of the cohort appearing</summary>
        public int CulmNumber { get; set; }

        /// <summary>The Leaf Number when the Tiller was added</summary>
        public double LeafAtAppearance { get; set; }

        /// <summary>The proportion of a whole tiller</summary>
        public double Proportion { get; set; }

        /// <summary>The area calcs for subsequent tillers are the same shape but not as tall</summary>
        public double VerticalAdjustment { get; set; }

        /// <summary>The area calcs for subsequent tillers are the same shape but not as tall</summary>
        public double CurrentLeafNumber { get; set; }


        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                //tags.Add(new AutoDocumentation.Paragraph("Area = " + Area, indent));
            }
        }
    }
}
