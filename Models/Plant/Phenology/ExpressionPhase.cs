using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Functions;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase extends from the end of the previous phase until the final main-stem leaf has finished expansion.  The duration of this phase is determined by leaf appearance rate and the final main stem node number.  
    /// As such, the model parameterisation of leaf appearance and final leaf number (set in the Structure object) are important for predicting the duration of the crop correctly.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ExpressionPhase : GenericPhase
    {
        /// <summary>The structure</summary>
        [Link]
        IFunction ExpressionTarget = null;

        [Link]
        IFunction Expression = null;

        
        /// <summary>Do our timestep development</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            base.DoTimeStep(PropOfDayToUse);

            if (Expression.Value >= ExpressionTarget.Value)
                    return 0.00001;
                else
                    return 0;
        }

        /// <summary>
        /// Target provided as input so no need to calculate
        /// </summary>
        /// <returns></returns>
        public override double CalcTarget()
        {
            return 1;
        }

        /// <summary>Return a fraction of phase complete.</summary>
        /// <value>The fraction complete.</value>
        [XmlIgnore]
        public override double FractionComplete
        {
            get
            {
                return Expression.Value / ExpressionTarget.Value;
            }
            set
            {
                throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
        }
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

            // Describe the start and end stages
            tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + ".  ", indent));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // get description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);
        }

    }
}

      
      
