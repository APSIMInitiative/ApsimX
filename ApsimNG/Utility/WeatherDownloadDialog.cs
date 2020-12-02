
namespace Utility
{
    using APSIM.Shared.Utilities;
    using Gtk;
    using Models;
    using Models.Climate;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using UserInterface.Commands;
    using UserInterface.Extensions;
    using UserInterface.Presenters;
    using UserInterface.Views;

    class WeatherDownloadDialog
    {
        // Gtk Widgets
        private Dialog dialog1 = null;
        private Button btnOk = null;
        private Button btnCancel = null;
        private RadioButton radioAus = null;
        private RadioButton radioWorld = null;
        private Button btnGetPlacename = null;
        private Button btnGetLocation = null;
        private Entry entryLatitude = null;
        private Entry entryLongitude = null;
        private Entry entryPlacename = null;
        private RadioButton radioSiloDataDrill = null;
        private RadioButton radioSiloPatchPoint = null;
        private RadioButton radioNASA = null;
        private Calendar calendarStart = null;
        private Calendar calendarEnd = null;
        private Entry entryFilePath = null;
        private Button btnBrowse = null;
        private Entry entryEmail = null;

        private Model dest = null; // The destination. Should either be a Weather (to be replaced) or a Simulation (to which the Weather will be added)
        private IModel replaceNode;
        private ExplorerView owningView;
        private ExplorerPresenter explorerPresenter;
        private ScrolledWindow scroller;
        private VBox vbox1;
        VBox dialogVBox;

        /// <summary>
        /// URI for accessing the Google geocoding API. I know the key shouldn't be placed on Github, but I'm not overly concerned.
        /// </summary>
        private static string googleGeocodingApi = "https://maps.googleapis.com/maps/api/geocode/json?key=AIzaSyC6OF6s7DwSHwibtQqAKC9GtOQEwTkCpkw&";

        /// <summary>
        /// Class constructor
        /// </summary>
        public WeatherDownloadDialog()
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.WeatherDownload.glade");
            dialog1 = (Dialog)builder.GetObject("dialog1");
            vbox1 = (VBox)builder.GetObject("vbox1");
            dialogVBox = (VBox)builder.GetObject("dialog-vbox1");
            scroller = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            scroller.SizeAllocated += OnSizeAllocated;
            radioAus = (RadioButton)builder.GetObject("radioAus");
            radioWorld = (RadioButton)builder.GetObject("radioWorld");
            entryLatitude = (Entry)builder.GetObject("entryLatitude");
            entryLongitude = (Entry)builder.GetObject("entryLongitude");
            entryPlacename = (Entry)builder.GetObject("entryPlacename");
            radioSiloDataDrill = (RadioButton)builder.GetObject("radioSiloDataDrill");
            radioSiloPatchPoint = (RadioButton)builder.GetObject("radioSiloPatchPoint");
            radioNASA = (RadioButton)builder.GetObject("radioNASA");
            btnOk = (Button)builder.GetObject("btnOk");
            btnCancel = (Button)builder.GetObject("btnCancel");
            btnGetPlacename = (Button)builder.GetObject("btnGetPlacename");
            btnGetLocation = (Button)builder.GetObject("btnGetLocation");
            calendarStart = (Calendar)builder.GetObject("calendarStart");
            calendarEnd = (Calendar)builder.GetObject("calendarEnd");
            entryFilePath = (Entry)builder.GetObject("entryFilePath");
            btnBrowse = (Button)builder.GetObject("btnBrowse");
            entryEmail = (Entry)builder.GetObject("entryEmail");
            calendarEnd.Date = DateTime.Today.AddDays(-1.0);
            radioAus.Clicked += RadioAus_Clicked;
            radioWorld.Clicked += RadioAus_Clicked;
            btnOk.Clicked += BtnOk_Clicked;
            btnCancel.Clicked += BtnCancel_Clicked;
            btnGetLocation.Clicked += BtnGetLocation_Clicked;
            btnGetPlacename.Clicked += BtnGetPlacename_Clicked;
            btnBrowse.Clicked += BtnBrowse_Clicked;
        }

        private void OnSizeAllocated(object o, SizeAllocatedArgs args)
        {
            try
            {
                if (vbox1.Allocation.Height > 1 && vbox1.Allocation.Width > 1)
                {
                    dialog1.DefaultHeight = vbox1.Allocation.Height + dialogVBox.Allocation.Height;
                    dialog1.DefaultWidth = vbox1.Allocation.Width + 20;
                    scroller.SizeAllocated -= OnSizeAllocated;
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        private void BtnBrowse_Clicked(object sender, EventArgs e)
        {
            try
            {
                string fileName = ViewBase.AskUserForFileName("Choose a location for saving the weather file", Utility.FileDialog.FileActionType.Save, "APSIM Weather file (*.met)|*.met", entryFilePath.Text);
                if (!String.IsNullOrEmpty(fileName))
                {
                    entryFilePath.Text = fileName;
                }
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        private void ShowMessage(MessageType type, string msg, string title)
        {
            MessageDialog md = new MessageDialog(dialog1, DialogFlags.Modal, type, ButtonsType.Ok, msg);
            md.Title = title;
            md.Run();
            md.Cleanup();
        }

        /// <summary>
        /// Handles presses of the "ok" button
        /// Attempts to retrieve the indicated soil description and put it in place of the original soil.
        /// Closes the dialog if successful
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnOk_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                    return;
                if (String.IsNullOrWhiteSpace(entryFilePath.Text))
                {
                    ShowMessage(MessageType.Warning, "You must provide a file name for saving the weather data", "No file path");
                    BtnBrowse_Clicked(this, null);
                    return;
                }
                string newWeatherPath = null;
                WaitCursor = true;
                try
                {
                    if (radioSiloDataDrill.Active)
                        newWeatherPath = GetDataDrill();
                    else if (radioSiloPatchPoint.Active)
                        newWeatherPath = GetPatchPoint();
                    else if (radioNASA.Active)
                        newWeatherPath = GetNasaChirps();
                }
                finally
                {
                    WaitCursor = false;
                }
                if (string.IsNullOrWhiteSpace(newWeatherPath))
                {
                    ShowMessage(MessageType.Error, "Unable to obtain data for this site", "Error");
                }
                else
                {
                    if (dest is Weather)
                    {
                        // If there is an existing Weather model (and there usually will be), is it better to replace
                        // the model, or modify the FullFileName of the original?
                        IPresenter currentPresenter = explorerPresenter.CurrentPresenter;
                        if (currentPresenter is MetDataPresenter)
                            (currentPresenter as MetDataPresenter).OnBrowse(newWeatherPath);
                        else
                            explorerPresenter.CommandHistory.Add(new UserInterface.Commands.ChangeProperty(dest, "FullFileName", newWeatherPath));
                    }
                    else if (dest is Simulation)
                    {
                        Weather newWeather = new Weather();
                        newWeather.FullFileName = newWeatherPath;
                        var command = new AddModelCommand(replaceNode, newWeather);
                        explorerPresenter.CommandHistory.Add(command, true);
                        explorerPresenter.Refresh();
                    }
                }
                dialog1.Cleanup();
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        private void RadioAus_Clicked(object sender, EventArgs e)
        {
            try
            {
                radioSiloDataDrill.Sensitive = radioAus.Active;
                radioSiloPatchPoint.Sensitive = radioAus.Active;
                if (!radioAus.Active)
                    radioNASA.Active = true;
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        /// <summary>
        /// Handles presses of the "get placename" button
        /// Uses Google's geocoding service to find the placename for the current latitude and longitude
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnGetPlacename_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                    return;
                string url = googleGeocodingApi + "latlng=" + entryLatitude.Text + ',' + entryLongitude.Text;

                MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
                stream.Position = 0;
                JsonTextReader reader = new JsonTextReader(new StreamReader(stream));
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("formatted_address"))
                    {
                        reader.Read();
                        entryPlacename.Text = reader.Value.ToString();
                        break;
                    }
                }
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        /// <summary>
        /// Handles presses of the "get location" button
        /// Uses Googles' geocoding service to find the co-ordinates of the specified placename
        /// Currently this displays only the first match. Since there can be multiple matches
        /// (there are a lot of "Black Mountain"s in Australia, for example, it would be better
        /// to present the user with the list of matches when there is more than one.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnGetLocation_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entryPlacename.Text))
                    return;
                // For now, name matching is restricted to Australia, since at this point we don't
                // yet have things set up for the global soil database
                string url = googleGeocodingApi + "components=" + (radioAus.Active ? "country:AU|" : "") + "locality:" + entryPlacename.Text;

                MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
                stream.Position = 0;
                JsonTextReader reader = new JsonTextReader(new StreamReader(stream));
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
                                entryLatitude.Text = reader.Value.ToString();
                            }
                            else if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("lng"))
                            {
                                reader.Read();
                                entryLongitude.Text = reader.Value.ToString();
                            }
                        }
                        break;
                    }
                    else if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("status"))
                    {
                        reader.Read();
                        string status = reader.Value.ToString();
                        if (status == "ZERO_RESULTS")
                        {
                            ShowMessage(MessageType.Warning, String.Format("No location matching the name '{0}' could be found.", entryPlacename.Text), "Location not found");
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        /// <summary>
        /// Handles presses of the "cancel" button by closing the dialog
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnCancel_Clicked(object sender, EventArgs e)
        {
            try
            {
                dialog1.Cleanup();
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        /// <summary>
        /// Initialises and displays the dialog
        /// </summary>
        /// <param name="dest">The Weather object to be replaced, or Zone to which Weather will be added</param>
        /// <param name="view">The ExplorerView displaying the soil object in its tree</param>
        /// <param name="nodePath">The soil object within the view's tree</param>
        /// <param name="explorerPresenter">The ExplorerPresenter that is managing all of this</param>
        public void ShowFor(Model dest, ExplorerView view, IModel nodePath, ExplorerPresenter explorerPresenter)
        {
            this.dest = dest;
            this.replaceNode = nodePath;
            this.owningView = view;
            this.explorerPresenter = explorerPresenter;
            dialog1.TransientFor = view.MainWidget.Toplevel as Window;
            dialog1.Parent = view.MainWidget.Toplevel;
            dialog1.WindowPosition = WindowPosition.CenterOnParent;
            // Attempt to find an initial latitude and longitude from a Weather model
            IModel weather = dest.FindInScope<Models.Interfaces.IWeather>() as IModel;
            double latitude, longitude;
            if (weather != null && weather is Weather)
            {
                Weather weatherObj = weather as Weather;
                this.dest = weatherObj;
                if (weatherObj.OpenDataFile())
                {
                    latitude = weatherObj.Latitude;
                    longitude = weatherObj.Longitude;
                    entryLatitude.Text = latitude.ToString();
                    entryLongitude.Text = longitude.ToString();
                }
            }
            dialog1.Show();
        }

        /// <summary>
        /// Checks to see whether the entered values for latitude or longitude are valid
        /// </summary>
        /// <param name="entryBox">The Entry control to check. Should be either entryLatitude or entryLongitude</param>
        /// <returns>True if the text in the entry is numeric and in the correct range; false otherwise</returns>
        private bool CheckValue(Entry entryBox)
        {
            bool result = false;
            double minVal = 0.0;
            double maxVal = 0.0;
            string contents = "";
            if (entryBox == entryLatitude)
            {
                minVal = -90.0;
                maxVal = 90.0;
                contents = "latitude";
            }
            else if (entryBox == entryLongitude)
            {
                minVal = -180.0;
                maxVal = 180.0;
                contents = "longitude";
            }

            double value;
            if (Double.TryParse(entryBox.Text, out value) && value >= minVal && value <= maxVal)
            {
                result = true;
            }
            else
            {
                result = false;
                ShowMessage(MessageType.Warning, String.Format("The value for {0} should be a number in the range {1:F2} to {2:F2}", contents, minVal, maxVal), "Invalid Entry");
            }
            return result;
        }

        public string GetDataDrill()
        {
            string newWeatherPath = null;
            string dest = PathUtilities.GetAbsolutePath(entryFilePath.Text, this.explorerPresenter.ApsimXFile.FileName);
            DateTime startDate = calendarStart.Date;
            if (startDate.Year < 1889)
            {
                ShowMessage(MessageType.Warning, "SILO data is not available before 1889", "Invalid start date");
                return null;
            }
            DateTime endDate = calendarEnd.Date;
            if (endDate.CompareTo(DateTime.Today) >= 0)
            {
                ShowMessage(MessageType.Warning, "SILO data end date can be no later than yesterday", "Invalid end date");
                return null;
            }
            if (endDate.CompareTo(startDate) < 0)
            {
                ShowMessage(MessageType.Warning, "The end date must be after the start date!", "Invalid dates");
                return null;
            }
            if (String.IsNullOrWhiteSpace(entryEmail.Text))
            {
                ShowMessage(MessageType.Warning, "The SILO data API requires you to provide your e-mail address", "E-mail address required");
                return null;
            }
            string url = String.Format("https://www.longpaddock.qld.gov.au/cgi-bin/silo/DataDrillDataset.php?start={0:yyyyMMdd}&finish={1:yyyyMMdd}&lat={2}&lon={3}&format=apsim&username={4}&password=silo",
                            startDate, endDate, entryLatitude.Text, entryLongitude.Text, System.Net.WebUtility.UrlEncode(entryEmail.Text));
            MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
            stream.Seek(0, SeekOrigin.Begin);
            string headerLine = new StreamReader(stream).ReadLine();
            stream.Seek(0, SeekOrigin.Begin);
            if (headerLine.StartsWith("[weather.met.weather]"))
            {
                using (FileStream fs = new FileStream(dest, FileMode.Create))
                {
                    stream.CopyTo(fs);
                    fs.Flush();
                }
                if (File.Exists(dest))
                {
                    newWeatherPath = dest;
                }
            }
            else
            {
                ShowMessage(MessageType.Error, new StreamReader(stream).ReadToEnd(), "Not valid APSIM weather data");
            }
            return newWeatherPath;
        }

        public string GetPatchPoint()
        {
            string newWeatherPath = null;
            string dest = PathUtilities.GetAbsolutePath(entryFilePath.Text, this.explorerPresenter.ApsimXFile.FileName);
            DateTime startDate = calendarStart.Date;
            if (startDate.Year < 1889)
            {
                ShowMessage(MessageType.Warning, "SILO data is not available before 1889", "Invalid start date");
                return null;
            }
            DateTime endDate = calendarEnd.Date;
            if (endDate.CompareTo(DateTime.Today) >= 0)
            {
                ShowMessage(MessageType.Warning, "SILO data end date can be no later than yesterday", "Invalid end date");
                return null;
            }
            if (endDate.CompareTo(startDate) < 0)
            {
                ShowMessage(MessageType.Warning, "The end date must be after the start date!", "Invalid dates");
                return null;
            }
            if (String.IsNullOrWhiteSpace(entryEmail.Text))
            {
                ShowMessage(MessageType.Warning, "The SILO data API requires you to provide your e-mail address", "E-mail address required");
                return null;
            }

            // Patch point get a bit complicated. We need a BOM station number, but can't really expect the user
            // to know that in advance. So what we can attempt to do is use the provided lat and long in the geocoding service
            // to get us a placename, then use the SILO name search API to find a list of stations for us.
            // If we get multiple stations, we let the user choose the one they wish to use.

            string url = googleGeocodingApi + "latlng=" + entryLatitude.Text + ',' + entryLongitude.Text;
            try
            {
                MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
                stream.Position = 0;
                JsonTextReader reader = new JsonTextReader(new StreamReader(stream));
                // Parsing the JSON gets a little tricky with a forward-reading parser. We're trying to track
                // down a "short_name" address component of "locality" type (I guess).
                string locName = "";
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("address_components"))
                    {
                        reader.Read();
                        if (reader.TokenType == JsonToken.StartArray)
                        {
                            JArray arr = JArray.Load(reader);
                            foreach (JToken token in arr)
                            {
                                JToken typesToken = token.Last;
                                JArray typesArray = typesToken.First as JArray;
                                for (int i = 0; i < typesArray.Count; i++)
                                {
                                    if (typesArray[i].ToString() == "locality")
                                    {
                                        locName = token["short_name"].ToString();
                                        break;
                                    }
                                }
                                if (!string.IsNullOrEmpty(locName))
                                    break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(locName))
                        break;
                }
                if (string.IsNullOrEmpty(locName))
                {
                    ShowMessage(MessageType.Error, "Unable to find a name key for the specified location", "Error determining location");
                    return null;
                }
                int stationNumber = -1;
                if (locName.Contains(" ")) // the SILO API doesn't handle spaces well
                {
                    Regex regex = new Regex(" .");
                    locName = regex.Replace(locName, "_");
                }
                string stationUrl = String.Format("https://www.longpaddock.qld.gov.au/cgi-bin/silo/PatchedPointDataset.php?format=name&nameFrag={0}", locName);
                MemoryStream stationStream = WebUtilities.ExtractDataFromURL(stationUrl);
                stationStream.Position = 0;
                StreamReader streamReader = new StreamReader(stationStream);
                string stationInfo = streamReader.ReadToEnd();
                string[] stationLines = stationInfo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                if (stationLines.Length == 0)
                {
                    ShowMessage(MessageType.Error, "Unable to find a BOM station for this location", "Cannot find station");
                    return null;
                }
                if (stationLines.Length == 1)
                {
                    string[] lineInfo = stationLines[0].Split('|');
                    stationNumber = Int32.Parse(lineInfo[0]);
                }
                else
                {
                    MessageDialog md = new MessageDialog(owningView.MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Question, ButtonsType.OkCancel,
                                       "Which station do you wish to use?");
                    md.Title = "Select BOM Station";
                    Gtk.TreeView tree = new Gtk.TreeView();
                    ListStore list = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
                    tree.AppendColumn("Number", new CellRendererText(), "text", 0);
                    tree.AppendColumn("Name", new CellRendererText(), "text", 1);
                    tree.AppendColumn("Latitude", new CellRendererText(), "text", 2);
                    tree.AppendColumn("Longitude", new CellRendererText(), "text", 3);
                    tree.AppendColumn("State", new CellRendererText(), "text", 4);
                    tree.AppendColumn("Elevation", new CellRendererText(), "text", 5);
                    tree.AppendColumn("Notes", new CellRendererText(), "text", 6);
                    foreach (string stationLine in stationLines)
                    {
                        string[] lineInfo = stationLine.Split('|');
                        list.AppendValues(lineInfo);
                    }
                    tree.Model = list;
                    tree.RowActivated += OnPatchPointSoilSelected;
#if NETFRAMEWORK
                    Box box = md.VBox;
#else
                    Box box = md.ContentArea;
#endif
                    box.PackStart(tree, true, true, 5);
                    box.ShowAll();

                    ResponseType result = (ResponseType)md.Run();
                    if (result == ResponseType.Ok)
                    {
                        TreeIter iter;
                        tree.Selection.GetSelected(out iter);
                        string stationString = (string)list.GetValue(iter, 0);
                        stationNumber = Int32.Parse(stationString);
                    }
                    md.Cleanup();
                }
                if (stationNumber >= 0) // Phew! We finally have a station number. Now fetch the data.
                {
                    string pointUrl = String.Format("https://www.longpaddock.qld.gov.au/cgi-bin/silo/PatchedPointDataset.php?start={0:yyyyMMdd}&finish={1:yyyyMMdd}&station={2}&format=apsim&username={3}",
                                    startDate, endDate, stationNumber, System.Net.WebUtility.UrlEncode(entryEmail.Text));
                    MemoryStream pointStream = WebUtilities.ExtractDataFromURL(pointUrl);
                    pointStream.Seek(0, SeekOrigin.Begin);
                    string headerLine = new StreamReader(pointStream).ReadLine();
                    pointStream.Seek(0, SeekOrigin.Begin);
                    if (headerLine.StartsWith("[weather.met.weather]"))
                    {
                        using (FileStream fs = new FileStream(dest, FileMode.Create))
                        {
                            pointStream.CopyTo(fs);
                            fs.Flush();
                        }
                        if (File.Exists(dest))
                        {
                            newWeatherPath = dest;
                        }
                    }
                    else
                    {
                        ShowMessage(MessageType.Error, new StreamReader(pointStream).ReadToEnd(), "Not valid APSIM weather data");
                    }
                }
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
            return newWeatherPath;
        }

        private void OnPatchPointSoilSelected(object sender, RowActivatedArgs args)
        {
            try
            {
                if (sender is Gtk.TreeView tree)
                {
                    tree.RowActivated -= OnPatchPointSoilSelected;
                    if (tree.Toplevel is Dialog dialog)
                        dialog.Respond(ResponseType.Ok);
                }
            }
            catch (Exception err)
            {
                ShowMessage(MessageType.Error, err.Message, "Error");
            }
        }

        public string GetNasaChirps()
        {
            string newWeatherPath = null;
            string dest = PathUtilities.GetAbsolutePath(entryFilePath.Text, this.explorerPresenter.ApsimXFile.FileName);
            DateTime startDate = calendarStart.Date;
            if (startDate.Year < 1981)
            {
                ShowMessage(MessageType.Warning, "NASA/CHIRPS data is not available before 1981", "Invalid start date");
                return null;
            }
            DateTime endDate = calendarEnd.Date;
            if (endDate.CompareTo(DateTime.Today) >= 0)
            {
                ShowMessage(MessageType.Warning, "NASA/CHRIPS data end date can be no later than yesterday", "Invalid end date");
                return null;
            }
            string url = String.Format("https://worldmodel.csiro.au/gclimate?lat={0}&lon={1}&format=apsim&start={2:yyyyMMdd}&stop={3:yyyyMMdd}",
                            entryLatitude.Text, entryLongitude.Text, startDate, endDate);
            MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
            stream.Position = 0;
            using (FileStream fs = new FileStream(dest, FileMode.OpenOrCreate))
            {
                stream.CopyTo(fs);
                fs.Flush();
            }
            if (File.Exists(dest))
            {
                newWeatherPath = dest;
            }
            return newWeatherPath;
        }

        private bool waiting = false;
        /// <summary>
        /// Used to modify the cursor. If set to true, the waiting cursor will be displayed.
        /// If set to false, the default cursor will be used.
        /// </summary>
        private bool WaitCursor
        {
            get
            {
                return waiting;
            }
            set
            {
                if (dialog1.Toplevel.GetGdkWindow() != null)
                {
                    dialog1.Toplevel.GetGdkWindow().Cursor = value ? new Gdk.Cursor(Gdk.CursorType.Watch) : null;
                    waiting = value;
                }
            }
        }
    }
}
