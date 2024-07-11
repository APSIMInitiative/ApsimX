
namespace UserInterface.Views
{
    using Gtk;
    using Interfaces;
    using System;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Globalization;
    using Extensions;
    using System.Runtime.InteropServices;
    using APSIM.Shared.Utilities;
    using System.Reflection;

    /// <summary>
    /// This provides a wrapper view to display model type, description and help link
    /// These are taken from the namespace and Description Attribute
    /// The Explorer presenter will use this wrapper if a Description attributre is present.
    /// </summary>
    /// <remarks>
    /// This styling in here (fonts, backgrounds, ...) needs to be redone for gtk3.
    /// As far as I know, this can (and should) now be done via css.
    /// </remarks>
    public class ModelDetailsWrapperView : ViewBase, IModelDetailsWrapperView
    {
        private Box hbox = null;
        private Box vbox1 = null;
        private Box labels = null;
        private Label modelTypeLabel = null;
        private Label modelDescriptionLabel = null;
        private LinkButton modelHelpLinkLabel = null;
        private Viewport bottomView = null;
        private string modelTypeLabelText;
        private string modelVersionLabelText;
        private string modelTypeColour;

        public ModelDetailsWrapperView(ViewBase owner) : base(owner)
        {
            string css = "";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Style.clem.css"))
            {
                using StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                css = reader.ReadToEnd();
            }

            Gtk.CssProvider css_provider = new CssProvider();
            css_provider.LoadFromData(css);

            hbox = new Box(Orientation.Horizontal, 0);
            vbox1 = new Box(Orientation.Vertical, 0);
            labels = new Box(Orientation.Vertical, 0);

            modelTypeLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3
            };

            hbox.StyleContext.AddProvider(css_provider,Gtk.StyleProviderPriority.Application);
            modelTypeLabel.StyleContext.AddProvider(css_provider,Gtk.StyleProviderPriority.Application);
            modelTypeLabel.StyleContext.AddClass("wrapper_label_type");

            modelDescriptionLabel = new Label()
            {
                Xalign = 0.0f,
                Xpad=4
            };
            modelDescriptionLabel.LineWrapMode = Pango.WrapMode.Word;
            modelDescriptionLabel.Wrap = true;

            modelHelpLinkLabel = new LinkButton("", "")
            {
                Xalign = 0.0f,
            };
            modelHelpLinkLabel.Clicked += ModelHelpLinkLabel_Clicked;

            modelHelpLinkLabel.Visible = false;

            Gtk.CellRendererPixbuf pixbufRender = new CellRendererPixbuf();
            pixbufRender.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Help.svg");
            pixbufRender.Xalign = 0.5f;
            Gtk.Image img = new Image(pixbufRender.Pixbuf);
            modelHelpLinkLabel.Image = img;
            modelHelpLinkLabel.Image.Visible = true;

            bottomView = new Viewport
            {
                ShadowType = ShadowType.None
            };

            hbox.PackStart(modelTypeLabel, false, true, 0);
            hbox.PackStart(modelHelpLinkLabel, false, false, 0);

            labels.PackStart(hbox, false, true, 0);
            modelDescriptionLabel.MarginBottom = 5;

            labels.PackStart(modelDescriptionLabel, false, true, 0);
            vbox1.PackStart(labels, false, true, 0);

            ScrolledWindow scroll = new ScrolledWindow();
            scroll.ShadowType = ShadowType.None;
            scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            scroll.Add(bottomView);
            vbox1.Add(scroll);

            mainWidget = vbox1;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void ModelHelpLinkLabel_Clicked(object sender, EventArgs e)
        {
            //Check internet connection and choose either local or online help files
            try
            {
                if(ModelHelpURL != "")
                {
                    string helpURL = "";
                    // does offline help exist
                    var directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string offlinePath = Path.Combine(directory, "CLEM\\Help");
                    if (File.Exists(Path.Combine(offlinePath, "Default.htm")))
                    {
                        helpURL = "file:///" + offlinePath.Replace(@"\","/") + "/" + ModelHelpURL.TrimStart('/');
                    }
                    // is this application online for online help
                    if(NetworkInterface.GetIsNetworkAvailable())
                    {
                        // set to web address
                        // not currently available during development until web help is launched
                        helpURL = "https://www.apsim.info/clem/" + ModelHelpURL.TrimStart('/');
                    }
                    if (helpURL == "")
                    {
                        helpURL = "https://www.apsim.info";
                    }
                    ProcessUtilities.ProcessStart(helpURL);
                }
            }
            catch(Exception ex)
            {
                ShowError(ex);
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                modelHelpLinkLabel.Clicked -= ModelHelpLinkLabel_Clicked;
                if (bottomView != null)
                {
                    foreach (Widget child in bottomView.Children)
                    {
                        bottomView.Remove(child);
                        child.Dispose();
                    }
                }
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        public string ModelTypeText
        {
            get
            {
                return modelTypeLabelText;
            }
            set
            {
                modelTypeLabelText = value;
                // update markup and include colour if supplied
                modelTypeLabel.Markup = value;
            }
        }

        public string ModelDescriptionText
        {
            get { return modelDescriptionLabel.Text; }
            set { modelDescriptionLabel.Markup = value; }
        }

        public string ModelVersionText
        {
            get
            {
                return modelVersionLabelText;
            }
            set
            {
                modelVersionLabelText = value;
            }
        }

        public string ModelHelpURL
        {
            get { return modelHelpLinkLabel.Uri; }
            set
            {
                modelHelpLinkLabel.Uri = value;
                modelHelpLinkLabel.Visible = (value.ToString() != "");
                modelDescriptionLabel.Ypad = (value.ToString() != "") ? 0 : 4;
            }
        }

        public string ModelTypeTextStyle
        {
            get { return ModelTypeTextStyle; }
            set
            {
            modelTypeLabel.StyleContext.AddClass($"wrapper_label_type_{value}");

                ;
            } 
        }

        public string ModelTypeTextColour
        {
            get { return "N/A"; }
            set
            {
                if (value.Length == 6)
                {
                    byte r = Convert.ToByte(value.Substring(0, 2), 16);
                    byte g = Convert.ToByte(value.Substring(2, 2), 16);
                    byte b = Convert.ToByte(value.Substring(4, 2), 16);

                    modelTypeColour = value;
                    ModelTypeText = ModelTypeText;
                }
            }
        }

        public void AddLowerView(object control)
        {
            foreach (Widget child in bottomView.Children)
            {
                bottomView.Remove(child);
                child.Dispose();
            }
            ViewBase view = control as ViewBase;
            if (view != null)
            {
                bottomView.Add(view.MainWidget);
                bottomView.ShowAll();
            }
        }
    }
}
