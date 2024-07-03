namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using EventArguments;
    using Gtk;
    using Models;
    using Models.Core;
    using Models.Management;
    using Extensions;
    using Utility;
    using APSIM.Shared.Graphing;
    using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;
    using Gtk.Sheet;


    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// FIXME - need to allow user to select which simulation to plot (dropdown list of SimulationNames)
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class RugPlotView : ViewBase
    {
        private ListStore stateList = new ListStore(typeof(string));
        private ListStore ruleTree = new ListStore(typeof(string));
        private Entry CurrPName;
        private DateTime earliestDate;
        private DateTime lastDate;
        private DateTime selectedDate;
        private Label selectedDateLabel;
        Gtk.TreeView RVTreeView;
        private Gtk.TreeStore RVTreeModel;

        //private string RVTreePaddockSelected;
        private Gtk.ListStore stateListStore;
        Button dateminus = null;
        Button dateplus = null;

        Button dateminus2 = null;
        Button dateplus2 = null;

        DrawingArea rugCanvas;

        int m_DateWidth = 100;
        int m_ColWidth = 100;
        int m_HeaderHeight = 30;
        
        Color DefaultOutlineColour;
        Color DefaultBackgroundColour;
        
        RotationRugplot rugPlotModel;

        List<string> myPaddocks = null;

        /// <summary>Drop down box which displays the simulation names.</summary>
        public DropDownView SimulationDropDown { get; private set; }
        /// <summary>
        /// If there are no simulation names to choose from, the dropdown box is hidden
        /// </summary>
        private HBox SimChooserBox;

        public RugPlotView(ViewBase owner = null) : base(owner)
        {
            VBox vbox1 =  new VBox(false, 0);
            mainWidget = vbox1;
            mainWidget.Destroyed += OnDestroyed;

            HBox hbox2 = new HBox();
            Label lmpVar = new Label("Current paddock:");
            lmpVar.TooltipText = "If a multipaddock manager is being used, this variable (eg [Manager].Script.currentPaddock) is used to direct which paddock is being considered by the rotation graph.";
            hbox2.PackStart(lmpVar, false, false, 5 );
            CurrPName = new Entry();
            CurrPName.WidthChars = 40;
            hbox2.PackStart(CurrPName, false, false, 5 );

            SimChooserBox = new HBox();
            SimulationDropDown = new DropDownView(this);
            SimChooserBox.PackStart(new Label("Simulation:"), false, false, 5);
            SimChooserBox.PackStart(SimulationDropDown.MainWidget, false, false, 5);
           
            hbox2.PackEnd(SimChooserBox, false, false, 5 );
            vbox1.PackEnd(hbox2, false, false, 5 );

            // the rugplot              
            VBox vbox2a = new VBox();
            rugCanvas = new DrawingArea();
            rugCanvas.AddEvents( (int)Gdk.EventMask.PointerMotionMask
                    | (int)Gdk.EventMask.ButtonPressMask
                    | (int)Gdk.EventMask.ButtonReleaseMask);

            rugCanvas.Drawn += OnDrawingAreaExpose;

            rugCanvas.ButtonPressEvent += OnMouseButtonPress;

            ScrolledWindow scroller = new ScrolledWindow();
            scroller.ShadowType = ShadowType.EtchedIn;
            scroller.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            scroller.Add(rugCanvas);

            rugCanvas.Realized += OnRealized;
            rugCanvas.SizeAllocated += OnRealized;
            vbox2a.PackStart(scroller, true, true, 0);

            // States in upper right
            VPaned vpane2b = new VPaned();
            
            ScrolledWindow StateLegend = new ScrolledWindow();
            StateLegend.ShadowType = ShadowType.EtchedIn;
            StateLegend.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            Gtk.TreeView stateTree = new Gtk.TreeView ();
            var cr = new Gtk.CellRendererText ();
            cr.Alignment = Pango.Alignment.Center;
            var c = stateTree.AppendColumn ("State", cr, "text", 0); 
            c.SetCellDataFunc(cr, new Gtk.TreeCellDataFunc (renderCell));

            stateListStore = new Gtk.ListStore (typeof (string));
            stateTree.Model = stateListStore;
            StateLegend.Add(stateTree);
            vpane2b.Pack1(StateLegend, true, true );

            VBox LRVBox = new VBox();
            HBox hbox1 = new HBox();
            Label l5 = new Label("Selected");
            hbox1.PackStart(l5, false, false, 25);

            dateminus2 = new Button(new Image(Gtk.Stock.ZoomOut, IconSize.Button));
            dateminus2.Clicked += onMinus2ButtonClicked;
            dateminus2.TooltipText = "Go back 7 days";
            hbox1.PackStart(dateminus2, false, false, 5);

            dateminus = new Button(new Image(Gtk.Stock.Remove, IconSize.Button));
            dateminus.Clicked += onMinusButtonClicked;
            dateminus.TooltipText = "Go back 1 day";
            hbox1.PackStart(dateminus, false, false, 5);

            selectedDateLabel = new Label("Selected Date Here");
            hbox1.PackStart(selectedDateLabel, false, false, 5);
            
            dateplus = new Button(new Image(Gtk.Stock.Add, IconSize.Button));
            dateplus.Clicked += onPlusButtonClicked;
            dateplus.TooltipText = "Go forward 1 day";
            hbox1.PackStart(dateplus, false, false, 5);

            dateplus2 = new Button(new Image(Gtk.Stock.ZoomIn, IconSize.Button));
            dateplus2.Clicked += onPlus2ButtonClicked;
            dateplus2.TooltipText = "Go forward 7 days";
            hbox1.PackStart(dateplus2, false, false, 5);

            hbox1.PackStart(dateplus, false, false, 5);
            LRVBox.PackStart(hbox1, false, false, 0);

            // Rule/values lower right
            ScrolledWindow RVTree = new ScrolledWindow();
            RVTree.ShadowType = ShadowType.EtchedIn;
            RVTree.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            RVTreeView = new Gtk.TreeView ();

            var cr2 = new Gtk.CellRendererText ();
            var c2 = RVTreeView.AppendColumn ("Paddock", cr2, "text", 0);
            c2.SetCellDataFunc(cr2, new Gtk.TreeCellDataFunc (renderCell));
            c2.Resizable = true;

            var cr3 = new Gtk.CellRendererText ();
            var c3 = RVTreeView.AppendColumn ("Target", cr3, "text", 1);
            c3.SetCellDataFunc(cr3, new Gtk.TreeCellDataFunc (renderCell));
            c3.Resizable = true;

            var c4 = RVTreeView.AppendColumn ("Rule", new Gtk.CellRendererText (), "text", 2);
            c4.Resizable = true;

            RVTreeModel = new TreeStore (typeof(string), typeof(string), typeof(string));
            RVTreeView.Model = RVTreeModel;
            RVTree.Add(RVTreeView);

            LRVBox.PackStart(RVTree, true, true, 0);
            vpane2b.Pack2(LRVBox, true, true );
            vpane2b.ShowAll();

            HPaned hpane2 = new HPaned();
            hpane2.Pack1(vbox2a, true, true );
            hpane2.Pack2(vpane2b, false, false );

            vbox1.PackEnd(hpane2, true, true , 0);
            vbox1.ShowAll();
        }

        /// <summary>
        /// Set up the colour mappings of the rigfht hand lists. Each column has a differnt encoding
        /// </summary>
        /// <param name="column"></param>
        /// <param name="cell"></param>
        /// <param name="model"></param>
        /// <param name="iter"></param>
        private void renderCell (Gtk.TreeViewColumn column, Gtk.CellRenderer cell,
                                      Gtk.ITreeModel model, Gtk.TreeIter iter)
        {
            if (column.Title == "Paddock") {
                var cellContents = (string) model.GetValue (iter, 0);
                double dblValue;
                if (Double.TryParse(cellContents, out dblValue)) 
                {
                    if (dblValue <= 0) 
                       (cell as Gtk.CellRendererText).Background = "red";
                    else
                       (cell as Gtk.CellRendererText).Background = "green";
                } else 
                    (cell as Gtk.CellRendererText).BackgroundRgba = DefaultBackgroundColour.ToRGBA();
            } 
            else if (column.Title == "Target") 
            {
                var state = (string) model.GetValue (iter, 1);
                var dgn = rugPlotModel.FindAncestor<RotationManager>().Nodes.
                       Find(n => n.Name == state) as APSIM.Shared.Graphing.Node;
                if (dgn != null) {
                    (cell as Gtk.CellRendererText).BackgroundRgba  = dgn.Colour.ToRGBA(); 
                } else {
                    (cell as Gtk.CellRendererText).BackgroundRgba = DefaultBackgroundColour.ToRGBA();
                    (cell as Gtk.CellRendererText).Text = "";
                }
            }
            else if (column.Title == "State") 
            {
                var state = (string) model.GetValue (iter, 0);
                var dgn = rugPlotModel.FindAncestor<RotationManager>().Nodes.
                       Find(n => n.Name == state) as APSIM.Shared.Graphing.Node;
                if (dgn != null) {
                    (cell as Gtk.CellRendererText).BackgroundRgba = dgn.Colour.ToRGBA(); 
                } else {
                    (cell as Gtk.CellRendererText).BackgroundRgba = DefaultBackgroundColour.ToRGBA();
                }
            }
            else
                throw new Exception("Don't know about column " + column.Title);
        }
        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="model">the model.</param>
        /// <param name="setSimName">Whether to tell the model to load data from the simulation name we're displaying.</param>
        public void SetModel(RotationRugplot model, bool setSimName)
        {
            ruleTree.Clear();
            earliestDate = selectedDate = new DateTime(0);
            rugPlotModel = model;

            if (model.RVIndices != null) {
                earliestDate = model.RVIndices.Keys.Min();

                selectedDate = earliestDate;
                selectedDateLabel.Text = selectedDate.ToString("dd MMM yyyy");

                lastDate = model.RVIndices.Keys.Max();
            }
            CurrPName.Text = model.CurrentPaddockString;
            CurrPName.Changed += OnCurrPNameChanged;

            if (model.Transitions != null)
                myPaddocks = model.Transitions.Select(t => t.paddock).Distinct().ToList();

            // Multipaddock simulations always have an "empty" toplevel.
            if (myPaddocks.Count > 1) {
               myPaddocks.RemoveAll(s => s == "");
               myPaddocks.RemoveAll(s => s == null);
            }

            SimulationDropDown.Values = model.GetSimulationNames();
            if (SimulationDropDown.Values.Length > 1) 
               EnableMultipleSims();
            else
               DisableMultipleSims();
               
            if (setSimName) 
               SimulationDropDown.SelectedValue = model.SimulationName;

            RVTreeModel.Clear();
            foreach (var p in myPaddocks) {
               RVTreeModel.AppendValues (new string [] {p, " ", " "});
            }

            stateListStore.Clear();
            foreach (var node in rugPlotModel.FindAncestor<RotationManager>().Nodes) {
               stateListStore.AppendValues (node.Name);
            }
            setDateTo ("", earliestDate);
        }
        /// <summary></summary>
        public void EnableMultipleSims() 
        {
            SimChooserBox.Visible = true;
        }
        /// <summary></summary>
        public void DisableMultipleSims() 
        {
            SimChooserBox.Visible = false;
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
                mainWidget.Destroyed -= OnDestroyed;
                dateminus.Clicked -= onMinusButtonClicked;
                dateplus.Clicked -= onPlusButtonClicked;
                rugCanvas.Realized -= OnRealized;
                rugCanvas.SizeAllocated -= OnRealized;
                CurrPName.Changed -= OnCurrPNameChanged;
                mainWidget.Dispose();
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
            if (myPaddocks?.Count > 0) {
               DrawLabels(drawingContext);
               DrawRug(drawingContext);
               DrawSelectedDate(drawingContext);
            }
        }

        private void DrawCentredText(CairoContext context, string text, Point point)
        {
            (int left, int top, int width, int height) = context.GetPixelExtents(text, false, false);
            double x = point.X - (width / 2 + left);
            double y = point.Y - (height / 2 + top);
            context.MoveTo(x, y);
            context.SetColour(DefaultOutlineColour);
            context.DrawText(text, false, false);
        }
        private void DrawRectangle(CairoContext context, int x, int y, int w, int h, Color color)
        {
            //context.SetColour(color);
            //context.DrawFilledRectangle(x,y,w,h);
            //context.Fill();

            context.SetColour(color);
            context.SetLineWidth(0);
            context.NewPath();
            context.Rectangle(new System.Drawing.Rectangle(x,y,w,h));
            context.StrokePreserve();
            context.Fill();

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

                   int x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                   for (var i = 1; i < Transitions.Count; i++) 
                   { 
                       var tEnd = Transitions[i].Date;
                       x1 = m_DateWidth + column * m_ColWidth;
                       x2 = m_DateWidth + (column + 1) * m_ColWidth;
                       y1 = (int) (yoffset +  (tStart - t0).Days * yscale);
                       y2 = (int) (yoffset +  (tEnd - t0).Days * yscale);
                       // transition is logged at the end of a phase, so the colour of the rect is the preceding state
                       var dgn = rugPlotModel.FindAncestor<RotationManager>().Nodes.
                                 Find(n => n.Name == Transitions[i - 1].state) as APSIM.Shared.Graphing.Node;
                       if(dgn != null) 
                          DrawRectangle(ctx, x1, y1, x2-x1, y2-y1, dgn.Colour);
                       tStart = tEnd;
                   }
                   DrawRectangle(ctx, x1, y2, x2-x1, (int) (yoffset +  (t1 - t0).Days * yscale) - y2, 
                              (rugPlotModel.FindAncestor<RotationManager>().Nodes.
                               Find(n => n.Name == Transitions.Last().state) as APSIM.Shared.Graphing.Node).Colour);

                   column++;
                }
            }
        }
        private void DrawSelectedDate(CairoContext context)
        {
            context.SetColour(DefaultOutlineColour);
            context.SetLineWidth(3);
            context.NewPath();
            context.MoveTo( m_DateWidth, (int) (yoffset +  (selectedDate - t0).Days * yscale) );
            context.LineTo(m_DateWidth + myPaddocks.Count * m_ColWidth, 
                            (int) (yoffset +  (selectedDate - t0).Days * yscale));
            context.StrokePreserve();
            context.Fill();
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
                if (args.Event.Button == 1)
                {
                    // Get the point clicked by the mouse.
                    Point clickPoint = new Point((int)args.Event.X, (int)args.Event.Y);

                    var column = Convert.ToInt32(Math.Floor((args.Event.X - m_DateWidth) / m_ColWidth));
                    column = Math.Max(0, Math.Min(myPaddocks.Count - 1, column));
                    var days = (args.Event.Y - yoffset) / yscale;
                    days = Math.Max(0, Math.Min(days, (t1 - t0).Days));

                    setDateTo (myPaddocks[column], t0.AddDays(days));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        private void setDateTo (string newPaddock, DateTime t) {
           selectedDate = t.Date;
           if (selectedDate > lastDate) selectedDate = lastDate;
           if (selectedDate < earliestDate) selectedDate = earliestDate;
           selectedDateLabel.Text = selectedDate.ToString("dd MMM yyyy");

           Gtk.TreeIter iter ;
           foreach (var p in myPaddocks) {
               //RVTreeModel.GetIterFirst(out iter);
               var thisPath = new Gtk.TreePath(new[]{myPaddocks.IndexOf(p)});
               iter = new TreeIter();

               // store whether the row is currently active, or the user has clicked this paddock on the rug 
               bool isExpanded = RVTreeView.GetRowExpanded(thisPath);
               if (newPaddock != "") { isExpanded = newPaddock == p; }

               // remove children
               do {
                  RVTreeModel.GetIter(out iter, new TreePath(new[]{myPaddocks.IndexOf(p), 0 }));
                  if (RVTreeModel.IterIsValid(iter)) 
                     RVTreeModel.Remove (ref iter);
               } while (RVTreeModel.IterIsValid(iter));

               // Add new children
               RVTreeModel.GetIter(out iter, thisPath);
               if (rugPlotModel.RVIndices.ContainsKey(selectedDate)) 
               {
                  var ruleMap = rugPlotModel.ruleHashes.ToDictionary(x => x.Value, x => x.Key);
                  var targetMap = rugPlotModel.targetHashes.ToDictionary(x => x.Value, x => x.Key);
                  var idx = rugPlotModel.RVIndices[selectedDate];
                  while (idx < rugPlotModel.RVPs.Count &&
                         rugPlotModel.RVPs[idx].Date.Date == selectedDate.Date)
                  {
                      if (rugPlotModel.RVPs[idx].paddock == p) 
                      {
                           RVTreeModel.AppendValues (iter, 
                                                     new string[] {
                                                        rugPlotModel.RVPs[idx].value.ToString(), 
                                                        targetMap [ rugPlotModel.RVPs[idx].target ],
                                                        ruleMap[ rugPlotModel.RVPs[idx].rule ] } );
                      }
                      idx++;
                  }
               }
               if (isExpanded) {
                  RVTreeView.ExpandRow(thisPath, false);
               } else {
                  RVTreeView.CollapseRow(thisPath);
               }
           }
        }

        private void onPlusButtonClicked( object obj, EventArgs args )
        {
            setDateTo ("", selectedDate.AddDays(1));
        }
        private void onPlus2ButtonClicked( object obj, EventArgs args )
        {
            setDateTo ("", selectedDate.AddDays(7));
        }
        private void onMinusButtonClicked( object obj, EventArgs args )
        {
            setDateTo ("", selectedDate.AddDays(-1));
        }
        private void onMinus2ButtonClicked( object obj, EventArgs args )
        {
            setDateTo ("", selectedDate.AddDays(-7));
        }
        private void OnCurrPNameChanged (object sender, EventArgs e) 
        {
            rugPlotModel.CurrentPaddockString = (sender as Gtk.Entry)?.Text;
        }
    }
}

