using System;

namespace Models.GrazPlan
{

    /// <summary>
    ///  Abbreviated animal initialisation set, used in TStockList.Buy                
    /// </summary>
    [Serializable]
    public struct PurchaseInfo
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string Genotype;

        /// <summary>
        /// Number of animals
        /// </summary>
        public int Number;

        /// <summary>
        /// Live weight
        /// </summary>
        public double LiveWt;

        /// <summary>
        /// Greasy fleece weight
        /// </summary>
        public double GFW;

        /// <summary>
        /// Age in days
        /// </summary>
        public int AgeDays;

        /// <summary>
        /// Condition score
        /// </summary>
        public double CondScore;

        /// <summary>
        /// Reproduction status
        /// </summary>
        public GrazType.ReproType Repro;

        /// <summary>
        /// Mated to animal
        /// </summary>
        public string MatedTo;

        /// <summary>
        /// Pregnant days
        /// </summary>
        public int Preg;

        /// <summary>
        /// Lactation days
        /// </summary>
        public int Lact;

        /// <summary>
        /// Number of young
        /// </summary>
        public int NYoung;

        /// <summary>
        /// Weight of young
        /// </summary>
        public double YoungWt;

        /// <summary>
        /// Greasy fleece weight of young
        /// </summary>
        public double YoungGFW;
    }
}
