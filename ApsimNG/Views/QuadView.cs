using Gtk;
using System;
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

        private double horizontalSlider = -1;

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
            topPaned = new Paned(Orientation.Horizontal);

            leftPaned = new Paned(Orientation.Vertical);
            leftPaned.Add1(new ScrolledWindow() {Name = WidgetPosition.TopLeft.ToString()});
            leftPaned.Add2(new ScrolledWindow() {Name = WidgetPosition.BottomLeft.ToString()});
            topPaned.Add1(leftPaned);

            rightPaned = new Paned(Orientation.Vertical);
            rightPaned.Add1(new ScrolledWindow() {Name = WidgetPosition.TopRight.ToString()});
            rightPaned.Add2(new ScrolledWindow() {Name = WidgetPosition.BottomRight.ToString()});
            topPaned.Add2(rightPaned);

            mainWidget = topPaned;
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            RemoveComponent(WidgetPosition.TopLeft);
            RemoveComponent(WidgetPosition.TopRight);
            RemoveComponent(WidgetPosition.BottomLeft);
            RemoveComponent(WidgetPosition.BottomRight);
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
            {
                if (horizontalSlider >= 0)
                    topPaned.Position = (int)Math.Round(paneWidth * horizontalSlider);
                else
                    topPaned.Position = (int)Math.Round(paneWidth * 0.5);
            }

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
                        leftPaned.Position = natHeight;
                    else if (position == WidgetPosition.TopRight)
                        rightPaned.Position = natHeight;
                    else if (position == WidgetPosition.BottomLeft)
                        leftPaned.Position = paneHeight - natHeight;
                    else if (position == WidgetPosition.BottomRight)
                        rightPaned.Position = paneHeight - natHeight;
                }
            }

            view = GetView(WidgetType.Text);
            if (view != null)
            {
                WidgetPosition position = WidgetTypeToPosition(WidgetType.Text);
                MarkdownView markdownView = view as MarkdownView;
                if (!string.IsNullOrEmpty(markdownView.Text))
                {
                    markdownView.MainWidget.GetPreferredHeight(out int minHeight, out int natHeight);
                    if (position == WidgetPosition.TopLeft)
                        leftPaned.Position = natHeight;
                    else if (position == WidgetPosition.TopRight)
                        rightPaned.Position = natHeight;
                    else if (position == WidgetPosition.BottomLeft)
                        leftPaned.Position = paneHeight - natHeight;
                    else if (position == WidgetPosition.BottomRight)
                        rightPaned.Position = paneHeight - natHeight;
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
                container = this.GetControl<MarkdownView>(name);
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
            return container;
        }

        public void RemoveComponent(WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
            {
                if (TopLeft != null)
                    TopLeft.Dispose();
                TopLeft = null;
            }
            else if (position == WidgetPosition.TopRight)
            {
                if (TopRight != null)
                    TopRight.Dispose();
                TopRight = null;
            }
            else if (position == WidgetPosition.BottomLeft)
            {
                if (BottomLeft != null)
                    BottomLeft.Dispose();
                BottomLeft = null;
            }
            else if (position == WidgetPosition.BottomRight)
            {
                if (BottomRight != null)
                    BottomRight.Dispose();
                BottomRight = null;
            }
        }

        public void SetLabelText(string text)
        {
            MarkdownView view = GetView(WidgetType.Text) as MarkdownView;
            if (view == null)
                throw new Exception("QuadView does not contain a Label");
            else
                view.Text = text;
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

        public void OverrideSlider(double percentage)
        {
            horizontalSlider = percentage;
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
            else if (view is MarkdownView)
                return WidgetType.Text;
            else if (view is ContainerView containerView)
                return WidgetType.Grid;
            else
                return WidgetType.None;
        }

        private WidgetPosition WidgetTypeToPosition(WidgetType type)
        {
            foreach(WidgetPosition position in Enum.GetValues(typeof(WidgetPosition)))
                if (type == PositionToWidgetType(position))
                    return position;
            
            throw new Exception($"{type} widget not found in QuadView");
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