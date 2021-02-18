using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
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

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">1 ");
                if ((Parent as IResourceType).Units != null)
                {
                    htmlWriter.Write(" " + (Parent as IResourceType).Units + " ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">[UNITS NOT SET]</span>");
                }

                htmlWriter.Write("<span class=\"resourcelink\">" + this.Parent.Name + "</span> ");
                htmlWriter.Write("= ");

                if (this.Factor != 0)
                {
                    htmlWriter.Write(" <span class=\"setvalue\">" + this.Factor.ToString("#,##0.##") + "</span> ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">[FACTOR NOT SET]</span>");
                }
                htmlWriter.Write(" ");
                if (this.Units != null)
                {
                    htmlWriter.Write(" <span class=\"setvalue\">" + this.Units + "</span> ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">[UNITS NOT SET]</span>");
                }
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}
