using System;
using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Water factor for daily soil organic matter mineralisation</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Mineralisation Water Factor from CERES-Maize")]
    public class CERESMineralisationWaterFactor : Model, IFunction, ICustomDocumentation
    {

        [Link]
        Soil soil = null;

        [Link]
        ISoilWater soilwater = null;

        [Link]
        IPhysical physical = null;

        /// <summary>Boolean to indicate sandy soil</summary>
        private bool isSand = false;

        /// <summary>
        /// Handler method for the start of simulation event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (soil.SoilType != null)
                if (soil.SoilType.ToLower() == "sand")
                    isSand = true;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");

            double[] SW = soilwater.SW;
            double[] LL15 = physical.LL15;
            double[] DUL = physical.DUL;
            double[] SAT = physical.SAT;

            double WF = 0;
            if (SW[arrayIndex] < LL15[arrayIndex])
                WF = 0;
            else if (SW[arrayIndex] < DUL[arrayIndex])
                    if (isSand)
                        WF = 0.05+0.95*Math.Min(1, 2 * MathUtilities.Divide(SW[arrayIndex] - LL15[arrayIndex], DUL[arrayIndex] - LL15[arrayIndex],0.0));
                    else
                        WF = Math.Min(1, 2 * MathUtilities.Divide(SW[arrayIndex] - LL15[arrayIndex], DUL[arrayIndex] - LL15[arrayIndex],0.0));
            else
                WF = 1 - 0.5 * MathUtilities.Divide(SW[arrayIndex] - DUL[arrayIndex], SAT[arrayIndex] - DUL[arrayIndex],0.0);

            return WF;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {

            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));


            // write memos.
            foreach (IModel memo in this.FindAllChildren<Memo>())
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);


        }
    }
}