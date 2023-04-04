using Gtk;
using System;
using System.Collections.Generic;
using System.Reflection;
using UserInterface.Interfaces;
using Utility;

namespace UserInterface.Views
{

    /// <summary>
    /// Encapsulates a toolstrip (button bar)
    /// </summary>
    public class ToolStripView : ViewBase, IToolStripView
    {
        private Toolbar toolStrip = null;
        private AccelGroup accelerators;

        /// <summary>Constructor.</summary>
        public ToolStripView()
        {
        }

        /// <summary>Constructor</summary>
        public ToolStripView(Toolbar toolbar)
        {
            toolStrip = toolbar;
        }

        /// <summary>
        /// A method used when a view is wrapping a gtk control.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="gtkControl">The gtk control being wrapped.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            owner = ownerView;
            toolStrip = (Toolbar) gtkControl;
            toolStrip.Destroyed += OnDestroyed;
        }

        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                toolStrip.Destroyed -= OnDestroyed;
                Destroy();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Destroy the toolstrip</summary>
        public void Destroy()
        {
            if (accelerators != null)
            {
                Window mainWindow = GetMainWindow();
                if (mainWindow != null)
                    mainWindow.RemoveAccelGroup(accelerators);
                accelerators.Dispose();
                accelerators = null;
            }

            foreach (Widget child in toolStrip.Children)
            {
                if (child is ToolButton)
                    child.DetachHandlers();
                toolStrip.Remove(child);
                child.Dispose();
            }
        }

        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Descriptions for each item.</param>
        public void Populate(List<MenuDescriptionArgs> menuDescriptions)
        {
            accelerators = new AccelGroup();
            foreach (Widget child in toolStrip.Children)
            {
                toolStrip.Remove(child);
                child.Dispose();
            }
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                Gtk.Image image = null;
                Gdk.Pixbuf pixbuf = null;
                ManifestResourceInfo info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(description.ResourceNameForImage);

                if (info != null)
                {
                    pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage, 20, 20);
                    image = new Gtk.Image(pixbuf);
                }
                ToolItem item = new ToolItem();
                item.Expand = true;

                if (description.OnClick == null)
                {
                    Label toolbarlabel = new Label();
                    if (description.RightAligned)
                        toolbarlabel.Xalign = 1.0F;
                    toolbarlabel.Xpad = 10;
                    toolbarlabel.Text = description.Name;
                    toolbarlabel.TooltipText = description.ToolTip;
                    toolbarlabel.Visible = !String.IsNullOrEmpty(toolbarlabel.Text);
                    item.Add(toolbarlabel);
                    toolStrip.Add(item);
                    toolStrip.ShowAll();
                }
                else
                {
                    ToolButton button = new ToolButton(image, description.Name);
                    button.Homogeneous = false;
                    button.LabelWidget = new Label(description.Name);
                    button.Clicked += description.OnClick;
                    if (!string.IsNullOrWhiteSpace(description.ShortcutKey))
                    {
                        Gtk.Accelerator.Parse(description.ShortcutKey, out uint key, out Gdk.ModifierType modifier);
                        button.AddAccelerator("clicked", accelerators, key, modifier, AccelFlags.Visible);
                    }
                    item = button;
                }
                toolStrip.Add(item);
            }
            Window mainWindow = GetMainWindow();
            if (mainWindow != null)
                mainWindow.AddAccelGroup(accelerators);
            toolStrip.ShowAll();
        }

        private Window GetMainWindow()
        {
            return ((ViewBase)MasterView).MainWidget.Toplevel as Window;
        }
    }
}
