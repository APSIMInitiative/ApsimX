using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer sequence
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ActivityTimerCropHarvest))]
    [ValidParent(ParentType = typeof(ActivityTimerCalendar))]
    [Description("This component adds a timer sequence to a parent timer")]
    [HelpUri(@"Content/Features/Timers/Sequence.htm")]
    [Version(1, 0, 1, "")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class ActivityTimerSequence : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// A defined sequence of true-false to determine when timers are used
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute("1")]
        [Description("Timer sequence (01x20 = false, true, true, false repeated)")]
        [Required]
        public string Sequence { get; set; }

        private string sequence;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerSequence()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
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
        public static string FormatSequence(string sequence)
        {
            // adjust sequence to standard format of non-separated 0's and 1's  
            string seq = sequence.ToLower();
            seq = seq.Replace("y", "1");
            seq = seq.Replace("n", "0");
            seq = seq.Replace("t", "1");
            seq = seq.Replace("f", "0");

            var seqArray = seq.Split(new char[] { ' ', ';', ',', ':', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < seqArray.Length; i++)
            {
                if (seqArray[i].Contains('x'))
                {
                    var parts = seqArray[i].Split('x');
                    seqArray[i] = "";
                    for (int j = 0; j < Convert.ToInt32(parts[1]); j++)
                    {
                        seqArray[i] += parts[0];
                    }
                }
            }

            seq = String.Join("", seqArray);

            // Check if seq contains only '0' and '1'  
            if (seq.Any(c => c != '0' && c != '1'))
            {
                throw new Exception($"Invalid sequence of characters supplied {sequence}. Expecting only 0's and 1's after formatting.");
            }

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

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check validity of sequence
            if (FormatSequence(sequence).Replace("0", "").Replace("1", "").Trim() != "")
            {
                yield return new ValidationResult($"Invalid sequence of characters supplied {sequence}, expecitng 1/0, T/F, or Y/N list of characters delimted by '' - , or : to represent sequence", new string[] { "ActivityTimerSequence" });
            }
        }
        #endregion

        /// <summary>
        /// Method to determine if an index is ok within a list of timer sequences
        /// </summary>
        /// <param name="timerSequences">List of sequences to consider</param>
        /// <param name="index">Index in sequence </param>
        /// <param name="reverseDirection">Work backward through sequence switch</param>
        /// <returns>Boolean indicating if ok in all sequences</returns>
        public static bool IsInSequence(IEnumerable<ActivityTimerSequence> timerSequences, int? index, bool reverseDirection = false)
        {
            if (timerSequences.Any())
            {
                foreach (var seq in timerSequences)
                {
                    int seqIndex = index ?? 0;
                    string sequence = seq.Sequence;
                    if (reverseDirection)
                    {
                        char[] array = sequence.ToCharArray();
                        Array.Reverse(array);
                        sequence = new String(array);
                    }
                    if (seqIndex >= seq.Sequence.Length)
                    {
                        // recalculate index
                        seqIndex = seqIndex - Convert.ToInt32(Math.Floor(seqIndex / (double)seq.Sequence.Length) * seq.Sequence.Length);
                    }
                    if (sequence.Substring(seqIndex, 1) == "0")
                        return false;
                }
                return (index != null);
            }
            return true;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
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
                    htmlWriter.Write($" <span class=\"filterset\">{(seqString[i] == '1' ? "OK" : "SKIP")} </span>");
                }
            }
            htmlWriter.Write("\r\n</div>");
            if (!Enabled & !FormatForParentControl)
            {
                htmlWriter.Write(" - DISABLED!");
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return "";
        }
        #endregion
    }
}
