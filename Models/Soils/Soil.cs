namespace Models.Soils
{
    using Models.Core;
    using Models.Soils.Standardiser;
    using System;

    /// <summary>
    /// The soil class encapsulates a soil characterisation and 0 or more soil samples.
    /// the methods in this class that return double[] always return using the
    /// "Standard layer structure" i.e. the layer structure as defined by the Water child object.
    /// method. Mapping will occur to achieve this if necessary.
    /// To obtain the "raw", unmapped, values use the child classes e.g. SoilWater, Analysis and Sample.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    public class Soil : Model
    {
        /// <summary>Gets or sets the record number.</summary>
        [Summary]
        [Description("Record number")]
        public int RecordNumber { get; set; }

        /// <summary>Gets or sets the asc order.</summary>
        [Summary]
        [Description("Australian Soil Classification Order")]
        public string ASCOrder { get; set; }

        /// <summary>Gets or sets the asc sub order.</summary>
        [Summary]
        [Description("Australian Soil Classification Sub-Order")]
        public string ASCSubOrder { get; set; }

        /// <summary>Gets or sets the type of the soil.</summary>
        [Summary]
        [Description("Soil texture or other descriptor")]
        public string SoilType { get; set; }

        /// <summary>Gets or sets the name of the local.</summary>
        [Summary]
        [Description("Local name")]
        public string LocalName { get; set; }

        /// <summary>Gets or sets the site.</summary>
        [Summary]
        [Description("Site")]
        public string Site { get; set; }

        /// <summary>Gets or sets the nearest town.</summary>
        [Summary]
        [Description("Nearest town")]
        public string NearestTown { get; set; }

        /// <summary>Gets or sets the region.</summary>
        [Summary]
        [Description("Region")]
        public string Region { get; set; }

        /// <summary>Gets or sets the state.</summary>
        [Summary]
        [Description("State")]
        public string State { get; set; }

        /// <summary>Gets or sets the country.</summary>
        [Summary]
        [Description("Country")]
        public string Country { get; set; }

        /// <summary>Gets or sets the natural vegetation.</summary>
        [Summary]
        [Description("Natural vegetation")]
        public string NaturalVegetation { get; set; }

        /// <summary>Gets or sets the apsoil number.</summary>
        [Summary]
        [Description("APSoil number")]
        public string ApsoilNumber { get; set; }

        /// <summary>Gets or sets the latitude.</summary>
        [Summary]
        [Description("Latitude (WGS84)")]
        public double Latitude { get; set; }

        /// <summary>Gets or sets the longitude.</summary>
        [Summary]
        [Description("Longitude (WGS84)")]
        public double Longitude { get; set; }

        /// <summary>Gets or sets the location accuracy.</summary>
        [Summary]
        [Description("Location accuracy")]
        public string LocationAccuracy { get; set; }

        /// <summary>Gets or sets the year of sampling.</summary>
        [Summary]
        [Description("Year of sampling")]
        public string YearOfSampling { get; set; }

        /// <summary>Gets or sets the data source.</summary>
        [Summary]
        [Description("Data source")]
        public string DataSource { get; set; }

        /// <summary>Gets or sets the comments.</summary>
        [Summary]
        [Description("Comments")]
        public string Comments { get; set; }

        /// <summary>Event handler to perform error checks at start of simulation.</summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("DoInitialSummary")]
        private void OnDoInitialSummary(object sender, EventArgs e)
        {
            SoilChecker.Check(this);
        }
    }
}