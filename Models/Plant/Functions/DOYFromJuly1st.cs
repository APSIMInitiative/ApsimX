using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;
using StdUnits;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    public class DOYFromJuly1st : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;

        ///// <summary>The stdUnits</summary>
        //       [Link]
        // public StdDate stdDate = null;


        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (!StdDate.LeapYear(Clock.Today.Year))

                return ((Clock.Today.DayOfYear - 181) > 0) ? Clock.Today.DayOfYear - 181 : Clock.Today.DayOfYear + 184;

            else

                return ((Clock.Today.DayOfYear - 182) > 0) ? Clock.Today.DayOfYear - 182 : Clock.Today.DayOfYear + 184;
            
        }

        
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);
            }
        }

    }

}