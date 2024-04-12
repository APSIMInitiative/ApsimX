using Models.Core;
using Models.GrazPlan;
using System;

namespace Models.Grazplan
{
    /// <summary>
    /// Green initialisation helper for GUI
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    public class GreenCohortInitialise : DryCohortInitialise
    {
        /// <summary>Root weight </summary>
        [Description("Root weight (kg/ha)")]
        public double[] RootWeight { get; set; } = new double[] { 400, 400 };

        /// <summary>Rooot depth (mm)</summary>
        [Description("Root depth (mm)")]
        public double RootDepth { get; set; } = 650;

        /// <summary>Establishment index. 0,1.0-KZ1. The index follows the ratio of the current shoot mass of the cohort
        /// to the shoot mass of its plants at germination – a surrogate for relative plant size.</summary>
        [Description("Seedling establishment index")]
        public double EstIndex { get; set; } = 0.0;

        /// <summary>Seedling stress index</summary>
        [Description("Seedling stress index (0-1)")]
        public double StressIndex { get; set; } = 0.0;

        /// <summary>Maximum amount of stem tissue to be relocated to seed.</summary>
        [Description("Stem relocation kg/ha")]
        public double StemReloc { get; set; } = 0.0;

        /// <summary> Number of frosts experienced by this herbage cohort during its lifetime</summary>
        [Description("Number of frosts")]
        public int Frosts { get; set; } = 0;
    }

    /// <summary>
    /// Dry initialisation helper for GUI
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    public class DryCohortInitialise : Model
    {
        /// <summary>Leaf dry matter digestibility (0-1)</summary>
        [Description("Status (type) of cohort")]
        public string Status { get; set; } = "established";

        /// <summary>Leaf dry matter digestibility (0-1)</summary>
        [Description("Leaf dry matter digestibility (0-1)")]
        public double[] LeafDMD { get; set; } = new double[] { 0.825 };

        /// <summary>Leaf weight (kg/ha)</summary>
        [Description("Leaf weight (kg/ha)")]
        public double[] LeafWeight { get; set; } = new double[] { 800.0 };

        /// <summary>Leaf nitrogen concentration (g/g)</summary>
        [Description("Leaf nitrogen concentration (g/g)")]
        public double[] LeafNConc { get; set; } = new double[] { 0.01 };

        /// <summary>Leaf specific area (cm^2/g)</summary>
        [Description("Leaf specific area (cm^2/g)")]
        public double[] LeafSpecificArea { get; set; } = new double[] { 430.0 };

        /// <summary>Stem dry matter digestibility (0-1)</summary>
        [Description("Stem dry matter digestibility (0-1)")]
        public double[] StemDMD { get; set; } = new double[] { 0.825 };

        /// <summary>Stem weight (kg/ha)</summary>
        [Description("Stem weight (kg/ha)")]
        public double[] StemWeight { get; set; } = new double[] { 800.0 };

        /// <summary>Stem nitrogen concentration (g/g)</summary>
        [Description("Stem nitrogen concentration (g/g)")]
        public double[] StemNConc { get; set; } = new double[] { 0.01 };

        /// <summary>Stem specific area (cm^2/g)</summary>
        [Description("Stem specific area (cm^2/g)")]
        public double[] StemSpecificArea { get; set; } = new double[] { 10.0 };
    }

    /// <summary>
    /// Mass of seeds in each soil layer
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    public class SeedCohortInitialise : Model
    {
        /// <summary>
        /// Mass of soft, unripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        [Description("Mass of soft, unripe seeds (kg/ha")]
        public double[] SoftUnripe { get; set; }    // kg/ha

        /// <summary>
        /// Mass of hard, unripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        [Description("Mass of hard, unripe seeds (kg/ha")]
        public double[] HardUnripe { get; set; }    // kg/ha

        /// <summary>
        /// Mass of hard, ripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        [Description("Mass of hard, ripe seeds (kg/ha")]
        public double[] HardRipe { get; set; }      // kg/ha

        /// <summary>
        /// Mass of soft, ripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        [Description("Mass of soft, ripe seeds (kg/ha")]
        public double[] SoftRipe { get; set; }      // kg/ha
    }
}
