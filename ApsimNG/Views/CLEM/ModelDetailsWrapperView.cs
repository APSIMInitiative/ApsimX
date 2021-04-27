
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
        private HBox hbox = null;
        private VBox vbox1 = null;
        private VBox labels = null;
        private Label modelTypeLabel = null;
        private Label modelDescriptionLabel = null;
        private LinkButton modelHelpLinkLabel = null;
        private LinkButton modelHelpLinkImg = null;
        private Label modelVersionLabel = null;
        private Viewport bottomView = null;
        private string modelTypeLabelText;
        private string modelVersionLabelText;
        private string modelTypeColour;

        public ModelDetailsWrapperView(ViewBase owner) : base(owner)
        {
            string css = "";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Style.clem.css"))
            {
                using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    css = reader.ReadToEnd();
                }
            }

#if NETCOREAPP3_1
            Gtk.CssProvider css_provider = new CssProvider();
            css_provider.LoadFromData(css);
#endif
            hbox = new HBox();
            vbox1 = new VBox();
            labels = new VBox();

            modelTypeLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3
            };

#if NETCOREAPP3_1
            hbox.StyleContext.AddProvider(css_provider,Gtk.StyleProviderPriority.Application);
            modelTypeLabel.StyleContext.AddProvider(css_provider,Gtk.StyleProviderPriority.Application);
            modelTypeLabel.StyleContext.AddClass("wrapper_label_type");
#endif

            modelDescriptionLabel = new Label()
            {
                Xalign = 0.0f,
                Xpad=4
            };
            modelDescriptionLabel.LineWrapMode = Pango.WrapMode.Word;
            modelDescriptionLabel.Wrap = true;
#if NETFRAMEWORK
            modelDescriptionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));
#endif
            modelHelpLinkLabel = new LinkButton("", "")
            {
                Xalign = 0.0f,
            };
            modelHelpLinkLabel.Clicked += ModelHelpLinkLabel_Clicked;
#if netframework
            modelhelplinklabel.modifybase(statetype.normal, new gdk.color(131, 0, 131));
#endif
            modelHelpLinkLabel.Visible = false;

            Gtk.CellRendererPixbuf pixbufRender = new CellRendererPixbuf();
            pixbufRender.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Help.png");
            pixbufRender.Xalign = 0.5f;
            Gtk.Image img = new Image(pixbufRender.Pixbuf);
            modelHelpLinkLabel.Image = img;
            modelHelpLinkLabel.Image.Visible = true;

            modelVersionLabel = new Label()
            {
                Xalign = 0.0f,
                Xpad = 4,
                UseMarkup = true
            };
#if NETFRAMEWORK
            modelVersionLabel.ModifyFg(StateType.Normal, new Gdk.Color(150, 150, 150));
            modelVersionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));
#endif
            modelVersionLabel.LineWrapMode = Pango.WrapMode.Word;
            modelVersionLabel.Wrap = true;

#if NETFRAMEWORK
            modelDescriptionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));
            modelVersionLabel.ModifyFg(StateType.Normal, new Gdk.Color(150, 150, 150));
            modelVersionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));
#else
            //labels.OverrideBackgroundColor(StateFlags.Normal, new Gdk.RGBA(131, 0, 131,0.5));
#endif

            bottomView = new Viewport
            {
                ShadowType = ShadowType.None
            };

            hbox.PackStart(modelTypeLabel, false, true, 0);
            hbox.PackStart(modelHelpLinkLabel, false, false, 0);

            labels.PackStart(hbox, false, true, 0);
            labels.PackStart(modelDescriptionLabel, false, true, 0);
            labels.PackStart(modelVersionLabel, false, true, 4);


            vbox1.PackStart(labels, false, true, 0);

            ScrolledWindow scroll = new ScrolledWindow();
            scroll.ShadowType = ShadowType.EtchedIn;
            scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
#if NETFRAMEWORK
            scroll.AddWithViewport(bottomView);
#else
            scroll.Add(bottomView);
#endif

            vbox1.Add(scroll);
            vbox1.SizeAllocated += Vbox1_SizeAllocated;

            mainWidget = vbox1;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void Hbox_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            try
            {
                modelHelpLinkImg.HeightRequest = 50;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// We want to wrap the description text within a space that uses all the available width,
        /// but Gtk doesn't make that easy. Here we respond to changes in the size of the enclosing VBox
        /// and adjust the width of the description label accordingly. We use a bit less than the full width
        /// so that Windows can still be reduced in size. See http://blog.borovsak.si/2009/05/wrapping-adn-resizing-gtklabel.html
        /// </summary>
        private void Vbox1_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            try
            {
                modelDescriptionLabel.WidthRequest = args.Allocation.Width - 8;
                modelVersionLabel.WidthRequest = args.Allocation.Width - 8;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
                vbox1.SizeAllocated -= Vbox1_SizeAllocated;
                if (bottomView != null)
                {
                    foreach (Widget child in bottomView.Children)
                    {
                        bottomView.Remove(child);
                        child.Cleanup();
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
#if NETFRAMEWORK
                modelTypeLabel.Markup = $"<span{(((modelTypeColour??"")!="")?$" foreground=\"#{modelTypeColour}\"":"")} size=\"15000\"><b>{value}</b></span>";
#else
                modelTypeLabel.Markup = value;
#endif
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
                modelVersionLabel.Markup = value;
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
#if NETCOREAPP3_1
            modelTypeLabel.StyleContext.AddClass($"wrapper_label_type_{value}");
#endif
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
#if NETFRAMEWORK
                    // gtk tbi
                    modelTypeLabel.ModifyFg(StateType.Normal, new Gdk.Color(r, g, b));
#endif
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
                child.Cleanup();
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
