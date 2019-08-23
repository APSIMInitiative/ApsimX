using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// The component is used to store details to convert units of a resource type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStoreType))]
    [ValidParent(ParentType = typeof(EquipmentType))]
    [ValidParent(ParentType = typeof(GrazeFoodStoreType))]
    [ValidParent(ParentType = typeof(GreenhouseGasesType))]
    [ValidParent(ParentType = typeof(HumanFoodStoreType))]
    [ValidParent(ParentType = typeof(LandType))]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [ValidParent(ParentType = typeof(ProductStoreType))]
    [ValidParent(ParentType = typeof(ProductStoreTypeManure))]
    [ValidParent(ParentType = typeof(WaterType))]
    [Description("Provides details to convert resource type to different units")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/UnitsConverter.htm")]

    public class ResourceUnitsConverter: CLEMModel
    {
        /// <summary>
        /// Conversion factor
        /// </summary>
        [Description("Conversion factor")]
        public double Factor { get; set; }

        /// <summary>
        /// Units of converted resource
        /// </summary>
        [Description("Units")]
        public string Units { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceUnitsConverter()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            //html += "\n<div class=\"activityentry\">Converts <span class=\"resourcelink\">" + this.Parent.Name+"</span> by a factor of ";
            //if (Factor <= 0)
            //{
            //    html += "<span class=\"errorlink\">[VALUE NOT SET]</span>";
            //}
            //else
            //{
            //    html += "<span class=\"setvalue\">" + Factor.ToString("0.#####") + "</span>";
            //}
            //html += "</div>";

            html += "\n<div class=\"activityentry\">1 ";
            if ((Parent as IResourceType).Units != null)
            {
                html += " " + (Parent as IResourceType).Units + " ";
            }
            else
            {
                html += "<span class=\"errorlink\">[UNITS NOT SET]</span>";
            }

            html += "<span class=\"resourcelink\">" + this.Parent.Name + "</span> ";
            html += "= ";

            if (this.Factor != 0)
            {
                html += " <span class=\"setvalue\">" + this.Factor.ToString("#,##0.##") + "</span> ";
            }
            else
            {
                html += "<span class=\"errorlink\">[FACTOR NOT SET]</span>";
            }
            html += " ";
            if (this.Units != null)
            {
                html += " <span class=\"setvalue\">" + this.Units + "</span> ";
            }
            else
            {
                html += "<span class=\"errorlink\">[UNITS NOT SET]</span>";
            }
            html += "</div>";
            return html;
        }

    }
}
