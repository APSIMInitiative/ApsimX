using APSIM.Shared.Utilities;
using UserInterface.Extensions;
using ISO3166;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Nutrients;
using Models.WaterModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UserInterface.Commands;
using UserInterface.Views;
using Utility;
using System.Web;
using System.Text;
using Gtk;

namespace UserInterface.Presenters
{
    public sealed class SoilDownloadPresenter : IPresenter
    {
        /// <summary>
        /// URI for accessing the Google geocoding API. I don't recall exactly who owns this key!
        /// </summary>
        private static string googleGeocodingApi = "https://maps.googleapis.com/maps/api/geocode/json?key=AIzaSyC6OF6s7DwSHwibtQqAKC9GtOQEwTkCpkw&";

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
            var weatherModel = model.FindInScope<IWeather>();
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
                        tasks.Add(GetASRISSoilsAsync(progress, report));
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
                        dataView.DataSource = soilData;
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

            var soilPhysical = soilInfo.Soil.FindChild<Physical>();
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
                
                string url = $"http://apsimdev.apsim.info/ApsoilWebService/Service.asmx/SearchSoilsReturnInfo?latitude={latitude}&longitude={longitude}&radius={radius}&SoilType=";
                using (var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token))
                {
                    stream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "SoilInfo");
                    foreach (XmlNode node in soilNodes)
                    {
                        string name = node["Name"].InnerText;
                        string infoUrl = $"https://apsimdev.apsim.info/ApsoilWebService/Service.asmx/SoilXML?Name={name}";
                        using (var infoStream = await WebUtilities.ExtractDataFromURL(infoUrl, cancellationTokenSource.Token))
                        {
                            infoStream.Position = 0;
                            string xml = HttpUtility.HtmlDecode(Encoding.UTF8.GetString(infoStream.ToArray()));
                            XmlDocument soilDoc = new XmlDocument();
                            soilDoc.LoadXml(xml);
                            foreach (XmlNode soilNode in XmlUtilities.ChildNodesRecursively(soilDoc, "Soil"))
                            {
                                var soilXML = $"<folder>{soilNode.OuterXml}</folder>";
                                var folder = FileFormat.ReadFromString<Folder>(soilXML, e => throw e, false).NewModel as Folder;
                                if (folder.Children.Any())
                                {
                                    var soil = folder.Children[0] as Soil;

                                    // fixme: this should be handled by the converter or the importer.
                                    InitialiseSoil(soil);
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
        /// Requests a "synthethic" Soil and Landscape grid soil from the ASRIS web service.
        /// </summary>
        /// <param name="progress">The system progress object</param>
        /// <param name="report">The reporting object used for this task</param>
        /// <returns></returns>
        private async Task<IEnumerable<SoilFromDataSource>> GetASRISSoilsAsync(IProgress<ProgressReportModel> progress, ProgressReportModel report)
        {
            var soils = new List<SoilFromDataSource>();
            string url = "https://www.asris.csiro.au/ASRISApi/api/APSIM/getApsoil?longitude=" +
                longitudeEditBox.Text + "&latitude=" + latitudeEditBox.Text;
            try
            {
                var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token);
                stream.Position = 0;
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);
                List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "soil");
                // We will have either 0 or 1 soil nodes
                if (soilNodes.Count > 0)
                {
                    var soil = FileFormat.ReadFromString<Soil>(soilNodes[0].OuterXml, e => throw e, false).NewModel as Soil;
                    soil.OnCreated();
                    InitialiseSoil(soil);
                    soils.Add(new SoilFromDataSource()
                    {
                        Soil = soil,
                        DataSource = "SLGA"
                    });
                    progress.Report(report);
                }
            }
            catch (OperationCanceledException)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }
            catch (Exception error)
            {
                explorerPresenter.MainPresenter.ShowError(error);
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
            string url = "https://worldmodel.csiro.au/apsimsoil?lon=" +
                longitudeEditBox.Text + "&lat=" + latitudeEditBox.Text;
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
                    var soilFolder = FileFormat.ReadFromString<Folder>(soilXML, e => throw e, false).NewModel as Folder;
                    var soil = soilFolder.Children[0] as Soil;
                    InitialiseSoil(soil);

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
            return soils;
        }

        /// <summary>
        /// Initialise soil and add in missing children.
        /// </summary>
        /// <param name="soil"></param>
        private static void InitialiseSoil(Soil soil)
        {
            var temperature = soil.FindChild<CERESSoilTemperature>();
            if (temperature == null)
                soil.Children.Add(new CERESSoilTemperature() {Name = "Temperature"});
            else
                temperature.Name = "Temperature";

            var physical = soil.FindChild<Physical>();
            if (physical != null)
            {
                if (soil.FindChild<Solute>("NO3") == null)
                    soil.Children.Add(new Solute()
                    {
                        Name = "NO3",
                        Thickness = physical.Thickness,
                        InitialValues = MathUtilities.CreateArrayOfValues(1.0, physical.Thickness.Length)
                    });
                if (soil.FindChild<Solute>("NH4") == null)
                    soil.Children.Add(new Solute()
                    {
                        Name = "NH4",
                        Thickness = physical.Thickness,
                        InitialValues = MathUtilities.CreateArrayOfValues(0.1, physical.Thickness.Length)
                    });
                if (soil.FindChild<Solute>("Urea") == null)
                    soil.Children.Add(new Solute()
                    {
                        Name = "Urea",
                        Thickness = physical.Thickness,
                        InitialValues = MathUtilities.CreateArrayOfValues(0, physical.Thickness.Length)
                    });
                var water = soil.FindChild<Water>();
                if (water != null && water.Thickness == null)
                {
                    water.Thickness = physical.Thickness;
                    water.InitialValues = physical.DUL;
                }
                if (water != null && water.InitialValues == null)
                {
                    water.InitialValues = physical.DUL;
                }
                var euc = physical.FindChild<SoilCrop>("EucalyptusSoil");
                var pinus = physical.FindChild<SoilCrop>("PinusSoil");
                if (euc != null && pinus == null)
                {
                    pinus = euc.Clone();
                    pinus.Name = "PinusSoil";
                    physical.Children.Add(pinus);
                }
                var scrum = physical.FindChild<SoilCrop>("SCRUMSoil");
                var firstSoilCrop = physical.FindChild<SoilCrop>();
                if (scrum == null && firstSoilCrop != null)
                {
                    scrum = firstSoilCrop.Clone();
                    scrum.Name = "SCRUMSoil";
                    physical.Children.Add(scrum);
                }
            }
            soil.OnCreated();
        }

        /// This alternative approach for obtaining ISRIC soil data need a little bit more work, but is largely complete
        /// There are still bits of the soil organic matter initialisation that should be enhanced.
        /// We probably don't really need two different ways to get to ISRIC data, but it may be interesting to see how the 
        /// two compare. The initial motiviation was what appears to be an order-of-magnitude problem with soil carbon
        /// in the World Modellers version.
        /// <summary>
        /// Gets and ISRIC soil description directly from SoilGrids
        /// </summary>
        /// <returns>A list of downloaded soils</returns>
        private async Task<IEnumerable<SoilFromDataSource>> GetISRICSoilsAsync()
        {
            var soils = new List<SoilFromDataSource>();
            string url = "https://rest.soilgrids.org/query?lon=" +
                longitudeEditBox.Text + "&lat=" + latitudeEditBox.Text;
            try
            {
                double[] bd = new double[7];
                double[] coarse = new double[7];
                double[] clay = new double[7];
                double[] silt = new double[7];
                double[] sand = new double[7];
                double[] thetaSat = new double[7];
                double[] awc20 = new double[7];
                double[] awc23 = new double[7];
                double[] awc25 = new double[7];
                double[] thetaWwp = new double[7];
                double[] ocdrc = new double[7];
                double[] phWater = new double[7];
                double[] cationEC = new double[7];
                double[] texture = new double[7];
                string soilType = String.Empty;
                double maxTemp = 0.0;
                double minTemp = 0.0;
                double ppt = 0.0;
                double bedrock = 2500.0;

                string[] textureClasses = new string[] { "Clay", "Silty Clay", "Sandy Clay", "Clay Loam", "Silty Clay Loam", "Sandy Clay Loam", "Loam", "Silty Loam", "Sandy Loam", "Silt", "Loamy Sand", "Sand", "NO DATA" };
                double[] textureToAlb = new double[] { 0.12, 0.12, 0.13, 0.13, 0.12, 0.13, 0.13, 0.14, 0.13, 0.13, 0.16, 0.19, 0.13 };
                double[] textureToCN2 = new double[] { 73.0, 73.0, 73.0, 73.0, 73.0, 73.0, 73.0, 73.0, 68.0, 73.0, 68.0, 68.0, 73.0 };
                double[] textureToSwcon = new double[] { 0.25, 0.3, 0.3, 0.4, 0.5, 0.5, 0.5, 0.5, 0.6, 0.5, 0.6, 0.75, 0.5 };
                try
                {
                    var stream = await WebUtilities.ExtractDataFromURL(url, cancellationTokenSource.Token);
                    stream.Position = 0;
                    JsonTextReader reader = new JsonTextReader(new StreamReader(stream));
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("properties") && reader.Depth == 1)
                        {
                            reader.Read(); // Read the "start object" token
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    string propName = reader.Value.ToString();
                                    double[] dest = null;
                                    double multiplier = 1.0;
                                    if (propName == "TAXNWRBMajor")
                                    {
                                        soilType = reader.ReadAsString();
                                    }
                                    else if (propName == "TMDMOD_2011")
                                    {
                                        maxTemp = 0.0;
                                        reader.Read();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                        {
                                            if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("M"))
                                            {
                                                reader.Read(); // Read start of object token
                                                for (int i = 0; i < 12; i++)
                                                {
                                                    reader.Read(); // Read a month name
                                                    maxTemp += (double)reader.ReadAsDouble();
                                                }
                                                maxTemp /= 12.0;
                                            }
                                        }
                                    }
                                    else if (propName == "TMNMOD_2011")
                                    {
                                        minTemp = 0.0;
                                        reader.Read();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                        {
                                            if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("M"))
                                            {
                                                reader.Read(); // Read start of object token
                                                for (int i = 0; i < 12; i++)
                                                {
                                                    reader.Read(); // Read a month name
                                                    minTemp += (double)reader.ReadAsDouble();
                                                }
                                                minTemp /= 12.0;
                                            }
                                        }
                                    }
                                    else if (propName == "PREMRG")
                                    {
                                        ppt = 0.0;
                                        reader.Read();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                        {
                                            if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("M"))
                                            {
                                                reader.Read(); // Read start of object token
                                                for (int i = 0; i < 12; i++)
                                                {
                                                    reader.Read(); // Read a month name
                                                    ppt += (double)reader.ReadAsDouble();
                                                }
                                            }
                                        }
                                    }
                                    else if (propName == "BDTICM")  // Is this the best metric to use for find the "bottom" of the soil?
                                    {
                                        reader.Read();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                        {
                                            if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("M"))
                                            {
                                                reader.Read(); // Read start of object token
                                                reader.Read(); // Read property name (which ought to be BDTICM_M)
                                                bedrock = 10.0 * (double)reader.ReadAsDouble();
                                                reader.Skip();
                                            }
                                        }
                                    }
                                    else if (propName == "AWCh1")
                                    {
                                        dest = awc20;
                                        multiplier = 0.01;
                                    }
                                    else if (propName == "AWCh2")
                                    {
                                        dest = awc23;
                                        multiplier = 0.01;
                                    }
                                    else if (propName == "AWCh3")
                                    {
                                        dest = awc25;
                                        multiplier = 0.01;
                                    }
                                    else if (propName == "AWCtS")
                                    {
                                        dest = thetaSat;
                                        multiplier = 0.01;
                                    }
                                    else if (propName == "BLDFIE")
                                    {
                                        dest = bd;
                                        multiplier = 0.001;
                                    }
                                    else if (propName == "CECSOL")
                                    {
                                        dest = cationEC;
                                        multiplier = 1.0;
                                    }
                                    else if (propName == "CLYPPT")
                                    {
                                        dest = clay;
                                        multiplier = 1.0;
                                    }
                                    else if (propName == "CRFVOL")
                                    {
                                        dest = coarse;
                                        multiplier = 1.0;
                                    }
                                    else if (propName == "ORCDRC")
                                    {
                                        dest = ocdrc;
                                        multiplier = 0.1;
                                    }
                                    else if (propName == "PHIHOX")
                                    {
                                        dest = phWater;
                                        multiplier = 0.1;
                                    }
                                    else if (propName == "SLTPPT")
                                    {
                                        dest = silt;
                                        multiplier = 1.0;
                                    }
                                    else if (propName == "SNDPPT")
                                    {
                                        dest = sand;
                                        multiplier = 1.0;
                                    }
                                    else if (propName == "TEXMHT")
                                    {
                                        dest = texture;
                                        multiplier = 1.0;
                                    }
                                    else if (propName == "WWP")
                                    {
                                        dest = thetaWwp;
                                        multiplier = 0.01;
                                    }

                                    if (dest != null)
                                    {
                                        reader.Read();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                        {
                                            if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("M"))
                                            {
                                                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                                {
                                                    if (reader.TokenType == JsonToken.PropertyName)
                                                    {
                                                        string tokenName = reader.Value.ToString();
                                                        if (tokenName.StartsWith("sl"))
                                                        {
                                                            int index = Int32.Parse(tokenName.Substring(2)) - 1;
                                                            dest[index] = (double)reader.ReadAsDouble() * multiplier;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                        reader.Skip();
                                }
                            }
                        }
                    }

                    var newSoil = new Soil();
                    Chemical analysis = new Chemical();
                    Physical waterNode = new Physical();
                    Organic organicMatter = new Organic();
                    WaterBalance soilWater = new WaterBalance();
                    Water initialWater = new Water();
                    Solute no3 = new Solute();
                    Solute nh4 = new Solute();
                    Nutrient nutrient = new Nutrient();
                    nutrient.ResourceName = "Nutrient";

                    SoilCrop wheat = new SoilCrop();
                    waterNode.Children.Add(wheat);
                    wheat.Name = "WheatSoil";
                    waterNode.ParentAllDescendants();

                    CERESSoilTemperature temperature = new CERESSoilTemperature();
                    temperature.Name = "Temperature";

                    newSoil.Children.Add(waterNode);
                    newSoil.Children.Add(soilWater);
                    newSoil.Children.Add(nutrient);
                    newSoil.Children.Add(organicMatter);
                    newSoil.Children.Add(analysis);
                    newSoil.Children.Add(initialWater);
                    newSoil.Children.Add(no3);
                    newSoil.Children.Add(nh4);
                    newSoil.Children.Add(temperature);
                    newSoil.ParentAllDescendants();
                    newSoil.OnCreated();

                    newSoil.Name = "Synthetic soil derived from ISRIC SoilGrids REST API";
                    newSoil.DataSource = "ISRIC SoilGrids";
                    newSoil.SoilType = soilType;
                    newSoil.Latitude = Convert.ToDouble(latitudeEditBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                    newSoil.Longitude = Convert.ToDouble(longitudeEditBox.Text, System.Globalization.CultureInfo.InvariantCulture);

                    // ISRIC values are for "levels", not "intervals", so we need to convert to layers
                    // Following Andrew Moore's lead on layer thickness and weightings.

                    double[] thickness = new double[] { 150.0, 150.0, 150.0, 150.0, 200.0, 200.0, 200.0, 200.0, 300.0, 300.0 };
                    double[] depth = new double[thickness.Length];
                    int layerCount = thickness.Length;
                    for (int i = 0; i < thickness.Length; i++)
                    {
                        depth[i] = thickness[i] + (i > 0 ? depth[i - 1] : 0.0);
                        if ((i > 0) && (layerCount == thickness.Length) && (bedrock < depth[i] + 20.0))
                        {
                            layerCount = i + 1;
                            thickness[i] = Math.Min(thickness[i], Math.Max(0.0, bedrock - (depth[i] - thickness[i])));
                            if (i == 1)
                                thickness[i] = Math.Max(50.0, thickness[i]);
                            Array.Resize(ref thickness, layerCount);
                        }
                    }

                    analysis.Thickness = thickness;
                    waterNode.Thickness = thickness;
                    soilWater.Thickness = thickness;
                    organicMatter.Thickness = thickness;

                    initialWater.Name = "Initial water";
                    initialWater.FilledFromTop = true;
                    initialWater.FractionFull = 0.0;

                    // Initialise nitrogen to 0.0
                    no3.Name = "NO3";
                    no3.InitialValues = new double[layerCount];
                    nh4.Name = "NH4";
                    nh4.InitialValues = new double[layerCount];

                    double tAvg = (maxTemp + minTemp) / 2.0;
                    soilWater.CNCov = 0.0;
                    soilWater.CNRed = 20.0;
                    soilWater.SummerDate = newSoil.Latitude <= 0.0 ? "1-nov" : "1-may";
                    soilWater.WinterDate = newSoil.Latitude <= 0.0 ? "1-apr" : "1-oct";
                    soilWater.SummerCona = 6.0;
                    soilWater.SummerU = 6.0;
                    soilWater.WinterCona = tAvg < 21.0 ? 2.5 : 6.0;
                    soilWater.WinterU = tAvg < 21.0 ? 4.0 : 6.0;
                    soilWater.Salb = textureToAlb[(int)Math.Round(texture[0] - 1)];
                    soilWater.CN2Bare = textureToCN2[(int)Math.Round(texture[0] - 1)];
                    double[] swcon = new double[7];
                    for (int i = 0; i < 7; i++)
                        swcon[i] = textureToSwcon[(int)Math.Round(texture[i] - 1)];
                    soilWater.SWCON = ConvertLayers(swcon, layerCount);

                    waterNode.BD = ConvertLayers(bd, layerCount);
                    waterNode.LL15 = ConvertLayers(thetaWwp, layerCount);
                    waterNode.SAT = ConvertLayers(thetaSat, layerCount);
                    waterNode.AirDry = ConvertLayers(MathUtilities.Divide_Value(thetaWwp, 3.0), layerCount);
                    double[] dul = new double[7];
                    for (int i = 0; i < 7; i++)
                        dul[i] = thetaWwp[i] + awc20[i];  // This could be made Moore complex
                    waterNode.DUL = ConvertLayers(dul, layerCount);

                    waterNode.ParticleSizeSand = ConvertLayers(sand, layerCount);
                    waterNode.ParticleSizeSilt = ConvertLayers(silt, layerCount);
                    waterNode.ParticleSizeClay = ConvertLayers(clay, layerCount);
                    // waterNode.Rocks = ConvertLayers(coarse, layerCount);
                    analysis.PH = ConvertLayers(phWater, layerCount);
                    // Obviously using the averaging in "ConvertLayers" for texture classes is not really correct, but should be OK as a first pass if we don't have sharply contrasting layers
                    double[] classes = ConvertLayers(texture, layerCount);
                    string[] textures = new string[layerCount];
                    for (int i = 0; i < layerCount; i++)
                        textures[i] = textureClasses[(int)Math.Round(classes[i]) - 1];


                    double[] xf = new double[layerCount];
                    double[] kl = new double[layerCount];
                    double[] ll = new double[layerCount];
                    double p1 = 1.4;
                    double p2 = 1.60 - p1;
                    double p3 = 1.80 - p1;
                    double topEffDepth = 0.0;
                    double klMax = 0.06;
                    double depthKl = 900.0;
                    double depthRoot = 1900.0;

                    for (int i = 0; i < layerCount; i++)
                    {
                        xf[i] = 1.0 - (waterNode.BD[i] - (p1 + p2 * 0.01 * waterNode.ParticleSizeSand[i])) / p3;
                        xf[i] = Math.Max(0.1, Math.Min(1.0, xf[i]));
                        double effectiveThickness = thickness[i] * xf[i];
                        double bottomEffDepth = topEffDepth + effectiveThickness;
                        double propMaxKl = Math.Max(0.0, Math.Min(bottomEffDepth, depthKl) - topEffDepth) / effectiveThickness;
                        double propDecrKl = Math.Max(Math.Max(0.0, Math.Min(bottomEffDepth, depthRoot) - topEffDepth) / effectiveThickness - propMaxKl, 0.0);
                        double propZeroKl = 1.0 - propMaxKl - propDecrKl;
                        double ratioTopDepth = Math.Max(0.0, Math.Min((depthRoot - topEffDepth) / (depthRoot - depthKl), 1.0));
                        double ratioBottomDepth = Math.Max(0.0, Math.Min((depthRoot - bottomEffDepth) / (depthRoot - depthKl), 1.0));
                        double meanDecrRatio = 0.5 * (ratioTopDepth + ratioBottomDepth);
                        double weightedRatio = propMaxKl * 1.0 + propDecrKl * meanDecrRatio + propZeroKl * 0.0;
                        kl[i] = klMax * weightedRatio;
                        ll[i] = waterNode.LL15[i] + (waterNode.DUL[i] - waterNode.LL15[i]) * (1.0 - weightedRatio);
                        if (kl[i] <= 0.0)
                            xf[i] = 0.0;
                        topEffDepth = bottomEffDepth;
                    }
                    wheat.XF = xf;
                    wheat.KL = kl;
                    wheat.LL = ll;

                    organicMatter.Carbon = ConvertLayers(ocdrc, layerCount);

                    double rootWt = Math.Max(0.0, Math.Min(3000.0, 2.5 * (ppt - 100.0)));
                    // For AosimX, root wt needs to be distributed across layers. This conversion logic is adapted from that used in UpgradeToVersion52
                    double[] rootWtFraction = new double[layerCount];
                    double profileDepth = depth[layerCount - 1];
                    double cumDepth = 0.0;
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        double fracLayer = Math.Min(1.0, MathUtilities.Divide(profileDepth - cumDepth, thickness[layer], 0.0));
                        cumDepth += thickness[layer];
                        rootWtFraction[layer] = fracLayer * Math.Exp(-3.0 * Math.Min(1.0, MathUtilities.Divide(cumDepth, profileDepth, 0.0)));
                    }
                    // get the actuall FOM distribution through layers (adds up to one)
                    double totFOMfraction = MathUtilities.Sum(rootWtFraction);
                    for (int layer = 0; layer < thickness.Length; layer++)
                        rootWtFraction[layer] /= totFOMfraction;
                    organicMatter.FOM = MathUtilities.Multiply_Value(rootWtFraction, rootWt);

                    double[] fBiom = { 0.04, 0.04 - 0.03 * (225.0 - 150.0) / (400.0 - 150.0),
                        (400.0 - 300.0) / (450.0 - 300.0) * (0.04 - 0.03 * (350.0 - 150.0) / (400.0 - 150.0)) + (450.0 - 400.0) / (450.0 - 300.0) * 0.01,
                        0.01, 0.01, 0.01, 0.01, 0.01, 0.01, 0.01 };
                    Array.Resize(ref fBiom, layerCount);
                    double inert_c = 0.95 * ocdrc[4];
                    double[] fInert = new double[7];
                    for (int layer = 0; layer < 7; layer++)
                        fInert[layer] = Math.Min(0.99, inert_c / ocdrc[layer]);
                    organicMatter.FInert = ConvertLayers(fInert, layerCount); // Not perfect, but should be good enough
                    organicMatter.FBiom = fBiom;
                    organicMatter.FOMCNRatio = 40.0;
                    organicMatter.SoilCNRatio = Enumerable.Repeat(11.0, layerCount).ToArray(); // Is there any good way to estimate this? ISRIC provides no N data

                    CERESSoilTemperature temperatureNew = new CERESSoilTemperature();
                    temperature.Name = "Temperature";

                    newSoil.Children.Add(temperatureNew);
                    newSoil.OnCreated();

                    soils.Add(new SoilFromDataSource()
                    {
                        DataSource = "ISRIC",
                        Soil = newSoil
                    });
                }
                catch (OperationCanceledException)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
            return soils;
        }

        /// <summary>
        /// Converts data for 7 input levels to layerCount (up to 10) depth ranges
        /// </summary>
        /// <param name="inputs"></param>
        /// /// <param name="layerCount"></param>
        /// <returns></returns>
        private static double[] ConvertLayers(double[] inputs, int layerCount)
        {
            double[] result = new double[layerCount];
            double[,] depthWeights = new double[,]
            {
                { 1.0/6.0, 3.0/6.0, 2.0/6.0,     0.0,     0.0,       0.0,       0.0 },
                {     0.0,     0.0, 1.0/2.0, 1.0/2.0,     0.0,       0.0,       0.0 },
                {     0.0,     0.0,     0.0, 3.0/4.0, 1.0/4.0,       0.0,       0.0 },
                {     0.0,     0.0,     0.0, 1.0/4.0, 3.0/4.0,       0.0,       0.0 },
                {     0.0,     0.0,     0.0,     0.0, 3.0/4.0,   1.0/4.0,       0.0 },
                {     0.0,     0.0,     0.0,     0.0, 1.0/4.0,   3.0/4.0,       0.0 },
                {     0.0,     0.0,     0.0,     0.0,     0.0, 18.0/20.0,  2.0/20.0 },
                {     0.0,     0.0,     0.0,     0.0,     0.0, 14.0/20.0,  6.0/20.0 },
                {     0.0,     0.0,     0.0,     0.0,     0.0,  9.0/20.0, 11.0/20.0 },
                {     0.0,     0.0,     0.0,     0.0,     0.0,  3.0/20.0, 17.0/20.0 }
            };
            for (int i = 0; i < Math.Max(10, layerCount); i++)
            {
                result[i] = 0.0;
                for (int j = 0; j < 7; j++)
                    result[i] += inputs[j] * depthWeights[i, j];
            }
            return result;
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