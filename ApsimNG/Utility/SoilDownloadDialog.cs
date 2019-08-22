namespace Utility
{
    using APSIM.Shared.Utilities;
    using Gtk;
    using Importer;
    using Newtonsoft.Json;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Soils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using UserInterface.Commands;
    using UserInterface.Presenters;
    using UserInterface.Views;
    using System.Globalization;

    /// <summary>
    /// Class for displaying a dialog to select a soil description to be downloaded from ASRIS or ISRIC
    /// This needs a bit more polishing to do a better job of guiding the user, and of informing them when
    /// things to wrong.
    /// </summary>
    class SoilDownloadDialog
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
        private RadioButton radioSynth = null;
        private RadioButton radioMatch = null;
        private RadioButton radioISRIC = null;
        private RadioButton radioSoilGrids = null;

        private Model dest = null; // The destination. Should either be a Soil (to be replaced) or a Zone (to which the Soil will be added)
        private string replaceNode;
        private ExplorerView owningView;
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Class constructor
        /// </summary>
        public SoilDownloadDialog()
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.SoilDownload.glade");
            dialog1 = (Dialog)builder.GetObject("dialog1");
            radioAus = (RadioButton)builder.GetObject("radioAus");
            radioWorld = (RadioButton)builder.GetObject("radioWorld");
            entryLatitude = (Entry)builder.GetObject("entryLatitude");
            entryLongitude = (Entry)builder.GetObject("entryLongitude");
            entryPlacename = (Entry)builder.GetObject("entryPlacename");
            radioSynth = (RadioButton)builder.GetObject("radioSynth");
            radioMatch = (RadioButton)builder.GetObject("radioMatch");
            radioISRIC = (RadioButton)builder.GetObject("radioISRIC");
            radioSoilGrids = (RadioButton)builder.GetObject("radioSoilGrids");
            btnOk = (Button)builder.GetObject("btnOk");
            btnCancel = (Button)builder.GetObject("btnCancel");
            btnGetPlacename = (Button)builder.GetObject("btnGetPlacename");
            btnGetLocation = (Button)builder.GetObject("btnGetLocation");
            radioAus.Clicked += RadioAus_Clicked;
            radioWorld.Clicked += RadioAus_Clicked;
            btnOk.Clicked += BtnOk_Clicked;
            btnCancel.Clicked += BtnCancel_Clicked;
            btnGetLocation.Clicked += BtnGetLocation_Clicked;
            btnGetPlacename.Clicked += BtnGetPlacename_Clicked;
        }

        private void RadioAus_Clicked(object sender, EventArgs e)
        {
            radioSynth.Visible = radioAus.Active;
            radioMatch.Visible = radioAus.Active;
            radioISRIC.Visible = !radioAus.Active;
            radioSoilGrids.Visible = !radioAus.Active;
        }

        /// <summary>
        /// URI for accessing the Google geocoding API. I don't recall exactly who owns this key!
        /// </summary>
        private static string googleGeocodingApi = "https://maps.googleapis.com/maps/api/geocode/json?key=AIzaSyC6OF6s7DwSHwibtQqAKC9GtOQEwTkCpkw&";

        /// <summary>
        /// Handles presses of the "get placename" button
        /// Uses Google's geocoding service to find the placename for the current latitude and longitude
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnGetPlacename_Clicked(object sender, EventArgs e)
        {
            if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                return;
            string url = googleGeocodingApi + "latlng=" + entryLatitude.Text + ',' + entryLongitude.Text;
            try
            {
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
            catch
            { }
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
            if (string.IsNullOrWhiteSpace(entryPlacename.Text))
                return;
            // For now, name matching is restricted to Australia, since at this point we don't
            // yet have things set up for the global soil database
            string url = googleGeocodingApi + "components=" + (radioAus.Active ? "country:AU|" : "") + "locality:" + entryPlacename.Text;
            try
            {
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
                            MessageDialog md = new MessageDialog(owningView.MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                            String.Format("No location matching the name '{0}' could be found.", entryPlacename.Text));
                            md.Title = "Location not found";
                            md.Run();
                            md.Destroy();
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Handles presses of the "cancel" button by closing the dialog
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnCancel_Clicked(object sender, EventArgs e)
        {
            dialog1.Destroy();
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
                MessageDialog md = new MessageDialog(owningView.MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                   String.Format("The value for {0} should be a number in the range {1:F2} to {2:F2}", contents, minVal, maxVal));
                md.Title = "Invalid entry";
                md.Run();
                md.Destroy();
            }
            return result;
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
            Soil newSoil = null;
            if (radioAus.Active)
            {
                if (radioSynth.Active)
                    newSoil = GetSyntheticSoil();
                else if (radioMatch.Active)
                    newSoil = GetMatchingSoil();
            }
            else if (radioWorld.Active)
            {
                if (radioISRIC.Active)
                    newSoil = GetISRICSoil();
                else if (radioSoilGrids.Active)
                    newSoil = GetISRICSoilGrids();
            }
            if (newSoil == null)
            {
                MessageDialog md = new MessageDialog(owningView.MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                   "Unable to obtain data for this site");
                md.Title = "Data read error";
                md.Run();
                md.Destroy();
            }
            else
            {
                if (dest is Soil)
                {
                    ReplaceModelCommand command = new ReplaceModelCommand(dest, newSoil, explorerPresenter);
                    explorerPresenter.CommandHistory.Add(command, true);
                }
                else if (dest is Zone)
                {
                    string modelStr = FileFormat.WriteToString(newSoil);
                    AddModelCommand command = new AddModelCommand(replaceNode, modelStr, owningView, explorerPresenter);
                    explorerPresenter.CommandHistory.Add(command, true);
                }
                MessageDialog md = new MessageDialog(owningView.MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                   "Initial values for water and soil nitrogen may not have been provided or may be inaccurate for this soil. " +
                                   "Please set sensible values before using this soil in a simulation.");
                md.Title = "Soil use warning";
                md.Run();
                md.Destroy();
                dialog1.Destroy();
            }
        }

        /// <summary>
        /// Initialises and displays the dialog
        /// </summary>
        /// <param name="soil">The soil object to be replaced</param>
        /// <param name="view">The ExplorerView displaying the soil object in its tree</param>
        /// <param name="nodePath">The path to the soil object within the view's tree</param>
        /// <param name="explorerPresenter">The ExplorerPresenter that is managing all of this</param>
        public void ShowFor(Model dest, ExplorerView view, string nodePath, ExplorerPresenter explorerPresenter)
        {
            this.dest = dest;
            this.replaceNode = nodePath;
            this.owningView = view;
            this.explorerPresenter = explorerPresenter;
            dialog1.TransientFor = view.MainWidget.Toplevel as Window;
            dialog1.Parent = view.MainWidget.Toplevel;
            dialog1.WindowPosition = WindowPosition.CenterOnParent;
            // Attempt to find an initial latitude and longitude from a Weather model
            IModel weather = Apsim.Find(dest, typeof(Models.Interfaces.IWeather));
            double latitude, longitude;
            if (weather is Weather)
            {
                Weather weatherObj = weather as Weather;
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
        /// Generates a new ApsimX Soil object from an Xml soil description in "classic" APSIM format
        /// </summary>
        /// <param name="soil">The xml "soil" node holding the description</param>
        /// <returns>True if successful</returns>
        private Soil SoilFromApsoil(XmlNode soil)
        {
            Soil soilObj = null;
            try
            {
                XmlDocument soilDoc = new XmlDocument();
                XmlNode rootNode = soilDoc.CreateNode("element", "root", "");
                APSIMImporter importer = new APSIMImporter();
                XmlNode newNode = null;
                newNode = importer.ImportSoil(soil, rootNode, newNode);

                List<Exception> errors = null;
                soilObj = (Soil)FileFormat.ReadFromString<Soil>(newNode.OuterXml, out errors);

                // Looks like we also need a soil temperature model as well....
                Structure.Add(new CERESSoilTemperature(), soilObj);
                soilObj.OnCreated();
            }
            catch (Exception) // Needs better error handling. We should inform the user of any problems.
            {
            }
            return soilObj;
        }

        /// <summary>
        /// Requests a "synthethic" ASPIM soil from the ASRIS web service
        /// </summary>
        /// <returns>True if successful</returns>
        private Soil GetSyntheticSoil()
        {
            if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                return null;
            string url = "http://www.asris.csiro.au/ASRISApi/api/APSIM/getApsoil?longitude=" +
                entryLongitude.Text + "&latitude=" + entryLatitude.Text;
            Soil newSoil = null;
            WaitCursor = true;
            try
            {
                try
                {
                    MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
                    stream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "soil");
                    // We will have either 0 or 1 soil nodes
                    if (soilNodes.Count > 0)
                        newSoil = SoilFromApsoil(soilNodes[0]);
                    return newSoil;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            finally
            {
                WaitCursor = false;
            }
        }

        /// <summary>
        /// Requests the closest APSOIL, using the ASRIS web service
        /// Currently obtains the closest 5, and presents these to the user
        /// </summary>
        /// <returns>True if successful</returns>
        private Soil GetMatchingSoil()
        {
            if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                return null;
            string url = "http://www.asris.csiro.au/ASRISApi/api/APSIM/getClosestApsoil?maxCnt=5&longitude=" +
                entryLongitude.Text + "&latitude=" + entryLatitude.Text;
            Soil newSoil = null;
            WaitCursor = true;
            try
            {
                try
                {
                    MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
                    stream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "soil");
                    // We should have 0 to 5 nodes. If multiple nodes, we should let the user choose
                    if (soilNodes.Count > 0)
                    {
                        int selNode = 0;
                        if (soilNodes.Count > 1)
                            selNode = SelectSoil(soilNodes);
                        if (selNode >= 0)
                            newSoil = SoilFromApsoil(soilNodes[selNode]);
                    }
                    return newSoil;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            finally
            {
                WaitCursor = false;
            }
        }

        Window selWindow;
        Gtk.TreeView soilsView;
        int curSel = -1;

        /// <summary>
        /// Displays a child window with a list of soils, from which the user can make a selection
        /// This should probabaly be re-factored into a separate class
        /// Code largely copied from the intellisense viewer
        /// </summary>
        /// <param name="soilList">The list of soils to display</param>
        /// <returns>The index within the list of the user's selection, or -2 if cancelled</returns>
        private int SelectSoil(List<XmlNode> soilList)
        {
            ListStore soilsModel;
            curSel = -1;
            selWindow = new Window(WindowType.Toplevel)
            {
                HeightRequest = 300,
                WidthRequest = 750,
                Decorated = false,
                SkipPagerHint = true,
                SkipTaskbarHint = true,
                TransientFor = dialog1,
                Parent = dialog1,
                WindowPosition = WindowPosition.CenterOnParent
            };

            Frame selFrame = new Frame();
            selWindow.Add(selFrame);

            ScrolledWindow selScroller = new ScrolledWindow();
            selFrame.Add(selScroller);

            soilsModel = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            soilsView = new Gtk.TreeView(soilsModel);
            selScroller.Add(soilsView);

            TreeViewColumn column = new TreeViewColumn()
            {
                Title = "Name",
                Resizable = true,
            };
            CellRendererText textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 25,
                Ellipsize = Pango.EllipsizeMode.End
            };

            column.PackStart(textRender, true);
            column.SetAttributes(textRender, "text", 0);
            soilsView.AppendColumn(column);

            column = new TreeViewColumn("Apsoil number", textRender, "text", 1)
            {
                Resizable = true
            };
            soilsView.AppendColumn(column);

            column = new TreeViewColumn("Soil type", textRender, "text", 2)
            {
                Resizable = true
            };
            soilsView.AppendColumn(column);

            column = new TreeViewColumn("Distance", textRender, "text", 3)
            {
                Resizable = true
            };
            soilsView.AppendColumn(column);

            soilsView.HasTooltip = true;
            soilsView.TooltipColumn = 4;
            soilsView.ButtonPressEvent += OnButtonPress;
            soilsView.KeyPressEvent += OnSoilListKeyDown;

            soilsModel.Clear();
            foreach (XmlNode node in soilList)
            {
                string name = XmlUtilities.NameAttr(node);
                string number = XmlUtilities.Value(node, "ApsoilNumber");
                string soilType = XmlUtilities.Value(node, "SoilType");
                string distance = XmlUtilities.Value(node, "distanceFromQueryLocation");
                string comments = XmlUtilities.Value(node, "Comments");
                soilsModel.AppendValues(name, number, soilType, distance, comments);
            }

            selWindow.ShowAll();
            while (curSel == -1)
                GLib.MainContext.Iteration();                
            return curSel;
        }

        /// <summary>
        /// Handles the item selected event, by setting the curSel variable to the selection index
        /// </summary>
        private void HandleItemSelected()
        {
            TreeViewColumn col;
            TreePath path;
            soilsView.GetCursor(out path, out col);
            curSel = path.Indices[0];
            selWindow.Destroy();
        }

        /// <summary>
        /// (Mouse) button press event handler. If it is a left mouse double click, selects the item and consumes 
        /// the ItemSelected event.
        /// </summary>
        /// <param name="o">Sender</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnButtonPress(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1)
            {
                HandleItemSelected();
                e.RetVal = true;
            }
        }

        /// <summary>
        /// Key down event handler. If the key is enter, selects current item and consumes the ItemSelected event.
        /// If the key is escape, closes the dialog
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnSoilListKeyDown(object sender, KeyPressEventArgs e)
        {
            // If user clicks ENTER and the context list is visible then insert the currently
            if (e.Event.Key == Gdk.Key.Return && selWindow.Visible)
            {
                HandleItemSelected();
                e.RetVal = true;
            }
            // If the user presses ESC and the context list is visible then close the list.
            else if (e.Event.Key == Gdk.Key.Escape && selWindow.Visible)
            {
                curSel = -2;
                selWindow.Destroy();
                e.RetVal = true;
            }
        }

        /// This alternative approach for obtaining ISRIC soil data need a little bit more work, but is largely complete
        /// There are still bits of the soil organic matter initialisation that should be enhanced.
        /// We probably don't really need two different ways to get to ISRIC data, but it may be interesting to see how the 
        /// two compare. The initial motiviation was what appears to be an order-of-magnitude problem with soil carbon
        /// in the World Modellers version.
        /// <summary>
        /// Gets and ISRIC soil description directly from SoilGrids
        /// </summary>
        /// <returns>True if successful</returns>
        private Soil GetISRICSoilGrids()
        {
            if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                return null;
            string url = "https://rest.soilgrids.org/query?lon=" +
                entryLongitude.Text + "&lat=" + entryLatitude.Text;
            WaitCursor = true;
            Soil newSoil = null;
            try
            {
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
                    double[] textureToAlb = new double[] {     0.12,         0.12,         0.13,        0.13,              0.12,              0.13,   0.13,         0.14,         0.13,   0.13,         0.16,   0.19,     0.13 };
                    double[] textureToCN2 = new double[] {     73.0,         73.0,         73.0,        73.0,              73.0,              73.0,   73.0,         73.0,         68.0,   73.0,         68.0,   68.0,     73.0 };
                    double[] textureToSwcon = new double[] {   0.25,          0.3,          0.3,         0.4,               0.5,               0.5,    0.5,          0.5,          0.6,    0.5,          0.6,   0.75,      0.5 };
                    MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
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

                    newSoil = new Soil(); 
                    Chemical analysis = new Chemical();
                    Physical waterNode = new Physical(); 
                    Organic organicMatter = new Organic();
                    SoilWater soilWater = new SoilWater();
                    InitialWater initialWater = new InitialWater();
                    Sample initialNitrogen = new Sample();
                    SoilNitrogen soilN = new SoilNitrogen();

                    SoilCrop wheat = new SoilCrop();
                    waterNode.Children.Add(wheat);
                    wheat.Name = "WheatSoil";
                    Apsim.ParentAllChildren(waterNode);

                    Model nh4 = new SoilNitrogenNH4();
                    nh4.Name = "NH4";
                    soilN.Children.Add(nh4);
                    Model no3 = new SoilNitrogenNO3();
                    no3.Name = "NO3";
                    soilN.Children.Add(no3);
                    Model urea = new SoilNitrogenUrea();
                    urea.Name = "Urea";
                    soilN.Children.Add(urea);
                    Model plantAvailNH4 = new SoilNitrogenPlantAvailableNH4();
                    plantAvailNH4.Name = "PlantAvailableNH4";
                    soilN.Children.Add(plantAvailNH4);
                    Model plantAvailNO3 = new SoilNitrogenPlantAvailableNO3();
                    plantAvailNO3.Name = "PlantAvailableNO3";
                    soilN.Children.Add(plantAvailNO3);
                    Apsim.ParentAllChildren(soilN);

                    newSoil.Children.Add(waterNode);
                    newSoil.Children.Add(soilWater);
                    newSoil.Children.Add(soilN);
                    newSoil.Children.Add(organicMatter);
                    newSoil.Children.Add(analysis);
                    newSoil.Children.Add(initialWater);
                    newSoil.Children.Add(initialNitrogen);
                    newSoil.Children.Add(new CERESSoilTemperature());
                    Apsim.ParentAllChildren(newSoil);
                    newSoil.OnCreated();

                    newSoil.Name = "Synthetic soil derived from ISRIC SoilGrids REST API";
                    newSoil.DataSource = "ISRIC SoilGrids";
                    newSoil.SoilType = soilType;
                    newSoil.Latitude = Double.Parse(entryLatitude.Text, CultureInfo.InvariantCulture);
                    newSoil.Longitude = Double.Parse(entryLongitude.Text, CultureInfo.InvariantCulture);

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
                    initialWater.PercentMethod = InitialWater.PercentMethodEnum.FilledFromTop;
                    initialWater.FractionFull = 0.0;

                    // Initialise nitrogen to 0.0
                    initialNitrogen.Name = "Initial nitrogen";
                    initialNitrogen.NH4N = new double[layerCount];
                    initialNitrogen.NO3N = new double[layerCount];

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

                    var particleSizeSand = ConvertLayers(sand, layerCount);
                    waterNode.ParticleSizeClay = ConvertLayers(clay, layerCount);
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
                        xf[i] = 1.0 - (waterNode.BD[i] - (p1 + p2 * 0.01 * particleSizeSand[i])) / p3;
                        xf[i] = Math.Max(0.1, Math.Min(1.0, xf[i]));
                        double effectiveThickness = thickness[i] * xf[i];
                        double bottomEffDepth = topEffDepth + effectiveThickness;
                        double propMaxKl = Math.Max(0.0, Math.Min(bottomEffDepth, depthKl) - topEffDepth) / effectiveThickness;
                        double propDecrKl = Math.Max( Math.Max(0.0, Math.Min(bottomEffDepth, depthRoot) - topEffDepth) / effectiveThickness - propMaxKl, 0.0);
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
                        fInert[layer] = Math.Min(0.99, inert_c / ocdrc[layer] );
                    organicMatter.FInert = ConvertLayers(fInert, layerCount); // Not perfect, but should be good enough
                    organicMatter.FBiom = fBiom;
                    organicMatter.FOMCNRatio = 40.0;
                    organicMatter.SoilCNRatio = Enumerable.Repeat(11.0, layerCount).ToArray(); // Is there any good way to estimate this? ISRIC provides no N data

                    return newSoil;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            finally
            {
                WaitCursor = false;
            }
        }

        /// <summary>
        /// Converts data for 7 input levels to layerCount (up to 10) depth ranges
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        private double[] ConvertLayers(double[] inputs, int layerCount)
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
        /// Gets a soil description from the ISRIC REST API for World Modellers
        /// </summary>
        /// <returns>True if successful</returns>
        private Soil GetISRICSoil()
        {
            if (!CheckValue(entryLatitude) || !CheckValue(entryLatitude))
                return null;
            string url = "https://worldmodel.csiro.au/apsimsoil?lon=" +
                entryLongitude.Text + "&lat=" + entryLatitude.Text;
            Soil newSoil = null;
            WaitCursor = true;
            try
            {
                try
                {
                    MemoryStream stream = WebUtilities.ExtractDataFromURL(url);
                    stream.Position = 0;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    List<XmlNode> soilNodes = XmlUtilities.ChildNodesRecursively(doc, "Soil");
                    // We will have either 0 or 1 soil nodes
                    if (soilNodes.Count > 0)
                        newSoil = SoilFromApsoil(soilNodes[0]);
                    return newSoil;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            finally
            {
                WaitCursor = false;
            }
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
                if (dialog1.Toplevel.GdkWindow != null)
                {
                    dialog1.Toplevel.GdkWindow.Cursor = value ? new Gdk.Cursor(Gdk.CursorType.Watch) : null;
                    waiting = value;
                }
            }
        }
    }
}
