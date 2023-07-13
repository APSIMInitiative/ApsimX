using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// Allocation of energy, protein etc for:
    /// </summary>
    [Serializable]
    public struct PhysiologicalState
    {
        /// <summary>
        /// Basal metab.+movement+digestion+cold
        /// </summary>
        public double Maint;

        /// <summary>
        /// Pregnancy
        /// </summary>
        public double Preg;

        /// <summary>
        /// Lactation
        /// </summary>
        public double Lact;

        /// <summary>
        /// Wool growth (sheep only)
        /// </summary>
        public double Wool;

        /// <summary>
        /// Weight gain (after efficiency losses)
        /// </summary>
        public double Gain;

        /// <summary>
        /// Basal metabolism
        /// </summary>
        public double Metab;

        /// <summary>
        /// Heat production in the cold
        /// </summary>
        public double Cold;

        /// <summary>
        /// Total value
        /// </summary>
        public double Total;
    }
}
