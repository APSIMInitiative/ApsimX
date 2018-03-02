// -----------------------------------------------------------------------
// <copyright file="ArrayFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using System.Diagnostics;

    /// <summary>
    /// Returns the value at the given index. If the index is outside the array, the last value will be returned.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ArrayFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>Gets the value.</summary>
        [Description("The values of the array (space seperated)")]
        public string ArrayValues { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("The optional units of the array")]
        public string Units { get; set; }

        private double[] str2dbl = null;

        /// <summary>Gets the value of the function.</summary>
        public override double[] Values()
        {
            if (str2dbl == null)
            {
                string[] split = ArrayValues.Split(' ');
                str2dbl = new double[split.Length];
                for (int i = 0; i < split.Length; i++)
                    try
                    {
                        str2dbl[i] = Convert.ToDouble(split[i], System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        throw new ApsimXException(this, "ArrayFunction: Could not convert " + split[i] + " to a number.");
                    }
            }

            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + ArrayValues);
            return str2dbl;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // get description and units.
            string description = AutoDocumentation.GetDescription(Parent, Name);
            string units = Units;
            if (units == null)
                units = AutoDocumentation.GetUnits(Parent, Name);
            if (units != string.Empty)
                units = " (" + units + ")";

            if (!(Parent is IFunction) && headingLevel > 0)
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            //TODO: Tidy up the printing -JF
            tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + ArrayValues + units + "</i>", indent));

            if (!String.IsNullOrEmpty(description))
                tags.Add(new AutoDocumentation.Paragraph(description, indent));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                AutoDocumentation.DocumentModel(memo, tags, -1, indent);

        }
    }
}