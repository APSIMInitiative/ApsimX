// -----------------------------------------------------------------------
// <copyright file="SupplementPresenter.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using Models.GrazPlan;
    using Views;

    /// <summary>
    /// A presenter class for the supplement model
    /// </summary>
    public class SupplementPresenter : IPresenter
    {
        /// <summary>
        /// The supplement model.
        /// </summary>
        private Supplement supplement;

        /// <summary>
        /// The initial water view;
        /// </summary>
        private ISupplementView supplementView;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The initial supplement model</param>
        /// <param name="view">The supplement view to work with</param>
        /// <param name="explrPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explrPresenter)
        {
            supplement = model as Supplement;
            supplementView = view as SupplementView;
            explorerPresenter = explrPresenter;

            ConnectViewEvents();
            PopulateView();

            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model and view from this presenter.
        /// </summary>
        public void Detach()
        {
            DisconnectViewEvents();
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Populate the view object
        /// </summary>
        public void PopulateView()
        {
            DisconnectViewEvents();
            PopulateDefaultNames();
            PopulateSupplementNames();
            if (supplement.NoStores > 1)
            {
                if (supplement.curIndex == 0)
                    supplement.curIndex = 1;
                supplementView.SelectedSupplementValues = supplement[supplement.curIndex];
                supplementView.SelectedSupplementIndex = supplement.curIndex - 1; // Offset by 1 to skip fodder
            }
            ConnectViewEvents();
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
            supplementView.SupplementSelected += OnSupplementSelected;
            supplementView.SupplementAdded += OnSupplementAdded;
            supplementView.SupplementDeleted += OnSupplementDeleted;
            supplementView.SupplementReset += OnSupplementReset;
            supplementView.AllSupplementsReset += OnAllSupplementsReset;
            supplementView.SuppAttrChanged += OnSuppAttrChanged;
            supplementView.SuppNameChanged += OnSuppNameChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            supplementView.SupplementSelected -= OnSupplementSelected;
            supplementView.SupplementAdded -= OnSupplementAdded;
            supplementView.SupplementDeleted -= OnSupplementDeleted;
            supplementView.SupplementReset -= OnSupplementReset;
            supplementView.AllSupplementsReset -= OnAllSupplementsReset;
            supplementView.SuppAttrChanged -= OnSuppAttrChanged;
            supplementView.SuppNameChanged -= OnSuppNameChanged;
        }

        /// <summary>
        /// Select the supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSupplementSelected(object sender, TIntArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.SelectSupplementCommand(supplement, supplement.curIndex, e.value + 1)); // Offset by 1 to skip fodder
        }

        /// <summary>
        /// Add a supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSupplementAdded(object sender, TStringArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.AddSupplementCommand(supplement, e.name));
        }

        /// <summary>
        /// Delete the supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnSupplementDeleted(object sender, System.EventArgs e)
        {
            if (supplement.curIndex > 0) // Don't delete fodder
                this.explorerPresenter.CommandHistory.Add(new Commands.DeleteSupplementCommand(supplement, supplement[supplement.curIndex]));
        }

        /// <summary>
        /// Resets the composition values for the current supplement,
        /// provided its name matches a default supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event agruments</param>
        private void OnSupplementReset(object sender, System.EventArgs e)
        {
            if (supplement.curIndex > 0)  
            {
                // Don't reset fodder
                List<TSupplementItem> suppList = new List<TSupplementItem>(1);
                suppList.Add(supplement[supplement.curIndex]);
                this.explorerPresenter.CommandHistory.Add(new Commands.ResetSupplementCommand(supplement, suppList));
            }
        }

        /// <summary>
        /// Resets the composition values for all supplements which have
        /// a name which matches a default supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnAllSupplementsReset(object sender, System.EventArgs e)
        {
            List<TSupplementItem> suppList = new List<TSupplementItem>(supplement.NoStores - 1);
            for (int i = 1; i < supplement.NoStores; i++)
            {
                suppList.Add(supplement[i]);
                // Don't reset fodder
                // InitSupplement(i, supplement[i].Name);
            }
            this.explorerPresenter.CommandHistory.Add(new Commands.ResetSupplementCommand(supplement, suppList));
        }

        /// <summary>
        /// The supplement attribute has changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSuppAttrChanged(object sender, TSuppAttrArgs e)
        {
            int attr = e.attr;
            if (attr == -2)
            {
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.curIndex], "IsRoughage", e.attrVal != 0.0));
            }
            else if (attr == -1)
            {
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.curIndex], "Amount", e.attrVal));
            }
            else if (attr >= 0)
            {
                string propName = null;
                TSupplement.TSuppAttribute tagEnum = (TSupplement.TSuppAttribute)e.attr;
                switch (tagEnum)
                {
                    case TSupplement.TSuppAttribute.spaDMP:
                        propName = "DM_Propn";
                        break;
                    case TSupplement.TSuppAttribute.spaDMD:
                        propName = "DM_Digestibility";
                        break;
                    case TSupplement.TSuppAttribute.spaMEDM:
                        propName = "ME_2_DM";
                        break;
                    case TSupplement.TSuppAttribute.spaEE:
                        propName = "EtherExtract";
                        break;
                    case TSupplement.TSuppAttribute.spaCP:
                        propName = "CrudeProt";
                        break;
                    case TSupplement.TSuppAttribute.spaDG:
                        propName = "DgProt";
                        break;
                    case TSupplement.TSuppAttribute.spaADIP:
                        propName = "ADIP_2_CP";
                        break;
                    case TSupplement.TSuppAttribute.spaPH:
                        propName = "Phosphorus";
                        break;
                    case TSupplement.TSuppAttribute.spaSU:
                        propName = "Sulphur";
                        break;
                    default:
                        break;
                }
                if (propName != null)
                    this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.curIndex], propName, e.attrVal));
            }
        }

        /// <summary>
        /// Change supplement name
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSuppNameChanged(object sender, TStringArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.curIndex], "Name", e.name));
        }

        /// <summary>
        /// Provide the view with default supplement names.
        /// </summary>
        private void PopulateDefaultNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < TSupplementLibrary.DefaultSuppConsts.Count; i++)
            {
                names.Add(TSupplementLibrary.DefaultSuppConsts[i].Name);
            }

            this.supplementView.DefaultSuppNames = names.ToArray();
        }

        /// <summary>
        /// Populate the view with supplement names.
        /// </summary>
        private void PopulateSupplementNames()
        {
            List<string> names = new List<string>();
            for (int i = 1; i < supplement.NoStores; i++)  
            {
                // SKIP element 0; that's reserved for fodder
                if (string.IsNullOrWhiteSpace(supplement[i].Name))
                {
                    names.Add("Supplement " + i.ToString());
                }
                else
                {
                    names.Add(supplement[i].Name);
                }
            }

            this.supplementView.SupplementNames = names.ToArray();

            if (names.Count > 0 && supplement.curIndex <= supplement.NoStores)
            {
                supplementView.SelectedSupplementValues = supplement[supplement.curIndex];
            }
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == supplement)
            {
                DisconnectViewEvents();
                PopulateSupplementNames();
                if (supplement.curIndex >= 0)
                    supplementView.SelectedSupplementValues = supplement[supplement.curIndex];
                supplementView.SelectedSupplementIndex = supplement.curIndex - 1;  // Offset by 1 to skip fodder
                ConnectViewEvents();
            }
            else if (changedModel is TSupplementItem && supplement.IndexOf(changedModel as TSupplementItem) >= 0)
            {
                supplementView.SelectedSupplementValues = supplement[supplement.curIndex];
            }
        }
    }
}
