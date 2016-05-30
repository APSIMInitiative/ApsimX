using System;
using System.IO;
using System.Reflection;
using APSIM.Shared.Utilities;
using Glade;
using Gtk;
using UserInterface.Views;


namespace UserInterface
{
    class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Gtk.Application.Init();
            MainForm win = new MainForm(args);
            win.MainWidget.ShowAll();
            Gtk.Application.Run();
        }
    }

    public class ViewBase
    {
        protected ViewBase _owner = null;
        protected Widget _mainWidget = null;
        public ViewBase Owner { get { return _owner; } }
        public Widget MainWidget { get { return _mainWidget; } }
        public ViewBase(ViewBase owner) { _owner = owner; }

        protected Gdk.Window mainWindow { get { return MainWidget == null ? null : MainWidget.Toplevel.GdkWindow; } }
        private bool waiting = false;

        public bool WaitCursor
        {
            get
            {
                return waiting;
            }
            set
            {
                if (mainWindow != null)
                {
                    if (value == true)
                        mainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                    else
                        mainWindow.Cursor = null;
                    while (Gtk.Application.EventsPending())
                        Gtk.Application.RunIteration();
                    waiting = value;
                }
            }
        }
    }

    public class MainForm : ViewBase
    {
        private Presenters.TabbedExplorerPresenter Presenter1;
        private Presenters.TabbedExplorerPresenter Presenter2;
        private string[] commandLineArguments;

        [Widget]
        private new Window mainWindow = null;
        [Widget]
        private HPaned hpaned1 = null;

        /// <summary>
        /// The error message will be set if an error results from a startup script.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm(string[] args) : base(null)
        {
            commandLineArguments = args;

            // Gtk.Settings.Default.SetLongProperty("gtk-button-images", 1, "");
            // Gtk.Settings.Default.ThemeName = "Raleigh";

            // The following code for changing theme settings comes from https://github.com/picoe/Eto/issues/442
            // The XftRgba setting makes a big difference on Windows
            // Get the Global Settings

            //Settings setts = Gtk.Settings.Default;
            // This enables clear text on Win32, makes the text look a lot less crappy
            //setts.XftRgba = "rgb";
            // This enlarges the size of the controls based on the dpi
            //setts.XftDpi = 96;
            // By Default Anti-aliasing is enabled, if you want to disable it for any reason set this value to 0
            // setts.XftAntialias = 0;
            // Enable text hinting
            // setts.XftHinting = 1;
            // setts.XftHintstyle = "hintslight";
            // setts.XftHintstyle = "hintfull";

            // Load the Theme
            // Gtk.CssProvider css_provider = new Gtk.CssProvider();
            // css_provider.LoadFromPath("themes/DeLorean-3.14/gtk-3.0/gtk.css");
            // css_provider.LoadFromPath("themes/DeLorean-Dark-3.14/gtk-3.0/gtk.css");
            // Gtk.StyleContext.AddProviderForScreen(Gdk.Screen.Default, css_provider, 800)

            // Glade can generate files in two different formats: libglade or GtkBuilder. 
            // libglade is the older format; GtkBuilder is intended to eventually replace it.
            // However, in the GtkSharp layer (well, version 2 anyway), the support for
            // GtkBuilder lacks the ability to autoconnect widgets and events contained 
            // in the glade descriptions. The connections must be done manually.
            // In GtkSharp version 3, though, Builder does have an Autoconnect member for
            // event connections.

            // What is the better way to add resources to a .NET assembly?
            // Should we use the .resx mechanism, or just add things and
            // set their Build Action to "Embedded Resource"?
            // Depending on how the resources are added, they are read in 
            // slightly different ways, apparently. So we might instead have:
            // Builder Gui = new Builder();
            // Gui.AddFromString(GTKUserInterface.Properties.Resources.MainForm);

            // Here's how we load the form description using an embedded GtkBuilder file
            // Builder Gui = new Builder("GTKUserInterface.Resources.Glade.MainForm.glade");
            // window1 = (Window)Gui.GetObject("window1");
            // hpaned1 = (HPaned)Gui.GetObject("hpaned1");

            // And here's part of how it works with libglade format.
            // The "[Widget]" attributes are part of the libglade
            // autoconnection stuff, replacing the need for the .GetObject
            // calls used with GtkBuilder.
            //Console.WriteLine("")
            //Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Glade.MainForm.glade");
            //Glade.XML gxmla = new Glade.XML(s, "mainWindow", null);
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.MainForm.glade", "mainWindow");
            gxml.Autoconnect(this);

            _mainWidget = mainWindow;

            mainWindow.Icon = new Gdk.Pixbuf(null, "ApsimNG.Resources.apsim logo32.png");
            Presenter1 = new Presenters.TabbedExplorerPresenter();
            Presenter2 = new Presenters.TabbedExplorerPresenter();
            TabbedExplorerView ExplorerView1 = new TabbedExplorerView(this);
            TabbedExplorerView ExplorerView2 = new TabbedExplorerView(this);
            Presenter1.Attach(ExplorerView1);
            Presenter2.Attach(ExplorerView2);
            hpaned1.Pack1(ExplorerView1.MainWidget, true, true);
            hpaned1.Pack2(ExplorerView2.MainWidget, true, true);
            hpaned1.PositionSet = true;
            hpaned1.Child2.Hide();
            hpaned1.Child2.NoShowAll = true;

            Console.WriteLine("Getting assembly version");
            // Get the version of the current assembly.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Major == 0)
                mainWindow.Title = "APSIM (Custom Build)";
            else
                mainWindow.Title = "APSIM " + version.ToString();

            try
            {
                if (Utility.Configuration.Settings.MainFormMaximized)
                    mainWindow.GdkWindow.Maximize();
                else
                {
                    System.Drawing.Point location = Utility.Configuration.Settings.MainFormLocation;
                    System.Drawing.Size size = Utility.Configuration.Settings.MainFormSize;
                    mainWindow.Move(location.X, location.Y);
                    mainWindow.Resize(size.Width, size.Height);
                }
            }
            catch (System.Exception)
            {
                mainWindow.GdkWindow.Maximize();
            }

            // Look for a script specified on the command line.
            if (commandLineArguments != null && commandLineArguments.Length > 0)
            {
                if (commandLineArguments[0].EndsWith(".cs"))
                {
                    try
                    {
                        ProcessStartupScript(commandLineArguments[0]);
                    }
                    catch (Exception err)
                    {
                        ErrorMessage = err.Message;
                        if (err.InnerException != null)
                            ErrorMessage += "\r\n" + err.InnerException.Message;
                        ErrorMessage += "\r\n" + err.StackTrace;
                        queryClose = false;
                        mainWindow.Destroy();  // Is this right?
                    }
                }
                else if (commandLineArguments[0].EndsWith(".apsimx"))
                {
                    Presenter1.OpenApsimXFileInTab(commandLineArguments[0]);
                }
            }
        }

        public void ToggleSecondExplorerViewVisible()
        {
            if (hpaned1.Child2.Visible)
                hpaned1.Child2.Hide();
            else
            {
                hpaned1.Child2.Show();
                hpaned1.Position = hpaned1.Allocation.Width / 2;
            }
        }

        public bool queryClose = true;

        public void OnMainWindowClose(object o, DeleteEventArgs e)
        {
            if (mainWindow != null)
            {
                int x, y, width, height;
                mainWindow.GetPosition(out x, out y);
                mainWindow.GetSize(out width, out height);
                Gdk.WindowState state = mainWindow.GdkWindow.State;
                Utility.Configuration.Settings.MainFormMaximized = state == Gdk.WindowState.Maximized;
                if (state == 0)
                {
                    Utility.Configuration.Settings.MainFormLocation = new System.Drawing.Point(x, y);
                    Utility.Configuration.Settings.MainFormSize = new System.Drawing.Size(width, height);
                }
                // Then save this for the next time.
                // Is there any easy way to determine whether we've been maximized?   
            }
            bool canClose = (!queryClose) || (Presenter1.AllowClose() && Presenter2.AllowClose());
            if (canClose)
                Application.Quit();
            else
               e.RetVal = true; // Do this to prevent caller from closing the window.
        }

        /// <summary>
        /// User has specified a startup script - execute it.
        /// </summary>
        private void ProcessStartupScript(string fileName)
        {
            StreamReader reader = new StreamReader(fileName);
            string code = reader.ReadToEnd();
            reader.Close();
            Assembly compiledAssembly = ReflectionUtilities.CompileTextToAssembly(code, null);

            // Get the script 'Type' from the compiled assembly.
            Type scriptType = compiledAssembly.GetType("Script");
            if (scriptType == null)
                throw new Exception("Cannot find a public class called 'Script'");

            // Look for a method called Execute
            MethodInfo executeMethod = scriptType.GetMethod("Execute");
            if (executeMethod == null)
                throw new Exception("Cannot find a method Script.Execute");

            // Create a new script model.
            object script = compiledAssembly.CreateInstance("Script");

            // Call Execute on our newly created script instance.
            object[] arguments = new object[] { Presenter1 };
            executeMethod.Invoke(script, arguments);
        }
    }
}