using Gtk;
using System;

namespace UserInterface.Views
{
    /// <summary>
    /// Types of Widget that can be held by a QuadView
    /// </summary>
    public enum WidgetType
    {
        None,
        Text,
        Graph,
        Grid,
        Property,
        Code,
        List,
        Button
    }

    /// <summary>
    /// Positions within a QuadView
    /// </summary>
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
        private double _horizontalSlider = -1;

        private Paned _topPaned;

        private Paned _leftPaned;

        private Paned _rightPaned;

        private ViewBase _topLeft;

        private ViewBase _topRight;

        private ViewBase _bottomLeft;

        private ViewBase _bottomRight;

        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public QuadView(ViewBase owner) : base(owner)
        {
            //Create our main structure of four quads
            _topPaned = new Paned(Orientation.Horizontal);

            _leftPaned = new Paned(Orientation.Vertical);
            _leftPaned.Add1(new ScrolledWindow() {Name = WidgetPosition.TopLeft.ToString()});
            _leftPaned.Add2(new ScrolledWindow() {Name = WidgetPosition.BottomLeft.ToString()});
            _topPaned.Add1(_leftPaned);

            _rightPaned = new Paned(Orientation.Vertical);
            _rightPaned.Add1(new ScrolledWindow() {Name = WidgetPosition.TopRight.ToString()});
            _rightPaned.Add2(new ScrolledWindow() {Name = WidgetPosition.BottomRight.ToString()});
            _topPaned.Add2(_rightPaned);

            mainWidget = _topPaned;
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            //clear all the quads to start with
            RemoveComponent(WidgetPosition.TopLeft);
            RemoveComponent(WidgetPosition.TopRight);
            RemoveComponent(WidgetPosition.BottomLeft);
            RemoveComponent(WidgetPosition.BottomRight);
        }

        /// <summary>Updates the view and sub</summary>
        public void Refresh()
        {
            if (MainWidget.ParentWindow == null)
                return;

            //hide right or left panel if no content on those sides
            int paneWidth = MainWidget.ParentWindow.Width;
            int paneHeight = MainWidget.ParentWindow.Height;
            if (_topLeft == null && _bottomLeft == null)
                _topPaned.Position = 0;
            else if (_topRight == null && _bottomRight == null)
                _topPaned.Position = paneWidth;
            else
            {
                if (_horizontalSlider >= 0)
                    _topPaned.Position = (int)Math.Round(paneWidth * _horizontalSlider);
                else
                    _topPaned.Position = (int)Math.Round(paneWidth * 0.5);
            }

            if (_topLeft == null)
                _leftPaned.Position = 0;
            else if (_bottomLeft == null)
                _leftPaned.Position = paneHeight;
            else
                _leftPaned.Position = (int)Math.Round(paneHeight * 0.5);

            if (_topRight == null)
                _rightPaned.Position = 0;
            else if (_bottomRight == null)
                _rightPaned.Position = paneHeight;
            else
                _rightPaned.Position = (int)Math.Round(paneHeight * 0.5);


            // Position the splitter to give the "Properties" section as much space as it needs, and no more
            ViewBase view = GetView(WidgetType.Property);
            if (view != null)
            {
                WidgetPosition position = WidgetTypeToPosition(WidgetType.Property);
                PropertyView propertyView = view as PropertyView;
                if (propertyView.AnyProperties)
                {
                    propertyView.MainWidget.GetPreferredHeight(out int minHeight, out int natHeight);
                    natHeight += 20;
                    if (position == WidgetPosition.TopLeft && GetView(WidgetPosition.BottomLeft) != null)
                        _leftPaned.Position = natHeight;
                    else if (position == WidgetPosition.TopRight && GetView(WidgetPosition.BottomRight) != null)
                        _rightPaned.Position = natHeight;
                    else if (position == WidgetPosition.BottomLeft && GetView(WidgetPosition.TopLeft) != null)
                        _leftPaned.Position = paneHeight - natHeight;
                    else if (position == WidgetPosition.BottomRight && GetView(WidgetPosition.TopRight) != null)
                        _rightPaned.Position = paneHeight - natHeight;
                }
            }

            view = GetView(WidgetType.Text);
            if (view != null)
            {
                WidgetPosition position = WidgetTypeToPosition(WidgetType.Text);
                MarkdownView markdownView = view as MarkdownView;
                markdownView.Refresh();
                if (!string.IsNullOrEmpty(markdownView.Text))
                {
                    markdownView.MainWidget.GetPreferredHeight(out int minHeight, out int natHeight);
                    natHeight += 20;
                    if (position == WidgetPosition.TopLeft && GetView(WidgetPosition.BottomLeft) != null)
                        _leftPaned.Position = natHeight;
                    else if (position == WidgetPosition.TopRight && GetView(WidgetPosition.BottomRight) != null)
                        _rightPaned.Position = natHeight;
                    else if (position == WidgetPosition.BottomLeft && GetView(WidgetPosition.TopLeft) != null)
                        _leftPaned.Position = paneHeight - natHeight;
                    else if (position == WidgetPosition.BottomRight && GetView(WidgetPosition.TopRight) != null)
                        _rightPaned.Position = paneHeight - natHeight;
                }
            }

            view = GetView(WidgetType.Button);
            if (view != null)
            {
                WidgetPosition position = WidgetTypeToPosition(WidgetType.Button);
                ButtonView buttonView = view as ButtonView;
                buttonView.MainWidget.GetPreferredHeight(out int minHeight, out int natHeight);
                natHeight += 20;
                if (position == WidgetPosition.TopLeft && GetView(WidgetPosition.BottomLeft) != null)
                    _leftPaned.Position = natHeight;
                else if (position == WidgetPosition.TopRight && GetView(WidgetPosition.BottomRight) != null)
                    _rightPaned.Position = natHeight;
                else if (position == WidgetPosition.BottomLeft && GetView(WidgetPosition.TopLeft) != null)
                    _leftPaned.Position = paneHeight - natHeight;
                else if (position == WidgetPosition.BottomRight && GetView(WidgetPosition.TopRight) != null)
                    _rightPaned.Position = paneHeight - natHeight;
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
            else if (type == WidgetType.Code)
            {
                container = this.GetControl<EditorView>(name);
            }
            else if (type == WidgetType.List)
            {
                container = this.GetControl<ExperimentView>(name);
            }
            else if (type == WidgetType.Button)
            {
                container = this.GetControl<ButtonView>(name);
            }

            SetView(container, position);
            return container;
        }

        public void RemoveComponent(WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
            {
                if (_topLeft != null)
                    _topLeft.Dispose();
                _topLeft = null;
            }
            else if (position == WidgetPosition.TopRight)
            {
                if (_topRight != null)
                    _topRight.Dispose();
                _topRight = null;
            }
            else if (position == WidgetPosition.BottomLeft)
            {
                if (_bottomLeft != null)
                    _bottomLeft.Dispose();
                _bottomLeft = null;
            }
            else if (position == WidgetPosition.BottomRight)
            {
                if (_bottomRight != null)
                    _bottomRight.Dispose();
                _bottomRight = null;
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
                return _topLeft;
            else if (PositionToWidgetType(WidgetPosition.TopRight) == type)
                return _topRight;
            else if (PositionToWidgetType(WidgetPosition.BottomLeft) == type)
                return _bottomLeft;
            else if (PositionToWidgetType(WidgetPosition.BottomRight) == type)
                return _bottomRight;
            else
                return null;
        }

        public ViewBase GetView(WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
                return _topLeft;
            else if (position == WidgetPosition.TopRight)
                return _topRight;
            else if (position == WidgetPosition.BottomLeft)
                return _bottomLeft;
            else if (position == WidgetPosition.BottomRight)
                return _bottomRight;
            else
                throw new Exception("QuadView GetView function requires a position, WidgetPosition.Any cannot be used.");
        }

        public void OverrideSlider(double percentage)
        {
            _horizontalSlider = percentage;
        }

        private void SetView(ViewBase view, WidgetPosition position)
        {
            if (position == WidgetPosition.TopLeft)
                _topLeft = view;
            else if (position == WidgetPosition.TopRight)
                _topRight = view;
            else if (position == WidgetPosition.BottomLeft)
                _bottomLeft = view;
            else if (position == WidgetPosition.BottomRight)
                _bottomRight = view;
        }

        private WidgetType PositionToWidgetType(WidgetPosition position)
        {
            ViewBase view = null;
            if (position == WidgetPosition.TopLeft)
                view = _topLeft;
            else if (position == WidgetPosition.TopRight)
                view = _topRight;
            else if (position == WidgetPosition.BottomLeft)
                view = _bottomLeft;
            else if (position == WidgetPosition.BottomRight)
                view = _bottomRight;
            
            if (view is GraphView)
                return WidgetType.Graph;
            else if (view is PropertyView)
                return WidgetType.Property;
            else if (view is MarkdownView)
                return WidgetType.Text;
            else if (view is ContainerView)
                return WidgetType.Grid;
            else if (view is EditorView)
                return WidgetType.Code;
            else if (view is ExperimentView)
                return WidgetType.List;
            else if (view is ButtonView)
                return WidgetType.Button;
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
                if (_topLeft != null)
                    _topLeft.Dispose();
                if (_topRight != null)
                    _topRight.Dispose();
                if (_bottomLeft != null)
                    _bottomLeft.Dispose();
                if (_bottomRight != null)
                    _bottomRight.Dispose();

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