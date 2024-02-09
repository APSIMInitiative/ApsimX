using System;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Data passed with PhaseChanged Event
    /// </summary>
    [Serializable]
    public class PhaseChangedType : EventArgs
    {
        /// <summary>The stage at phase change</summary>
        public String StageName = "";
    }

    /// <summary>
    /// Data passed with StageChanged Event
    /// </summary>
    [Serializable]
    public class StageSetType : EventArgs
    {
        /// <summary>The stage number at phase change</summary>
        public double StageNumber { get; set; }
    }
}
