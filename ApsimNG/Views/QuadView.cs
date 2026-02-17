using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Presentation;
using Gtk;
using System;
using System.Linq;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public enum WidgetType
    {
        None,
        Text,
        Graph,
        Grid,
        Property
    }

    public enum WidgetPosition
    {
        Any,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public class QuadView : ViewBase
    {
        public IEditorView editorView { get; private set; } = null;

        private Paned topPaned;

        private Paned leftPaned;

        private Paned rightPaned;

        private ViewBase TopLeft;

        private ViewBase TopRight;

        private ViewBase BottomLeft;

        private ViewBase BottomRight;

        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public QuadView(ViewBase owner) : base(owner)
        {
            Builder builder = SetGladeResource("ApsimNG.Resources.Glade.QuadView.glade");
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            topPaned = mainWidget as Paned;
            leftPaned = (Paned)builder.GetObject("left");
            rightPaned = (Paned)builder.GetObject("right");

            ClearQuad(WidgetPosition.TopLeft);
            ClearQuad(WidgetPosition.TopRight);
            ClearQuad(WidgetPosition.BottomLeft);
            ClearQuad(WidgetPosition.BottomRight);
        }

        /// <summary></summary>
        public void Refresh()
        {
            //hide right or left panel if no content on those sides
            int paneWidth = MainWidget.ParentWindow.Width;
            int paneHeight = MainWidget.ParentWindow.Height;
            if (TopLeft == null && BottomLeft == null)
                topPaned.Position = 0;
            else if (TopRight == null && BottomRight == null)
                topPaned.Position = paneWidth;
            else
                topPaned.Position = (int)Math.Round(paneWidth * 0.5);

            if (TopLeft == null)
                leftPaned.Position = 0;
            else if (BottomLeft == null)
                leftPaned.Position = paneHeight;
            else
                leftPaned.Position = (int)Math.Round(paneHeight * 0.5);

            if (TopRight == null)
                rightPaned.Position = 0;
            else if (BottomRight == null)
                rightPaned.Position = paneHeight;
            else
                rightPaned.Position = (int)Math.Round(paneHeight * 0.5);


            // Position the splitter to give the "Properties" section as much space as it needs, and no more
            ViewBase view = GetView(WidgetType.Property);
            if (view != null)
            {
                WidgetPosition position = WidgetTypeToPosition(WidgetType.Property);
                PropertyView propertyView = view as PropertyView;
                if (propertyView.AnyProperties)
                {
                    propertyView.MainWidget.GetPreferredHeight(out int minHeight, out int natHeight);
                    if (position == WidgetPosition.TopLeft)
                        leftPaned.Position = minHeight;
                    else if (position == WidgetPosition.TopRight)
                        rightPaned.Position = minHeight;
                    else if (position == WidgetPosition.BottomLeft)
                        leftPaned.Position = paneHeight - minHeight;
                    else if (position == WidgetPosition.BottomRight)
                        rightPaned.Position = paneHeight - minHeight;
                }
            }

            view = GetView(WidgetType.Text);
            if (view != null)
            {
                WidgetPosition position = WidgetTypeToPosition(WidgetType.Text);
                ContainerView containerView = view as ContainerView;
                if (!string.IsNullOrEmpty(((containerView.Widget.Children.First() as Viewport).Children.First() as Label).Text))
                {
                    containerView.MainWidget.GetPreferredHeight(out int minHeight, out int natHeight);
                    if (position == WidgetPosition.TopLeft)
                        leftPaned.Position = minHeight;
                    else if (position == WidgetPosition.TopRight)
                        rightPaned.Position = minHeight;
                    else if (position == WidgetPosition.BottomLeft)
                        leftPaned.Position = paneHeight - minHeight;
                    else if (position == WidgetPosition.BottomRight)
                        rightPaned.Position = paneHeight - minHeight;
                }
            }
        }

        /// <summary></summary>
        public ViewBase AddComponent(WidgetType type, WidgetPosition position)
        {
            string name = position.ToString();
            ViewBase container = null;
            if (type == WidgetType.Text)
            {
                Gtk.Label label = new Gtk.Label();
                label.Text = "adstwhsadtghjsdgjs";
                //label.Xalign = 0;
                //label.Yalign = 0;
                ContainerView view = this.GetControl<ContainerView>(name);
                view.Widget.Add(label);
                container = view;
            }
            else if (type == WidgetType.Graph)
            {
                container = this.GetControl<GraphView>(name);
            }
            else if (type == WidgetType.Grid)
            {
                container = this.GetControl<ContainerView>(name);
            }
            else if (type == WidgetType.Property)
            {
                container = this.GetControl<PropertyView>(name);
            }

            SetView(container, position);
            Refresh();
            return container;
        }

        public void SetLabelText(string text)
        {
            ContainerView view = GetView(WidgetType.Text) as ContainerView;
            if (view == null)
                throw new Exception("QuadView does not contain a Label");
            else
                ((view.Widget.Children.First() as Viewport).Children.First() as Label).Text = text;
            Refresh();
        }

        public ViewBase GetView(WidgetType type)
        {
            if (PositionToWidgetType(WidgetPosition.TopLeft) == type)
                return TopLeft;
            else if (PositionToWidgetType(WidgetPosition.TopRight) == type)
                return TopRight;
            else if (PositionToWidgetType(WidgetPosition.BottomLeft) == type)
                return BottomLeft;
            else if (PositionToWidgetType(WidgetPosition.BottomRight) == type)
                return BottomRight;
            else
                return null;
        }

        public ViewBase GetView(WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
                return TopLeft;
            else if (position == WidgetPosition.TopRight)
                return TopRight;
            else if (position == WidgetPosition.BottomLeft)
                return BottomLeft;
            else if (position == WidgetPosition.BottomRight)
                return BottomRight;
            else
                throw new Exception("QuadView GetView function requires a position, WidgetPosition.Any cannot be used.");
        }

        private void SetView(ViewBase view, WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
                TopLeft = view;
            else if (position == WidgetPosition.TopRight)
                TopRight = view;
            else if (position == WidgetPosition.BottomLeft)
                BottomLeft = view;
            else if (position == WidgetPosition.BottomRight)
                BottomRight = view;
        }

        private WidgetType PositionToWidgetType(WidgetPosition position)
        {
            ViewBase view = null;
            if (position == WidgetPosition.TopLeft)
                view = TopLeft;
            else if (position == WidgetPosition.TopRight)
                view = TopRight;
            else if (position == WidgetPosition.BottomLeft)
                view = BottomLeft;
            else if (position == WidgetPosition.BottomRight)
                view = BottomRight;
            
            if (view is GraphView)
                return WidgetType.Graph;
            else if (view is PropertyView)
                return WidgetType.Property;
            if (view is ContainerView containerView)
            {
                if (containerView.Widget.Children.Length == 1 && (containerView.Widget.Children.First() as Viewport).Children.First() is Gtk.Label)
                    return WidgetType.Text;
                else
                    return WidgetType.Grid;
            }

            return WidgetType.None;
        }

        private WidgetPosition WidgetTypeToPosition(WidgetType type)
        {
            foreach(WidgetPosition position in Enum.GetValues(typeof(WidgetPosition)))
                if (type == PositionToWidgetType(position))
                    return position;
            
            throw new Exception($"{type} widget not found in QuadView");
        }

        private void ClearQuad(WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
                TopLeft = null;
            else if (position == WidgetPosition.TopRight)
                TopRight = null;
            else if (position == WidgetPosition.BottomLeft)
                BottomLeft = null;
            else if (position == WidgetPosition.BottomRight)
                BottomRight = null;
        }

        /// <summary>Invoked when main widget has been destroyed.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                if (TopLeft != null)
                    TopLeft.Dispose();
                if (TopRight != null)
                    TopRight.Dispose();
                if (BottomLeft != null)
                    BottomLeft.Dispose();
                if (BottomRight != null)
                    BottomRight.Dispose();

                mainWidget.Destroyed -= OnMainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}