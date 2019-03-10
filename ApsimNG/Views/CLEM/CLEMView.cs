using DocumentFormat.OpenXml.EMMA;
using Gtk;
using Models.CLEM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface;
using UserInterface.Interfaces;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Views
{
    //duplicate of InputView because we want to place this at the top of our simulation not onto the Datastore
    interface ICLEMView
    {
        /// <summary>
        /// Property to provide access to the summary view.
        /// </summary>
        Viewport SummaryView { get; }
        /// <summary>
        /// Property to provide access to the messages view.
        /// </summary>
        Viewport MessagesView { get; }
        /// <summary>
        /// Property to provide access to the properties view.
        /// </summary>
        Viewport PropertiesView { get; }
        /// <summary>
        /// Property to provide access to the versions view.
        /// </summary>
        Viewport VersionsView { get; }

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
        private Label summaryLabel = null;
        private Viewport summaryView = null;
        private Label messagesLabel = null;
        private Viewport messagesView = null;
        private Label propertiesLabel = null;
        private Viewport propertiesView = null;
        private Label versionsLabel = null;
        private Viewport versionsView = null;

        /// <summary>Invoked when tab selected</summary>
        public event EventHandler<EventArgs> TabSelected;

        private bool setupComplete = false;

        public CLEMView(ViewBase owner) : base(owner)
        {
            nbook = new Notebook();

            nbook.SwitchPage += NotebookSwitchPage;

            messagesView = new Viewport()
            {
                ShadowType = ShadowType.None,
            };
            messagesLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3,
                Text = "Messages"
            };

            summaryView = new Viewport()
            {
                ShadowType = ShadowType.None,
            };
            summaryLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3,
                Text = "Summary"
            };

            propertiesView = new Viewport()
            {
                ShadowType = ShadowType.None,
            };
            propertiesLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3,
                Text = "Properties"
            };

            versionsView = new Viewport()
            {
                ShadowType = ShadowType.None,
            };
            versionsLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3,
                Text = "Versions"
            };

            nbook.CurrentPage = 0;

            _mainWidget = nbook;
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

        public Viewport SummaryView
        {
            get { return summaryView; }
        }
        public Viewport MessagesView
        {
            get { return messagesView; }
        }
        public Viewport PropertiesView
        {
            get { return propertiesView; }
        }
        public Viewport VersionsView
        {
            get { return versionsView; }
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
            Viewport tab = null;
            Label tablab = null;
            switch (tabName)
            {
                case "Summary":
                    tab = summaryView;
                    tablab = summaryLabel;
                    break;
                case "Messages":
                    tab = messagesView;
                    tablab = messagesLabel;
                    break;
                case "Properties":
                    tab = propertiesView;
                    tablab = propertiesLabel;
                    break;
                case "Version":
                    tab = versionsView;
                    tablab = versionsLabel;
                    break;
                default:
                    throw new Exception(String.Format("Invalid tab name [{0}] used in CLEMView", tabName));
            }

            // check if tab has been added
            if(nbook.GetTabLabelText(tab) == null)
            {
                nbook.AppendPage(tab, tablab);
            }

            foreach (Widget child in tab.Children)
            {
                tab.Remove(child);
                child.Destroy();
            }
            if (typeof(ViewBase).IsInstanceOfType(control))
            {
                ViewBase view = (ViewBase)control;
                tab.Add(view.MainWidget);
                tab.ShowAll();
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
