using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>A model for holding layer structure information</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class LayerStructure : Model, IGridTable
    {
        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Summary]
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get => SoilUtilities.ToDepthStrings(Thickness);
            set => Thickness = SoilUtilities.ToThickness(value);
        }

        /// <summary>Gets or sets the thickness.</summary>
        public double[] Thickness { get; set; }

        /// <summary>Tabular data. Called by GUI.</summary>
        public GridTable GetGridTable()
        {
            return new GridTable(Name, new GridTable.Column[]
            {
                new GridTable.Column("Depth", new VariableProperty(this, GetType().GetProperty("Depth")), readOnly:false)
            });
        }
    }
}
