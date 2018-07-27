using System;

namespace Models.PMF.Phen
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PhaseChangedType : EventArgs
    {
        /// <summary>The old phase name</summary>
        public String OldPhaseName = "";
        /// <summary>The new phase name</summary>
        public String NewPhaseName = "";
        /// <summary>The stage at phase change</summary>
        public String EventStageName = "";
    }
}
