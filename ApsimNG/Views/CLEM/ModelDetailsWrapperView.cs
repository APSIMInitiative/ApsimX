
namespace UserInterface.Views
{
    using EventArguments;
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using APSIM.Shared.Utilities;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Net;
    using System.Globalization;

    /// <summary>
    /// This provides a wrapper view to display model type, description and help link
    /// These are taken from the namespace and Description Attribute
    /// The Explorer presenter will use this wrapper if a Description attributre is present.
    /// </summary>
    public class ModelDetailsWrapperView : ViewBase, IModelDetailsWrapperView
    {
        private HBox hbox = null;
        private VBox vbox1 = null;
        private Label modelTypeLabel = null;
        private Label modelDescriptionLabel = null;
        private LinkButton modelHelpLinkLabel = null;
        private LinkButton modelHelpLinkImg = null;
        private Label modelVersionLabel = null;
        private Viewport bottomView = null;

        public ModelDetailsWrapperView(ViewBase owner) : base(owner)
        {
            hbox = new HBox();
            vbox1 = new VBox();

            modelTypeLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3
            };
            Pango.FontDescription font = new Pango.FontDescription
            {
                Size = Convert.ToInt32(16 * Pango.Scale.PangoScale, CultureInfo.InvariantCulture),
                Weight = Pango.Weight.Semibold
            };
            modelTypeLabel.ModifyFont(font);

            modelDescriptionLabel = new Label()
            {
                Xalign = 0.0f,
                Xpad=4
            };
            modelDescriptionLabel.LineWrapMode = Pango.WrapMode.Word;
            modelDescriptionLabel.Wrap = true;
            modelDescriptionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));

            modelHelpLinkLabel = new LinkButton("", "")
            {
                Xalign = 0.0f,
            };
            modelHelpLinkLabel.Clicked += ModelHelpLinkLabel_Clicked;
            modelHelpLinkLabel.ModifyBase(StateType.Normal, new Gdk.Color(131, 0, 131));
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
                Xpad = 4
            };
            font = new Pango.FontDescription
            {
                Size = Convert.ToInt32(8 * Pango.Scale.PangoScale, CultureInfo.InvariantCulture),
                Weight = Pango.Weight.Normal,
            };
            modelVersionLabel.ModifyFont(font);
            modelVersionLabel.ModifyFg(StateType.Normal, new Gdk.Color(150, 150, 150));
            modelVersionLabel.LineWrapMode = Pango.WrapMode.Word;
            modelVersionLabel.Wrap = true;
            modelVersionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));

            bottomView = new Viewport
            {
                ShadowType = ShadowType.None
            };

            hbox.PackStart(modelTypeLabel, false, true, 0);
            hbox.PackStart(modelHelpLinkLabel, false, false, 0);

            vbox1.PackStart(hbox, false, true, 0);
            vbox1.PackStart(modelTypeLabel, false, true, 0);
            vbox1.PackStart(modelDescriptionLabel, false, true, 0);
            vbox1.PackStart(modelVersionLabel, false, true, 4);
            vbox1.Add(bottomView);
            vbox1.SizeAllocated += Vbox1_SizeAllocated;

            mainWidget = vbox1;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void Hbox_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            modelHelpLinkImg.HeightRequest = 50;
        }

        /// <summary>
        /// We want to wrap the description text within a space that uses all the available width,
        /// but Gtk doesn't make that easy. Here we respond to changes in the size of the enclosing VBox
        /// and adjust the width of the description label accordingly. We use a bit less than the full width
        /// so that Windows can still be reduced in size. See http://blog.borovsak.si/2009/05/wrapping-adn-resizing-gtklabel.html
        /// </summary>
        private void Vbox1_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            modelDescriptionLabel.WidthRequest = args.Allocation.Width - 8;
            modelVersionLabel.WidthRequest = args.Allocation.Width - 8;
        }

        private void ModelHelpLinkLabel_Clicked(object sender, EventArgs e)
        {
            //Check internet connection and choose either local or online help files
            if(ModelHelpURL != "")
            {
                try
                {
                    string helpURL = "";
                    // does offline help exist
                    var directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string offlinePath = Path.Combine(directory, "CLEM/Help");
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
                    System.Diagnostics.Process.Start(helpURL);
                }
                catch(Exception ex)
                {
                    throw new Exception(ex.Message); 
                }
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            modelHelpLinkLabel.Clicked -= ModelHelpLinkLabel_Clicked;
            vbox1.SizeAllocated -= Vbox1_SizeAllocated;
            if (bottomView != null)
            {
                foreach (Widget child in bottomView.Children)
                {
                    bottomView.Remove(child);
                    child.Destroy();
                }
            }
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        public string ModelTypeText
        {
            get { return modelTypeLabel.Text; }
            set { modelTypeLabel.Text = value; }
        }

        public string ModelDescriptionText
        {
            get { return modelDescriptionLabel.Text; }
            set { modelDescriptionLabel.Markup = value; }
        }

        public string ModelVersionText
        {
            get { return modelVersionLabel.Text; }
            set { modelVersionLabel.Markup = value; }
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
                    modelTypeLabel.ModifyFg(StateType.Normal, new Gdk.Color(r, g, b));
                }
            }
        }

        public void AddLowerView(object control)
        {
            foreach (Widget child in bottomView.Children)
            {
                bottomView.Remove(child);
                child.Destroy();
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
