using ApsimNG.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Presenters
{
    public class TernaryGraphPresenter : IPresenter
    {
        /// <summary>
        /// The model.
        /// </summary>
        private IModel model;

        /// <summary>
        /// The view.
        /// </summary>
        private ITernaryGraphView view;

        /// <summary>
        /// The explorer presenter.
        /// </summary>
        private ExplorerPresenter presenter;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = view as ITernaryGraphView;
            this.presenter = explorerPresenter;
            Initialise();
        }

        public void Detach()
        {
            this.view.Detach();
        }

        private void Initialise()
        {
            this.view.X = 1.0 / 3;
            this.view.Y = 1.0 / 3;
            this.view.Z = 1.0 / 3;
            this.view.Total = 1;

            this.view.Show();
        }
    }
}
