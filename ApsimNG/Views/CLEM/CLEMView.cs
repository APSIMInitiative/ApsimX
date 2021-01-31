using DocumentFormat.OpenXml.EMMA;
using Gtk;
using Models.CLEM;
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

    public class CLEMView : ViewBase, Views.ICLEMView
    {
        private Notebook nbook = null;

        private Dictionary<string, Label> labelDictionary = new Dictionary<string, Label>();
        private Dictionary<string, Viewport> viewportDictionary = new Dictionary<string, Viewport>();

        /// <summary>Invoked when tab selected</summary>
        public event EventHandler<EventArgs> TabSelected;

        private bool setupComplete = false;

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
                    if (TabSelected != null)
                    {
                        TabSelected.Invoke(this, tabEArgs);
                    }
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
            if(labelDictionary.ContainsKey(tabName))
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

            if (nbook.GetTabLabelText(newViewport) == null)
            {
                nbook.AppendPage(newViewport, newLabel);
            }

            foreach (Widget child in newViewport.Children)
            {
                newViewport.Remove(child);
                child.Cleanup();
            }
            if (typeof(ViewBase).IsInstanceOfType(control))
            {
                EventBox frame = new EventBox();
#if NETFRAMEWORK
                frame.ModifyBg(StateType.Normal, mainWidget.Style.Base(StateType.Normal));
#endif
                HBox hbox = new HBox();
                uint border = 0;
                if (tabName != "Properties" & tabName != "Display" & tabName != "Data")
                {
                    border = 10;
                }

                hbox.BorderWidth = border;

                ViewBase view = (ViewBase)control;

                //if (view is ActivityLedgerGridView)
                //{
                //    hbox.Add(view.MainWidget);
                //}
                //else
                //{
                    hbox.Add(view.MainWidget);
                //}
                frame.Add(hbox);
                newViewport.Add(frame);

                newViewport.ShowAll();
            }
        }
    }

    public class TabChangedEventArgs : EventArgs
    {
        public string TabName { get; set; }

        public TabChangedEventArgs(string myString)
        {
            this.TabName = myString;
        }
    }
}
