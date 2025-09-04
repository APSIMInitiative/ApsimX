using APSIM.Shared.Utilities;
using ISO3166;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Nutrients;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UserInterface.Commands;
using UserInterface.Views;
using Utility;
using APSIM.Numerics;
using APSIM.Core;

namespace UserInterface.Presenters
{
    public sealed class SoilDownloadPresenter : IPresenter
    {
        /// <summary>
        /// URI for accessing the Google geocoding API. I don't recall exactly who owns this key!
        /// </summary>
        private static string googleGeocodingApi = "https://maps.googleapis.com/maps/api/geocode/json?key=AIzaSyA4QRojYT4wqhZMiXrFklkWwC_pkg4qJJ8&";

        /// <summary>The view.</summary>
        private ViewBase view;

        /// <summary>The model selected in the tree.</summary>
        private IModel model;

        /// <summary>The list view control.</summary>
        private ListView dataView;

        /// <summary>The refresh button.</summary>
        private EditView latitudeEditBox;

        /// <summary>The download button.</summary>
        private EditView longitudeEditBox;

        /// <summary>The country dropdown.</summary>
        private DropDownView countryDropDown;

        /// <summary>The stop button.</summary>
        private EditView placeNameEditBox;

        /// <summary>The delete button.</summary>
        private EditView radiusEditBox;

        /// <summary>Search button.</summary>
        private ButtonView searchButton;

        /// <summary>Add soil to simulation button.</summary>
        private ButtonView addSoilButton;

        /// <summary>The main presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>List of all countries.</summary>
        private Country[] countries;

        /// <summary>The label for soil count found</summary>
        private LabelView labelCount;

        /// <summary>All found soils.</summary>
        private List<SoilFromDataSource> allSoils= new List<SoilFromDataSource>();

        /// <summary>The token used for cancelling the download</summary>
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Attach the view to this presenter.
        /// </summary>
        /// <param name="zoneModel"></param>
        /// <param name="viewBase"></param>
        /// <param name="explorerPresent"></param>
        public void Attach(object zoneModel, object viewBase, ExplorerPresenter explorerPresent)
        {
            view = (ViewBase)viewBase;
            model = (IModel)zoneModel;
            explorerPresenter = explorerPresent;

            dataView = view.GetControl<ListView>("dataListView");
            latitudeEditBox = view.GetControl<EditView>("latitudeEditBox");
            longitudeEditBox = view.GetControl<EditView>("longitudeEditBox");
            countryDropDown = view.GetControl<DropDownView>("countryDropDown");
            placeNameEditBox = view.GetControl<EditView>("placeNameEditBox");
            radiusEditBox = view.GetControl<EditView>("radiusEditBox");
            searchButton = view.GetControl<ButtonView>("searchButton");
            addSoilButton = view.GetControl<ButtonView>("addSoilButton");
            labelCount = view.GetControl<LabelView>("labelCount");

            PopulateView();

            searchButton.Clicked += OnSearchClicked;
            addSoilButton.Clicked += OnAddSoilButtonClicked;
        }

        /// <summary>Detach the view from this presenter.</summary>
        public void Detach()
        {
            if (countryDropDown.SelectedValue != string.Empty)
            {
                Configuration.Settings.DownloadFromDataSourceCountry = countryDropDown.SelectedValue;
                Configuration.Settings.Save();
            }

            searchButton.Clicked -= OnSearchClicked;
            addSoilButton.Clicked -= OnAddSoilButtonClicked;
            view.Dispose();
        }

        /// <summary>Populate the controls.</summary>
        private void PopulateView()
        {
            var weatherModel = model.Node.Find<IWeather>();
            if (weatherModel != null)
            {
                latitudeEditBox.Text = weatherModel.Latitude.ToString();
                longitudeEditBox.Text = weatherModel.Longitude.ToString();
            }

            if(weatherModel == null)
            {
                this.explorerPresenter.MainPresenter.
                    ShowMessage("Tip: To have the latitude and longitude fields auto-filled add a weather node to your simulation.",
                    Simulation.MessageType.Warning,
                    true);
            }
            radiusEditBox.Text = "10";

            dataView.SortColumn = "Distance (km)";
            dataView.SortAscending = true;

            countries = ISO3166.Country.List;

            var countryNames = countries.Select(country => country.Name).ToList();
            countryNames.Insert(0, string.Empty);
            countryDropDown.Values = countryNames.ToArray();

            if (countryNames.Contains(Configuration.Settings.DownloadFromDataSourceCountry))
                countryDropDown.SelectedValue = Configuration.Settings.DownloadFromDataSourceCountry;
            else
                countryDropDown.SelectedValue = string.Empty;
        }

        /// <summary>User has clicked the search button.</summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private async void OnSearchClicked(object sender, EventArgs e)
        {
            if (searchButton.Text == "Stop")
            {
                cancellationTokenSource.Cancel();
                searchButton.Text = "Search for soils";
                explorerPresenter.MainPresenter.ShowWaitCursor(false);
                await Task.Delay(200);
            }
            else
            {
                searchButton.Text = "Stop";
                labelCount.Text = "";
                enableControls(false);
                try
                {
                    explorerPresenter.MainPresenter.ShowWaitCursor(true);
                    try
                    {
                        if (!string.IsNullOrEmpty(placeNameEditBox.Text) && !string.IsNullOrEmpty(countryDropDown.SelectedValue))
                        {
                            if (await GetLatLongFromPlaceNameAsync() == false)
                                throw new Exception("Cannot find a latitude/longitude from the specified place name.");
                        }

                        if (string.IsNullOrEmpty(latitudeEditBox.Text) || string.IsNullOrEmpty(longitudeEditBox.Text))
                            throw new Exception("Must specifiy either a place name or a latitude/longitude.");

                        // Update the place name edit box.
                        string fullName = await GetPlacenameFromLatLongAsync();

                        //remove the address at start of the name "This business is closed, Dalby"
                        if (fullName != null)
                        {
                            if (fullName.Contains(','))
                                fullName = fullName.Substring(fullName.IndexOf(',') + 2);
                            placeNameEditBox.Text = fullName;
                        }

                        // Use this to monitor task progress
                        Progress<ProgressReportModel> progress = new Progress<ProgressReportModel>();
                        progress.ProgressChanged += ReportProgress;
                        // create a report object here because I will use the same one for all parallel tasks
                        ProgressReportModel report = new ProgressReportModel();
                        report.Count = 0;

                        List<Task<IEnumerable<SoilFromDataSource>>> tasks = new List<Task<IEnumerable<SoilFromDataSource>>>();
                        tasks.Add(GetApsoilSoilsAsync(progress, report));

                        tasks.Add(GetWorldModellersSoilsAsync(progress, report));
                        //tasks.Add(GetISRICSoilsAsync()); // Web API no longer operational?

                        DataTable soilData = new DataTable();
                        soilData.Columns.Add("Name", typeof(string));
                        soilData.Columns.Add("Data source", typeof(string));
                        soilData.Columns.Add("Soil type", typeof(string));
                        soilData.Columns.Add("Distance (km)", typeof(double));
                        soilData.Columns.Add("PAWC for profile", typeof(double));
                        soilData.Columns.Add("PAWC to 300mm", typeof(double));
                        soilData.Columns.Add("PAWC to 600mm", typeof(double));
                        soilData.Columns.Add("PAWC to 1500mm", typeof(double));
                        dataView.ClearRows();

                        double[] pawcmappingLayerStructure = { 300, 300, 900 };

                        var results = await Task.WhenAll(tasks);

                        allSoils.Clear();
                        foreach (var item in results)
                        {
                            foreach (var soilInfo in item)
                            {
                                allSoils.Add(soilInfo);
                                soilData.Rows.Add(MakeDataRow(soilData, soilInfo));
                            }
                        }
                        soilData.DefaultView.Sort = "Distance (km) ASC";
                        dataView.DataSource = soilData.DefaultView.ToTable();
                        labelCount.Text = soilData.Rows.Count.ToString() + " soils found";
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationTokenSource.Dispose();
                        cancellationTokenSource = new CancellationTokenSource();
                        enableControls(true);
                    }
                }
                catch (Exception err)
                {
                    explorerPresenter.MainPresenter.ShowError(err.Message);
                }
                finally
                {
                    explorerPresenter.MainPresenter.ShowWaitCursor(false);
                    searchButton.Text = "Search for soils";
                    enableControls(true);
                }
            }
        }

        /// <summary>
        /// Switch the editable state of some controls. Used while searching.
        /// </summary>
        /// <param name="enable">State of editable flag</param>
        private void enableControls(bool enable)
        {
            latitudeEditBox.Editable = enable;
            longitudeEditBox.Editable = enable;
            radiusEditBox.Editable = enable;
            placeNameEditBox.Editable = enable;
        }

        /// <summary>
        /// Make a new DataTable row and populate it with the soil information
        /// </summary>
        /// <param name="soilData">The DataTable</param>
        /// <param name="soilInfo">The soil found from the services</param>
        /// <returns>The new DataRow in the DataTable</returns>
        private DataRow MakeDataRow(DataTable soilData, SoilFromDataSource soilInfo)
        {
            double[] pawcmappingLayerStructure = { 300, 300, 900 };

            var soilPhysical = soilInfo.Soil.Node.FindChild<Physical>();
            var row = soilData.NewRow();
            row["Name"] = soilInfo.Soil.Name;
            row["Data source"] = soilInfo.DataSource;
            row["Soil type"] = soilInfo.Soil.SoilType;
            row["Distance (km)"] = MetUtilities.Distance(Convert.ToDouble(latitudeEditBox.Text, System.Globalization.CultureInfo.InvariantCulture),
                                                         Convert.ToDouble(longitudeEditBox.Text, System.Globalization.CultureInfo.InvariantCulture),
                                                         soilInfo.Soil.Latitude,
                                                         soilInfo.Soil.Longitude);

            var pawc = soilPhysical.PAWCmm;
            row["PAWC for profile"] = pawc.Sum();

            var pawcConcentration = MathUtilities.Divide(pawc, soilPhysical.Thickness);
            var mappedPawcConcentration = SoilUtilities.MapConcentration(pawcConcentration, soilPhysical.Thickness, pawcmappingLayerStructure, 0);
            var mappedPawc = MathUtilities.Multiply(mappedPawcConcentration, pawcmappingLayerStructure);
            row["PAWC to 300mm"] = mappedPawc[0];
            row["PAWC to 600mm"] = (mappedPawc[0] + mappedPawc[1]);
            row["PAWC to 1500mm"] = mappedPawc.Sum();

            return row;
        }

        /// <summary>
        /// Used to lock the download count update of the gui
        /// </summary>
        private readonly object countLock = new object();

        /// <summary>
        /// The event handler for the updating of the report object
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The report model object</param>
        private void ReportProgress(object sender, ProgressReportModel e)
        {
            lock (countLock)
            {
                e.Count++;
                labelCount.Text = e.Count.ToString() + " soils found";
            }
        }

        /// <summary>
        /// User has clicked the add soil button.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAddSoilButtonClicked(object sender, EventArgs e)
        {
            foreach (int selectedIndex in dataView.SelectedIndicies)
            {
                var values = dataView.GetRow(selectedIndex);
                var soilName = (string)values[0];
                Soil matchingSoil = Apsim.Clone<Soil>(allSoils.First(s => s.Soil.Name == soilName).Soil);
                matchingSoil.Name = matchingSoil.Name.Trim();
                if (!matchingSoil.Children.Any(c => c is INutrient))
                    matchingSoil.Children.Add(new Nutrient() { ResourceName = "Nutrient" });
                ICommand addSoil = new AddModelCommand(model, matchingSoil, explorerPresenter.GetNodeDescription);
                explorerPresenter.CommandHistory.Add(addSoil);
            }
            explorerPresenter.Populate();
        }

        /// <summary>
        /// Return zero or more APSOIL soils.
        /// </summary>
        /// <param name="progress">The system progress object</param>
        /// <param name="report">The reporting object used for this task</param>
        /// <returns>List of soils</returns>
        private async Task<IEnumerable<SoilFromDataSource>> GetApsoilSoilsAsync(IProgress<ProgressReportModel> progress, ProgressReportModel report)
        {
            var soils = new List<SoilFromDataSource>();
            try
            {
                if(!double.TryParse(latitudeEditBox.Text, out double latitude))
                    throw new Exception("Latitude field has invalid input \"" + radiusEditBox.Text +"\"");

                if(!double.TryParse(longitudeEditBox.Text, out double longitude))
                    throw new Exception("Longitude field has invalid input \"" + radiusEditBox.Text +"\"");

                if(!double.TryParse(radiusEditBox.Text, out double radius))
                    throw new Exception("Radius field has invalid input \"" + radiusEditBox.Text +"\"");

                string url = $"https://apsoil.apsim.info/search?latitude={latitude}&longitude={longitude}&Radius={radius}&output=ExtendedInfo&output=FullSoil&SoilType=";
                using (var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token))
                {
                    stream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "Soil");
                    foreach (XmlNode soilNode in soilNodes)
                    {
                        var soilXML = $"<folder>{soilNode.OuterXml}</folder>";
                        var folder = FileFormat.ReadFromString<Folder>(soilXML).Model as Folder;
                        if (folder.Children.Any())
                        {
                            var soil = folder.Children[0] as Soil;

                            // fixme: this should be handled by the converter or the importer.
                            SoilSanitise.InitialiseSoil(soil);
                            soils.Add(new SoilFromDataSource()
                            {
                                Soil = soil,
                                DataSource = "APSOIL"
                            });
                            progress.Report(report);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }

            return soils;
        }


        /// <summary>
        /// Gets a soil description from the ISRIC REST API for World Modellers
        /// </summary>
        /// <param name="progress">The system progress object</param>
        /// <param name="report">The reporting object used for this task</param>
        /// <returns>True if successful</returns>
        private async Task<IEnumerable<SoilFromDataSource>> GetWorldModellersSoilsAsync(IProgress<ProgressReportModel> progress, ProgressReportModel report)
        {
            var soils = new List<SoilFromDataSource>();

            // Loop through all grid points within a radius of the lat/long user has specified.
            double latitude = Convert.ToDouble(latitudeEditBox.Text, System.Globalization.CultureInfo.InvariantCulture);
            double longitude = Convert.ToDouble(longitudeEditBox.Text, System.Globalization.CultureInfo.InvariantCulture);
            double radius = Convert.ToDouble(radiusEditBox.Text, System.Globalization.CultureInfo.InvariantCulture);

            foreach ((double lat, double lon) point in MathUtilities.GetGridPointsWithinRadius(latitude, longitude, radius,
                                                                                               resolution: 0.25, offset: 0.125))
            {
                string url = "https://worldmodel.csiro.au/apsimsoil?lon=" +
                    point.lon.ToString() + "&lat=" + point.lat.ToString();
                try
                {
                    var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token);
                    stream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "Soil");
                    // We will have either 0 or 1 soil nodes
                    if (soilNodes.Count > 0)
                    {
                        var soilXML = $"<folder>{soilNodes[0].OuterXml}</folder>";
                        var soilFolder = FileFormat.ReadFromString<Folder>(soilXML).Model as Folder;
                        var soil = soilFolder.Children[0] as Soil;
                        SoilSanitise.InitialiseSoil(soil);

                        soils.Add(new SoilFromDataSource()
                        {
                            Soil = soil,
                            DataSource = "ISRIC"
                        });
                        progress.Report(report);
                    }
                }
                catch (OperationCanceledException)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                }
                catch (Exception err)
                {
                    explorerPresenter.MainPresenter.ShowError(err);
                }
            }
            return soils;
        }

        /// <summary>
        /// Get place name from a lat/long.
        /// Uses Google's geocoding service to find the placename for the current latitude and longitude.
        /// </summary>
        private async Task<string> GetPlacenameFromLatLongAsync()
        {
            if (!string.IsNullOrEmpty(latitudeEditBox.Text) && !string.IsNullOrEmpty(longitudeEditBox.Text))
            {
                string url = googleGeocodingApi + "latlng=" + latitudeEditBox.Text + ',' + longitudeEditBox.Text;
                try
                {
                    var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token);
                    stream.Position = 0;
                    JsonTextReader reader = new JsonTextReader(new StreamReader(stream));
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("formatted_address"))
                        {
                            reader.Read();
                            return reader.Value.ToString();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                }
                catch
                { }
            }
            return null;
        }

        /// <summary>
        /// Uses Googles' geocoding service to find the co-ordinates of the specified placename
        /// Currently this displays only the first match. Since there can be multiple matches
        /// (there are a lot of "Black Mountain"s in Australia, for example, it would be better
        /// to present the user with the list of matches when there is more than one.
        /// </summary>
        /// <returns>True if successful</returns>
        private async Task<bool> GetLatLongFromPlaceNameAsync()
        {
            try
            {
                var country = countries.First(c => c.Name == countryDropDown.SelectedValue);
                string url = $"{googleGeocodingApi}components=country:{country.TwoLetterCode}|locality:{placeNameEditBox.Text}";
                using (var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token))
                {
                    stream.Position = 0;
                    using (JsonTextReader reader = new JsonTextReader(new StreamReader(stream)))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("location"))
                            {
                                reader.Read(); // Read the "start object" token
                                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                {
                                    if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("lat"))
                                    {
                                        reader.Read();

                                        // This uses the current culture.
                                        latitudeEditBox.Text = reader.Value.ToString();
                                    }
                                    else if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("lng"))
                                    {
                                        reader.Read();

                                        // This uses the current culture.
                                        longitudeEditBox.Text = reader.Value.ToString();
                                    }
                                }
                                return true;
                            }
                            else if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("status"))
                            {
                                reader.Read();
                                string status = reader.Value.ToString();
                                if (status == "ZERO_RESULTS")
                                    return false;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }
            return false;
        }

        private class SoilFromDataSource
        {
            public string DataSource { get; set; }
            public Soil Soil { get; set; }

        }

        /// <summary>
        /// The object used to report progress to the gui from each download task
        /// </summary>
        private class ProgressReportModel
        {
            public int Count { get; set; } = 0;
            //public List<SoilFromDataSource> Soils { get; set; } = new List<SoilFromDataSource>();
        }

    }
}