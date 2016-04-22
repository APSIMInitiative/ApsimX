using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.Functions
{
    /// <summary>Returns the value of today's MaximumTemperature</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class MaxTFunction : Model, IFunction
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>The clock</summary>
        [Link]
        protected Clock Clock = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (MetData != null)
                    return MetData.MaxT;
                return 0;                    
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // get description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

         }

    }
}