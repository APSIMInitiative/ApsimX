namespace Models.Core.ConfigFile
{
    /// <summary>
    /// Used to manage files for the models --apply switch.
    /// </summary>
    public class ApplyRunManager
    {
        /// <summary>The original files' path.</summary>
        public string OriginalFilePath { get; set; }

        /// <summary>A stored save path. This is set when a save command is encountered.</summary>
        public string SavePath { get; set; }

        /// <summary>A stored load path. This is set when a load command is encountered.</summary>
        public string LoadPath { get; set; }

        /// <summary>A stored temporary Simulations object.</summary>
        public Simulations TempSim { get; set; }

        /// <summary>Is the Simulations object going to be run?</summary>
        public bool IsSimToBeRun { get; set; }

        /// <summary> A stored last save file path. Used to know what file was last saved.</summary>
        public string LastSaveFilePath { get; set; }

        /// <summary>
        /// Creates a new ApplyRunManager
        /// </summary>
        public ApplyRunManager() { }
    }
}

