namespace Models.Soils.Nutrient
{
    using Interfaces;
    using Models.Core;
    using System;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Soil carbon model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class Nutrient : Model
    {
    }
}
