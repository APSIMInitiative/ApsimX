using Models.Surface;

namespace Models.Soils.NutrientPatching
{
    /// <summary>Different approaches to use for N partitioning between patches.</summary>
    public enum PartitionApproachEnum
    {
        /// <summary>Based on concentration and delta.</summary>
        BasedOnConcentrationAndDelta,

        /// <summary>Based on concentration only.</summary>
        BasedOnLayerConcentration,

        /// <summary>Based on soil concentration.</summary>
        BasedOnSoilConcentration
    }

    /// <summary>Specifies different types of auto amalgamation of patches.</summary>
    public enum AutoAmalgamationApproachEnum
    {
        /// <summary>No automatic amalgamation.</summary>
        None,

        /// <summary>All patches are compared before they are merged.</summary>
        CompareAll,

        /// <summary>All patches are compared to base first, then merged, then compared again.</summary>
        CompareBase,

        /// <summary>Patches are compared and merged at once if deemed equal, then compared to next.</summary>
        CompareMerge,

        /// <summary></summary>
        CompareAge
    }

    /// <summary>The implemented approaches to use when defining the base patch.</summary>
    public enum BaseApproachEnum
    {
        /// <summary>The patch with lowest ID (=0) is used as the base.</summary>
        IDBased,

        /// <summary>The first patch with the biggest area is used as base.</summary>
        AreaBased
    }

    /// <summary>
    /// This interface defines what a Nutrient Patch Manager does.
    /// </summary>
    public interface INutrientPatchManager
    {
        /// <summary>The maximum amount of N that is made available to plants in one day (kg/ha/day).</summary>
        double MaximumNitrogenAvailableToPlants { get; set; }

        /// <summary>The approach used for partitioning the N between patches.</summary>
        PartitionApproachEnum NPartitionApproach { get; set; }

        /// <summary>Approach to use when comparing patches for AutoAmalagamation.</summary>
        AutoAmalgamationApproachEnum AutoAmalgamationApproach { get; set; }

        /// <summary>Approach to use when defining the base patch.</summary>
        /// <remarks>
        /// This is used to define the patch considered the 'base'. It is only used when comparing patches during
        /// potential auto-amalgamation (comparison against base are more lax)
        /// </remarks>
        BaseApproachEnum basePatchApproach { get; set; }

        /// <summary>Allow force amalgamation of patches based on age?</summary>
        bool AllowPatchAmalgamationByAge { get; set; }

        /// <summary>Age of patch at which merging is enforced (years).</summary>
        double PatchAgeForForcedMerge { get; set; }

        /// <summary>
        /// Add a new patch.
        /// </summary>
        /// <param name="patch">Details of the patch to add.</param>
        void Add(AddSoilCNPatchType patch);
    }
}