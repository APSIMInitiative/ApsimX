namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using EventArguments;
    using APSIM.Interop.Visualisation;
    using Cairo;
    using Gtk;
    using Models.Management;
    using Models;
    using Extensions;
    using Utility;
    using APSIM.Shared.Graphing;
    using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;
    using System.Threading;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class RugPlotView : ViewBase
    {
        private Box vbox1 = null;
        private ListStore stateList = new ListStore(typeof(string));
        private ListStore ruleTree = new ListStore(typeof(string));
        private DateTime earliestDate;
        private Label earliestDateLabel;
        private DateTime lastDate;
        private Label lastDateLabel;
        private DateTime selectedDate;
        private Label selectedDateLabel;

        Button dateminus = null;
        Button dateplus = null;

        DrawingArea rugCanvas;

        int m_DateWidth = 100;
        int m_ColWidth = 100;
        int m_HeaderHeight = 30;
        //int m_DateHeight = 30;

        Color DefaultOutlineColour;
        Color DefaultBackgroundColour;
        
        rotationRugplot rugPlotModel;
        List<string> myPaddocks = null;
        //List<RVPair> myRVs;
        //Dictionary<DateTime, int> myRVIndices;

        /// <summary>
        /// Properties editor.
        /// </summary>
        public IPropertyView PropertiesView { get; private set; }
        public RugPlotView(ViewBase owner = null) : base(owner)
        {
            vbox1 = new VBox(false, 0);
            mainWidget = vbox1;
            mainWidget.Destroyed += OnDestroyed;

            HBox hbox1 = new HBox();

            Label l1 = new Label("Start Date:");
            hbox1.PackStart(l1, false, false, 5 );

            earliestDateLabel = new Label("Some Date Here");
            hbox1.PackStart(earliestDateLabel, false, false, 5);

            Label l3 = new Label("End Date:");   
            hbox1.PackStart(l3, false, false, 5);

            lastDateLabel = new Label("Some Date Here");
            hbox1.PackStart(lastDateLabel, false, false, 5);

            Label l5 = new Label("Selected");
            hbox1.PackStart(l5, false, false, 5);

            selectedDateLabel = new Label("Selected Date Here");
            hbox1.PackStart(selectedDateLabel, false, false, 5);
            
            dateminus = new Button(new Image(Gtk.Stock.Remove, IconSize.Button));
            dateminus.Clicked += onMinusButtonClicked;
            hbox1.PackStart(dateminus, false, false, 5);

            dateplus = new Button(new Image(Gtk.Stock.Add, IconSize.Button));
            dateplus.Clicked += onPlusButtonClicked;
            hbox1.PackStart(dateplus, false, false, 5);

            vbox1.PackStart(hbox1, false, false, 10);

            HPaned hpane2 = new HPaned();

            // the rugplot              
            VBox vbox2a = new VBox();
            rugCanvas = new DrawingArea();
            rugCanvas.AddEvents(
            (int)Gdk.EventMask.PointerMotionMask
            | (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask);


            rugCanvas.Drawn += OnDrawingAreaExpose;

            rugCanvas.ButtonPressEvent += OnMouseButtonPress;

            ScrolledWindow scroller = new ScrolledWindow()
            {
                HscrollbarPolicy = PolicyType.Always,
                VscrollbarPolicy = PolicyType.Always
            };


            scroller.Add(rugCanvas);

            rugCanvas.Realized += OnRealized;
            rugCanvas.SizeAllocated += OnRealized;
            vbox2a.PackStart(scroller, true, true, 0);

            // States in upper right
            VBox vbox2b = new VBox();
            VPaned vpane2b = new VPaned();
            
            ScrolledWindow StateLegend = new ScrolledWindow();
            StateLegend.ShadowType = ShadowType.EtchedIn;
            StateLegend.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            //StateLegend.Add((stateList as ViewBase).MainWidget);
            vpane2b.Pack1(StateLegend, true, true );

            // Rule/values lower right
            ScrolledWindow RVTree = new ScrolledWindow();
            RVTree.ShadowType = ShadowType.EtchedIn;
            RVTree.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            //RVTree.Add((CropList as ViewBase).MainWidget);
            vpane2b.Pack2(RVTree, true, true );
            vpane2b.ShowAll();

            hpane2.Pack1(vbox2a, true, true );
            hpane2.Pack2(vbox2b, true, true );

            vbox1.PackEnd(hpane2, true, true , 0);
            vbox1.ShowAll();


            PropertiesView = new PropertyView(this);
            ((ScrolledWindow)((ViewBase)PropertiesView).MainWidget).HscrollbarPolicy = PolicyType.Never;


            //graphView.OnGraphObjectSelected += OnGraphObjectSelected;
            //graphView.OnGraphObjectMoved += OnGraphObjectMoved;
            //combobox1.Changed += OnComboBox1SelectedValueChanged;

            // Ensure the menu is populated
            Select(null);
        }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="model">the model.</param>
        public void SetModel(rotationRugplot model)
        {
            ruleTree.Clear();
            earliestDate = selectedDate = new DateTime(0);
            rugPlotModel = model;

            if (model.RVIndices != null) {
                earliestDate = model.RVIndices.Keys.Min();
                earliestDateLabel.Text = earliestDate.ToString("d MMM yyyy");

                selectedDate = earliestDate;
                selectedDateLabel.Text = selectedDate.ToString("d MMM yyyy");

                lastDate = model.RVIndices.Keys.Max();
                lastDateLabel.Text = lastDate.ToString("d MMM yyyy");
            }
            if (model.Transitions != null)
                myPaddocks = model.Transitions.Select(t => t.paddock).Distinct().ToList();
            
        }

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectName">Name of the object to be selected.</param>
        public void Select(string objectName)
        {
        }

        /// <summary>
        /// Called when the main widget is destroyed.
        /// Need to detach all event handlers to/from native objects
        /// to allow them to be correctly disposed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnDestroyed(object sender, EventArgs args)
        {
            try
            {
                (PropertiesView as ViewBase).Dispose();
                mainWidget.Destroyed -= OnDestroyed;
                dateminus.Clicked -= onMinusButtonClicked;
                dateplus.Clicked -= onPlusButtonClicked;
                rugCanvas.Realized -= OnRealized;
                rugCanvas.SizeAllocated -= OnRealized;

                
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnDrawingAreaExpose(object sender, DrawnArgs args)
        {
            try
            {
                DrawingArea area = (DrawingArea)sender;

                Cairo.Context context = args.Cr;

                DefaultOutlineColour = area.StyleContext.GetColor(StateFlags.Normal).ToColour();
#pragma warning disable 0612
                DefaultBackgroundColour = area.StyleContext.GetBackgroundColor(StateFlags.Normal).ToColour();
#pragma warning restore 0612

                CairoContext drawingContext = new CairoContext(context, rugCanvas);
                Gdk.Rectangle rug = new Gdk.Rectangle(); 
                int baseline;
                rugCanvas.GetAllocatedSize(out rug, out baseline);
                SetupXfrms(rug.Size);
                Draw(drawingContext);

                ((IDisposable)context.GetTarget()).Dispose();
                ((IDisposable)context).Dispose();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        private DateTime t0 ;
        private DateTime t1 ;
        private double xoffset, xscale;
        private double yoffset, yscale;
        /// <summary>
        /// Drawing area has been rendered - make sure it has enough space.
        /// </summary>
        /// <param name="Size">width/heigh of canvas.</param>
        private void SetupXfrms(Gdk.Size Size)
        {
            if (myPaddocks == null) return;
            t0 = new DateTime(earliestDate.Year, 1, 1);
            t1 = new DateTime(lastDate.Year, 12, 31);
            xoffset = m_DateWidth;
            xscale =  Math.Max(0.0, (Size.Width - xoffset)) / myPaddocks.Count;

            yoffset = m_HeaderHeight;
            yscale =  Math.Max(0.0, (Size.Height - yoffset)) / (t1 - t0).Days;

        }

        /// <summary>
        /// Drawing area has been rendered - make sure it has enough space.
        /// </summary>
        /// <param name="drawingContext">The context.</param>
        private void Draw(CairoContext drawingContext)
        {
            DrawLabels(drawingContext);
            DrawRug(drawingContext);
        }

        private void DrawCentredText(CairoContext context, string text, Point point)
        {
            (int left, int top, int width, int height) = context.GetPixelExtents(text, false, false);
            double x = point.X - (width / 2 + left);
            double y = point.Y - (height / 2 + top);
            context.MoveTo(x, y);
            context.SetColour(DefaultOutlineColour);
            //OutlineColour
            context.DrawText(text, false, false);
        }
        private void DrawRectangle(CairoContext context, int x, int y, int w, int h, Color color)
        {
            context.SetColour(color);
            context.DrawFilledRectangle(x,y,w,h );
        }
        
        private void DrawLabels(CairoContext ctx)
        {
            if (myPaddocks != null)
            {
                for (int i = 0; i < myPaddocks.Count; ++i)
                {
                    string paddock = myPaddocks[i];
                    var lMargin = m_DateWidth + m_ColWidth / 2;
                    Point p = new Point(lMargin + i * m_ColWidth, m_HeaderHeight / 2);
                    DrawCentredText(ctx, paddock, p);
                }
            }
            if (earliestDate != lastDate) 
            {
                var nYears = lastDate.Year - earliestDate.Year;
                int yStep = nYears < 10 ? 1 :
                               nYears < 30 ? 5 : 10;
                for (var t = t0; t <= t1; t = t.AddYears(yStep))
                {
                    Point p = new Point((int) (xoffset / 2), (int) (yoffset +  (t - t0).Days * yscale));
                    DrawCentredText(ctx, $"1/1/{t.Year}", p);
                }
            }
        }
        /// <summary>
        /// Draw the rug
        /// </summary>
        private void DrawRug(CairoContext ctx)
        {
            if (myPaddocks != null)
            {
                int column = 0;
                foreach (var paddock in myPaddocks) {
                   var Transitions = rugPlotModel.Transitions.FindAll(x => x.paddock == paddock);
                   if (Transitions.Count <= 1) { continue; }
                   var tStart = Transitions[0].Date;

                   for (var i = 1; i < Transitions.Count; i++) 
                   { 
                       var tEnd = Transitions[i].Date;
                       var x1 = m_DateWidth + (column * m_ColWidth) + i * m_ColWidth;
                       var x2 = m_DateWidth + (column * m_ColWidth) + (i + 1) * m_ColWidth;
                       var y1 = (int) (yoffset +  (tStart - t0).Days * yscale);
                       var y2 = (int) (yoffset +  (tEnd - t0).Days * yscale);
                       // fixme:
                       // var dgn = rugPlotModel.FindAncestor<RotationManager>().Nodes.Find(n => n.Name == Transitions[i].state) as DGNode;
                       //DrawRectangle(ctx, x1, y1, x2-x1, y2-y1, dgn.color);

                       tStart = tEnd;
                   }
                   column++;
                }
            }
        }
        /// <summary>
        /// Drawing area has been rendered - make sure it has enough space.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnRealized(object sender, EventArgs args)
        {
            try
            {
                // CheckSizing();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }


        /// <summary>Mouse button has been pressed</summary>
        private void OnMouseButtonPress(object o, ButtonPressEventArgs args)
        {
            try
            {
                // Get the point clicked by the mouse.
                Point clickPoint = new Point((int)args.Event.X, (int)args.Event.Y);
                
                if (args.Event.Button == 1)
                {
                    //mouseDown = true;

                    // Redraw area.
                    (o as DrawingArea).QueueDraw();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void onPlusButtonClicked( object obj, EventArgs args )
        {
            selectedDate = selectedDate.AddDays(1);
            if (selectedDate > lastDate) selectedDate = lastDate;
            selectedDateLabel.Text = selectedDate.ToString("d MMM yyyy");
        }
        private void onMinusButtonClicked( object obj, EventArgs args )
        {
            selectedDate = selectedDate.AddDays(-1);
            if (selectedDate < earliestDate) selectedDate = earliestDate;
            selectedDateLabel.Text = selectedDate.ToString("d MMM yyyy");
        }

        private void onDateSelected(object source, System.EventArgs args)
        {
            //selectedDate = ???
            System.Console.WriteLine("Fixme ");
        }
    }
}

