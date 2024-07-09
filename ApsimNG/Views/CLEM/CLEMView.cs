using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface;
using UserInterface.Extensions;
using UserInterface.Interfaces;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Views
{
    //duplicate of InputView because we want to place this at the top of our simulation not onto the Datastore

    public class CLEMView : ViewBase, Views.ICLEMView
    {
        private Notebook nbook = null;
        private Dictionary<string, Label> labelDictionary = new Dictionary<string, Label>();
        private Dictionary<string, Viewport> viewportDictionary = new Dictionary<string, Viewport>();
        private bool setupComplete = false;
        private string previousTabLabel = "";

        /// <summary>Invoked when tab selected</summary>
        public event EventHandler<EventArgs> TabSelected;

        public CLEMView(ViewBase owner) : base(owner)
        {
            nbook = new Notebook();
            nbook.SwitchPage += NotebookSwitchPage;
            nbook.CurrentPage = 0;
            mainWidget = nbook;
            setupComplete = true;
        }

        private void NotebookSwitchPage(object o, SwitchPageArgs args)
        {
            try
            {
                if (setupComplete && nbook.CurrentPage >= 0)
                {
                    string selectedLabel = nbook.GetTabLabelText(nbook.GetNthPage(nbook.CurrentPage));
                    TabChangedEventArgs  tabEArgs = new TabChangedEventArgs(selectedLabel);
                    if (TabSelected != null && selectedLabel != previousTabLabel)
                    {
                        TabSelected.Invoke(this, tabEArgs);
                    }
                    previousTabLabel = selectedLabel;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            nbook.SwitchPage -= NotebookSwitchPage;
        }

        public void SelectTabView(string tabName)
        {
            int page = 0;
            for (int i = 0; i < nbook.Children.Count(); i++)
            {
                if(nbook.GetTabLabelText(nbook.GetNthPage(i))==tabName)
                {
                    page = i;
                    break;
                }
            }
            nbook.CurrentPage = page;
        }

        public void AddTabView(string tabName, object control)
        {
            if (labelDictionary.ContainsKey(tabName))
            {
                return;
            }

            Viewport newViewport = new Viewport()
            {
                ShadowType = ShadowType.None,
            };

            Label newLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3,
                Text = tabName
            };

            viewportDictionary.Add(tabName, newViewport);
            labelDictionary.Add(tabName, newLabel);

            if (!nbook.Children.Contains(newViewport))
            {
                nbook.AppendPage(newViewport, newLabel);
            }

            foreach (Widget child in newViewport.Children)
            {
                newViewport.Remove(child);
                child.Dispose();
            }
            if (typeof(ViewBase).IsInstanceOfType(control))
            {
                EventBox frame = new EventBox();

                Box hbox = new Box(Orientation.Horizontal, 0);

                ViewBase view = (ViewBase)control;

                hbox.Add(view.MainWidget);
                view.MainWidget.Expand = true;
                frame.Add(hbox);
                newViewport.Add(frame);

                newViewport.ShowAll();
            }
        }
    }

    interface ICLEMView
    {
        /// <summary>
        /// Adds a new tab view to the display
        /// </summary>
        /// <param name="tabName"></param>
        /// <param name="control"></param>
        void AddTabView(string tabName, object control);

        /// <summary>
        /// selects the tab view to the display
        /// </summary>
        /// <param name="tabName"></param>
        void SelectTabView(string tabName);

        /// <summary>Invoked when tab is selected</summary>
        event EventHandler<EventArgs> TabSelected;
    }
}
