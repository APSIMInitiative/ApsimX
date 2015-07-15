using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.IO;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the temperature of the surface soil layer
    /// </summary>
    [Serializable]
    [Description("returns the temperature of the surface soil layer")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilTemperatureFunction : Model, IFunction
    {
        /// <summary>The xy pairs</summary>
        [Link]
        private XYPairs XYPairs = null;               // Temperature effect on Growth Interpolation Set

        /// <summary>The maxt_soil_surface</summary>
        [Description("maxt_soil_surface")]
        public double maxt_soil_surface { get; set; } //Fixme.  These need to be connected to soil temp model when complete

        /// <summary>The mint_soil_surface</summary>
        [Description("mint_soil_surface")]
        public double mint_soil_surface { get; set; } //Fixme.  These need to be connected to soil temp model when complete
        
        /// <summary>constructor</summary>
        public SoilTemperatureFunction()
        {
            maxt_soil_surface = 20;
            mint_soil_surface = 10;
        }
        
        /// <summary>Gets the value.</summary>
        [Units("deg.day")]
        public double Value
        {
            get
            {
                AirTemperatureFunction airtempfunction = new AirTemperatureFunction();
                //airtempfunction.XYPairs = XYPairs;
                return airtempfunction.Linint3hrlyTemp(maxt_soil_surface, mint_soil_surface, XYPairs);
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

            // add graph and table.
            if (XYPairs != null)
                tags.Add(new AutoDocumentation.GraphAndTable(XYPairs, Name, "Temperature (oC)", Name + " (deg. day)", indent));
        }

    }
}
   
