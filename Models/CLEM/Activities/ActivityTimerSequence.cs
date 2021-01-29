using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer sequence
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ActivityTimerCropHarvest))]
    [Description("This component adds a timer sequence to selected timers")]
    [HelpUri(@"Content/Features/Timers/Sequence.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerSequence : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// A defined sequence of true-false to determine when timers are used
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute("1")]
        [Description("Activity sequence (010 = false, true, false, repeated)")]
        [Required]
        public string Sequence { get; set; }

        private string sequence;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerSequence()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // adjust sequence to standard format of 0's and 1's
            sequence = FormatSequence(Sequence);
        }

        /// <summary>
        /// Format sequence provided by user to standard
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns>formatted sequence</returns>
        private string FormatSequence(string sequence)
        {
            // adjust sequence to standard format of non-separated 0's and 1's
            string seq = sequence.ToLower();
            seq = String.Join("", seq.Split(new char[] { ' ', ';', ',', ':', '-' }, StringSplitOptions.RemoveEmptyEntries));
            seq = seq.Replace("y", "1");
            seq = seq.Replace("n", "0");
            seq = seq.Replace("t", "1");
            seq = seq.Replace("f", "0");
            return seq;
        }

        /// <summary>
        /// Determine if the month in sequence is enabled
        /// </summary>
        /// <param name="sequenceMonth"></param>
        /// <returns>Whether timer is enabled</returns>
        public bool TimerOK(int sequenceMonth)
        {
            int index = sequenceMonth - ((sequenceMonth / sequence.Length) * sequence.Length);
            return sequence[index] == '1';
        }

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // check validity of sequence
            if (sequence.Replace("0", "").Replace("1", "").Trim() != "")
            {
                string[] memberNames = new string[] { "ActivityTimerSequence" };
                results.Add(new ValidationResult($"Invalid sequence of characters supplied {sequence}, expecitng 1/0, T/F, or Y/N list of characters delimted by '' - , or : to represent sequence", memberNames));
            }
            return results;
        } 
        #endregion

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
                htmlWriter.Write("\r\n<div class=\"filter\">");
                if (Sequence is null || Sequence == "")
                {
                    htmlWriter.Write($"Sequence <span class=\"errorlink\">NOT SET</span>");
                }
                else
                {
                    htmlWriter.Write("<span style=\"float:left; margin-right:5px;\">Use sequence</span>");
                    string seqString = FormatSequence(Sequence);
                    for (int i = 0; i < seqString.Length; i++)
                    {
                        htmlWriter.Write($" <span class=\"filterset\">{(seqString[i] == '1' ? "OK" : "SKIP")}</span>");
                    }
                }
                htmlWriter.Write("\r\n</div>");
                if (!this.Enabled)
                {
                    htmlWriter.Write(" - DISABLED!");
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        } 
        #endregion
    }
}
