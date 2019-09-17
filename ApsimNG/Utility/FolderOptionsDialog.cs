using GLib;
using Gtk;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Presenters;
using UserInterface.Views;

namespace Utility
{
    public class FolderOptionsDialog
    {
        private Folder folder;
        private ExplorerPresenter presenter;

        private PropertyPresenter properties;
        private GridView grid;

        private Window window;
        public FolderOptionsDialog(Folder model, ExplorerPresenter presenter)
        {
            this.folder = model;
            this.presenter = presenter;

            properties = new PropertyPresenter();
            grid = new GridView(presenter.GetView());
            properties.Attach(folder, grid, presenter);

            window = new Window("Folder options");
            window.TransientFor = presenter.GetView().MainWidget.Toplevel as Window;
            window.WidthRequest = 600;
            window.WindowPosition = WindowPosition.CenterAlways;
            window.Add(grid.MainWidget);
            window.Destroyed += OnDestroyed;
            window.ShowAll();
        }

        private void OnDestroyed(object sender, EventArgs e)
        {
            window.Destroyed -= OnDestroyed;
            properties.Detach();
        }
    }
}
