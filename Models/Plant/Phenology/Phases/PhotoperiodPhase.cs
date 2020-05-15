using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>Describe the phenological development through a photoperiod-determined phase.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class PhotoperiodPhase : Model, IPhase, ICustomDocumentation
    {
        [Link(ByName = true)]
        IFunction Photoperiod = null;

        [Link(ByName = true)]
        IFunction PhotoperiodDelta = null;

        /// <summary>Critical photoperiod to move into next phase</summary>
        [Description("Critical photoperiod to move into next phase")]
        public double CricialPhotoperiod { get; set; }

            /// <summary>
            ///  Photoperiod Type
            /// </summary>
        public enum PPType
        {
            /// <summary>
            /// Increasing Photoperiod
            /// </summary>
            Increasing,
            /// <summary>
            /// Decreasing Photoperiod
            /// </summary>
            Decreasing
        }

            /// <summary>Flag to specify whether photoperiod should be increasing</summary>
        [Description("Flag to specify whether photoperiod should be increasing")]
        public PPType PPDirection { get; set; }

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [XmlIgnore]
        public double FractionComplete { get; }
        
        /// <summary>Units of progress through phase on this time step.</summary>
        [XmlIgnore]
        public double ProgressionForTimeStep { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [XmlIgnore]
        public double ProgressThroughPhase { get; set; }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase;

            if (Photoperiod.Value() > CricialPhotoperiod && PhotoperiodDelta.Value() > 0 && PPDirection == PPType.Increasing)
                proceedToNextPhase = true;
            else if (Photoperiod.Value() < CricialPhotoperiod && PhotoperiodDelta.Value() < 0 && PPDirection == PPType.Decreasing)
                proceedToNextPhase = true;
            else
                proceedToNextPhase = false;

            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

                // write description of this class
                tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + 
                    ". The phase ends when photoperiod has a reaches a critical photoperiod with a given direction (Increasing/Decreasing).  "+
                    "The base model uses a critical photoperiod of "+CricialPhotoperiod.ToString()+ " hours ("+PPDirection.ToString()+").", indent));

                // write memos
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }
    }
}

      
      
