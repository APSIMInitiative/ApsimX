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
                if (supplement.CurIndex == 0)
                    supplement.CurIndex = 1;
                supplementView.SelectedSupplementValues = supplement[supplement.CurIndex];
                supplementView.SelectedSupplementIndex = supplement.CurIndex - 1; // Offset by 1 to skip fodder
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
            this.explorerPresenter.CommandHistory.Add(new Commands.SelectSupplementCommand(supplement, supplement.CurIndex, e.Value + 1)); // Offset by 1 to skip fodder
        }

        /// <summary>
        /// Add a supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSupplementAdded(object sender, TStringArgs e)
        {
            try
            {
                this.explorerPresenter.CommandHistory.Add(new Commands.AddSupplementCommand(supplement, e.Name));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Delete the supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnSupplementDeleted(object sender, System.EventArgs e)
        {
            try
            {
                if (supplement.CurIndex > 0) // Don't delete fodder
                    this.explorerPresenter.CommandHistory.Add(new Commands.DeleteSupplementCommand(supplement, supplement[supplement.CurIndex]));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Resets the composition values for the current supplement,
        /// provided its name matches a default supplement
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event agruments</param>
        private void OnSupplementReset(object sender, System.EventArgs e)
        {
            try
            {
                if (supplement.CurIndex > 0)
                {
                    // Don't reset fodder
                    List<SupplementItem> suppList = new List<SupplementItem>(1);
                    suppList.Add(supplement[supplement.CurIndex]);
                    explorerPresenter.CommandHistory.Add(new Commands.ResetSupplementCommand(supplement, suppList));
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
            {
                List<SupplementItem> suppList = new List<SupplementItem>(supplement.NoStores - 1);
                for (int i = 1; i < supplement.NoStores; i++)
                {
                    suppList.Add(supplement[i]);
                    // Don't reset fodder
                    // InitSupplement(i, supplement[i].Name);
                }
                explorerPresenter.CommandHistory.Add(new Commands.ResetSupplementCommand(supplement, suppList));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The supplement attribute has changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSuppAttrChanged(object sender, TSuppAttrArgs e)
        {
            try
            {
                int attr = e.Attr;
                if (attr == -2)
                {
                    explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.CurIndex], "IsRoughage", e.AttrVal != 0.0));
                }
                else if (attr == -1)
                {
                    explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.CurIndex], "Amount", e.AttrVal));
                }
                else if (attr >= 0)
                {
                    string propName = null;
                    FoodSupplement.SuppAttribute tagEnum = (FoodSupplement.SuppAttribute)e.Attr;
                    switch (tagEnum)
                    {
                        case FoodSupplement.SuppAttribute.spaDMP:
                            propName = "DMPropn";
                            break;
                        case FoodSupplement.SuppAttribute.spaDMD:
                            propName = "DMDigestibility";
                            break;
                        case FoodSupplement.SuppAttribute.spaMEDM:
                            propName = "ME2DM";
                            break;
                        case FoodSupplement.SuppAttribute.spaEE:
                            propName = "EtherExtract";
                            break;
                        case FoodSupplement.SuppAttribute.spaCP:
                            propName = "CrudeProt";
                            break;
                        case FoodSupplement.SuppAttribute.spaDG:
                            propName = "DegProt";
                            break;
                        case FoodSupplement.SuppAttribute.spaADIP:
                            propName = "ADIP2CP";
                            break;
                        case FoodSupplement.SuppAttribute.spaPH:
                            propName = "Phosphorus";
                            break;
                        case FoodSupplement.SuppAttribute.spaSU:
                            propName = "Sulphur";
                            break;
                        default:
                            break;
                    }
                    if (propName != null)
                        explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.CurIndex], propName, e.AttrVal));
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Change supplement name
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The arguments</param>
        private void OnSuppNameChanged(object sender, TStringArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(supplement[supplement.CurIndex], "Name", e.Name));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Provide the view with default supplement names.
        /// </summary>
        private void PopulateDefaultNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < SupplementLibrary.DefaultSuppConsts.Count; i++)
            {
                names.Add(SupplementLibrary.DefaultSuppConsts[i].Name);
            }

            supplementView.DefaultSuppNames = names.ToArray();
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

            supplementView.SupplementNames = names.ToArray();

            if (names.Count > 0 && supplement.CurIndex <= supplement.NoStores)
            {
                supplementView.SelectedSupplementValues = supplement[supplement.CurIndex];
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
                if (supplement.CurIndex >= 0)
                    supplementView.SelectedSupplementValues = supplement[supplement.CurIndex];
                supplementView.SelectedSupplementIndex = supplement.CurIndex - 1;  // Offset by 1 to skip fodder
                ConnectViewEvents();
            }
            else if (changedModel is SupplementItem && supplement.IndexOf(changedModel as SupplementItem) >= 0)
            {
                supplementView.SelectedSupplementValues = supplement[supplement.CurIndex];
            }
        }
    }
}
