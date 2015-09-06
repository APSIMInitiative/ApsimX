using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.Functions;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase simulates time to emergence as a function of sowing depth.  Thermal time target from sowing to emergence = SowingDepth (set with sow()
    /// method called from the manager)  x ShootRate + ShootLag.
    /// </summary>
    /// \pre A \ref Models.PMF.Plant "Plant" function has to exist to 
    /// provide the sowing depth (\f$D_{seed}\f$).
    /// \param ShootLag An initial period of fixed thermal time during 
    /// which shoot elongation is slow (the "lag" phase, \f$T_{lag}\f$, deg;Cd)
    /// \param ShootRate The rate of shoot elongation (\f$r_{e}\f$, 
    /// deg;Cd mm<sup>-1</sup>) towards the soil surface is 
    /// linearly related to air temperature.
    /// <remarks>
    /// The thermal time target in the emerging phase includes 
    /// an effect of the depth of sowing (\f$D_{seed}\f$), an 
    /// initial period of fixed thermal time during which 
    /// shoot elongation is slow (the "lag" phase, \f$T_{lag}\f$, \p ShootLag)
    /// and a linear period, where the rate of shoot elongation (\f$r_{e}\f$, 
    /// \p ShootRate) towards the soil surface is linearly related to 
    /// air temperature. Then, the thermal time target (\f$T_{emer}\f$) 
    /// is calculated by 
    /// \f[
    /// T_{emer}=T_{lag}+r_{e}D_{seed}
    /// \f]
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class EmergingPhase : GenericPhase
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>Gets or sets the shoot lag.</summary>
        /// <value>The shoot lag.</value>
        [Units("oCd")]
       // [XmlIgnore]
        [Description("ShootLag")]
        public double ShootLag { get; set; }
        /// <summary>Gets or sets the shoot rate.</summary>
        /// <value>The shoot rate</value>
        [Units("oCd/mm")]
       // [XmlIgnore]
        [Description("ShootRate")]
        public double ShootRate { get; set; }

        /// <summary>Return the target to caller. Can be overridden by derived classes.</summary>
        /// <returns></returns>
        public override double CalcTarget()
        {
            double retVAl = 0;
            if (Plant != null)
                retVAl = ShootLag + Plant.SowingData.Depth * ShootRate;
            return retVAl;
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

            tags.Add(new AutoDocumentation.Paragraph("This phase simulates time to emergence as a function of sowing depth."
                + " Thermal time target from sowing to emergence is given by:<br>"
                + "&nbsp;&nbsp;&nbsp;&nbsp;***SowingDepth x ShootRate + ShootLag***<br>"
                + "Where:<br>"
                + "&nbsp;&nbsp;&nbsp;&nbsp;***ShootRate*** = " + ShootRate + ",<br>"
                + "&nbsp;&nbsp;&nbsp;&nbsp;***ShootLag*** = " + ShootLag + ", <br>"
                + "and ***SowingDepth*** is sent from the manager with the sowing event.", indent));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            tags.Add(new AutoDocumentation.Paragraph("Progress toward emergence is driven by Thermal time accumulation where thermal time is calculated as:", indent));
            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                child.Document(tags, -1, indent);
        }
    }

    /// <summary>
    /// The class below is for Plant15. Need to get rid of this eventually.
    /// </summary>
    [Serializable]
    public class EmergingPhase15 : GenericPhase
    {
        /// <summary>The plant</summary>
        [Link]
        Models.PMF.OldPlant.Plant15 Plant = null;

        /// <summary>Gets or sets the shoot lag.</summary>
        /// <value>The shoot lag.</value>
        public double ShootLag { get; set; }
        /// <summary>Gets or sets the shoot rate.</summary>
        /// <value>The shoot rate.</value>
        public double ShootRate { get; set; }

        /// <summary>Return the target to caller. Can be overridden by derived classes.</summary>
        /// <returns></returns>
        public override double CalcTarget()
        {
            return ShootLag + Plant.SowingData.Depth * ShootRate;
        }

    }
}