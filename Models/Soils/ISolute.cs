namespace Models.Soils
{

    /// <summary>The known types of solute setters.</summary>
    public enum SoluteSetterType
    {
        /// <summary>The setting model is a plant model</summary>
        Plant,
        /// <summary>The setting model is a soil model</summary>
        Soil,
        /// <summary>The setting model is a fertiliser model</summary>
        Fertiliser,
        /// <summary>Anything else</summary>
        Other
    }

    /// <summary>This interface defines what a solute can do.</summary>
    public interface ISolute
    {
        /// <summary>Name of solute.</summary>
        string Name { get; }

        /// <summary>Solute amount (kg/ha)</summary>
        double[] kgha { get; set; }

        /// <summary>Solute amount (ppm)</summary>
        double[] ppm { get; }

        /// <summary>Depth constant (mm) used to calculate amount of solute lost in runoff water.</summary>
        public double DepthConstant { get; set; }

        /// <summary>MaxDepthSoluteAccessible (mm) used to calculate amount of solute lost in runoff water.</summary>
        public double MaxDepthSoluteAccessible { get; set; }

        /// <summary>RunoffEffectivenessAtMovingSolute (0-1) used to calculate amount of solute lost in runoff water.</summary>
        public double RunoffEffectivenessAtMovingSolute { get; set; }

        /// <summary>MaxEffectiveRunoff (mm) used to calculate amount of solute lost in runoff water.</summary>
        public double MaxEffectiveRunoff { get; set; }

        /// <summary>Amount of solute in solution.</summary>
        double[] AmountInSolution { get; set; }

        /// <summary>Concentration of solute adsorbed (ug/g soil).</summary>
        double[] ConcAdsorpSolute { get; set; }

        /// <summary>Amount of solute lost in runoff water (kg/ha).</summary>
        double[] AmountLostInRunoff { get; set; }

        /// <summary>Setter for kgha.</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        void SetKgHa(SoluteSetterType callingModelType, double[] value);

        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta);
    }
}