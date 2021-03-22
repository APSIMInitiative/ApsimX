using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// This model calculates the CO~2~ impact on RUE using the approach of [Reyenga1999].
    /// 
    /// For C3 plants,
    /// 
    ///     _F~CO2~ = (CO~2~ - CP) x (350 + 2 x CP)/(CO~2~ + 2 x CP) x (350 - CP)_
    ///     
    /// where CP, is the compensation point calculated from daily average temperature (T) as
    /// 
    ///     _CP = (163.0 - T) / (5.0 - 0.1 * T)_
    /// 
    /// For C4 plants,
    /// 
    ///     _F<sub>CO~2~</sub> = 0.000143 * CO~2~ + 0.95_
    /// 
    /// </summary>
    [Serializable]
    [Description("This model calculates CO2 Impact on RUE using the approach of <br>Reyenga, Howden, Meinke, Mckeon (1999) <br>Modelling global change impact on wheat cropping in south-east Queensland, Australia. <br>Enivironmental Modelling && Software 14:297-306")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class RUECO2Function : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The photosynthetic pathway</summary>
        [Description("PhotosyntheticPathway")]
        public String PhotosyntheticPathway { get; set; }


        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Average daily temperature too high for RUE CO2 Function
        /// or
        /// CO2 concentration too low for RUE CO2 Function
        /// or
        /// Unknown photosynthetic pathway in RUECO2Function
        /// </exception>
        public double Value(int arrayIndex = -1)
        {
            if (PhotosyntheticPathway == "C3")
            {

                double temp = (MetData.MaxT + MetData.MinT) / 2.0; // Average temperature


                if (temp >= 50.0)
                    throw new Exception("Average daily temperature too high for RUE CO2 Function");

                if (MetData.CO2 < 300)
                    throw new Exception("CO2 concentration too low for RUE CO2 Function");
                else if (MetData.CO2 == 350)
                    return 1.0;
                else
                {
                    double CP;      //co2 compensation point (ppm)
                    double first;
                    double second;

                    CP = (163.0 - temp) / (5.0 - 0.1 * temp);

                    first = (MetData.CO2 - CP) * (350.0 + 2.0 * CP);
                    second = (MetData.CO2 + 2.0 * CP) * (350.0 - CP);
                    return first / second;
                }
            }
            else if (PhotosyntheticPathway == "C4")
            {
                return 0.000143 * MetData.CO2 + 0.95; //Mark Howden, personal communication
            }
            else
                throw new Exception("Unknown photosynthetic pathway in RUECO2Function");
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // get description of this class
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children.
                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}