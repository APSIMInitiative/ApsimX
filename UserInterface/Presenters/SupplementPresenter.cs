// -----------------------------------------------------------------------
// <copyright file="SupplementPresenter.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Interfaces;
    using Models;
    using Models.Core;
    using Models.Grazplan;
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
        /// Index of the currently selected supplement.
        /// Index 0 points to the fodder supplement, which we don't want exposed
        /// </summary>
        private int suppIdx; 

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The initial supplement model</param>
        /// <param name="view">The supplement view to work with</param>
        /// <param name="explrPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explrPresenter)
        {
            this.supplement = model as Supplement;
            this.supplementView = view as SupplementView;
            this.explorerPresenter = explrPresenter;

            this.ConnectViewEvents();
            this.PopulateView();

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the model and view from this presenter.
        /// </summary>
        public void Detach()
        {
            this.DisconnectViewEvents();
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
            this.supplementView.SupplementSelected += this.OnSupplementSelected;
            this.supplementView.SupplementAdded += this.OnSupplementAdded;
            this.supplementView.SupplementDeleted += this.OnSupplementDeleted;
            this.supplementView.SupplementReset += this.OnSupplementReset;
            this.supplementView.AllSupplementsReset += this.OnAllSupplementsReset;
            this.supplementView.SuppAttrChanged += this.OnSuppAttrChanged;
            this.supplementView.SuppNameChanged += this.OnSuppNameChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            this.supplementView.SupplementSelected -= this.OnSupplementSelected;
            this.supplementView.SupplementAdded -= this.OnSupplementAdded;
            this.supplementView.SupplementDeleted -= this.OnSupplementDeleted;
            this.supplementView.SupplementReset -= this.OnSupplementReset;
            this.supplementView.AllSupplementsReset -= this.OnAllSupplementsReset;
            this.supplementView.SuppAttrChanged -= this.OnSuppAttrChanged;
            this.supplementView.SuppNameChanged -= this.OnSuppNameChanged;
        }

        /// <summary>
        /// Handles supplement selected events sent from the view
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">
        /// Event argument holding an integer value indicating the
        /// index of the newly selected supplement
        /// </param>
        private void OnSupplementSelected(object sender, TIntArgs e)
        {
            this.suppIdx = e.value + 1;  // Offset by 1 to skip fodder
            this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
        }

        /// <summary>
        /// Handled supplement added events sent from the view
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">Event argument holding a string with the
        /// name of the supplement to be added.
        /// </param>
        private void OnSupplementAdded(object sender, TStringArgs e)
        {
            this.suppIdx = this.supplement.Add(e.name);
            this.PopulateSupplementNames();
            this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
            this.supplementView.SelectedSupplementIndex = this.suppIdx - 1;  // Offset by 1 to skip fodder
        }

        /// <summary>
        /// Handles supplement deleted events sent from the view
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">Event argument (empty; not used)</param>
        private void OnSupplementDeleted(object sender, System.EventArgs e)
        {
            // Don't delete fodder (index 0)
            if (this.suppIdx > 0) 
            {
                this.supplement.Delete(this.suppIdx);
                this.suppIdx = Math.Min(this.suppIdx, this.supplement.NoStores - 1);
                this.PopulateSupplementNames();
                this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
                this.supplementView.SelectedSupplementIndex = this.suppIdx - 1;  // Offset by 1 to skip fodder
            }
        }

        /// <summary>
        /// Resets the composition values for the current supplement,
        /// provided its name matches a default supplement
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">Event argument (empty; not used)</param>
        private void OnSupplementReset(object sender, System.EventArgs e)
        {
            // Don't reset fodder (index 0)
            if (this.suppIdx > 0)  
            {
                this.InitSupplement(this.suppIdx, this.supplement[this.suppIdx].sName);
                this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
            }
        }

        /// <summary>
        /// Resets the composition values for all supplements which have
        /// a name which matches a default supplement
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">Event argument (empty; not used)</param>
        private void OnAllSupplementsReset(object sender, System.EventArgs e)
        {
            // Don't reset fodder, so start with index 1
            for (int i = 1; i < this.supplement.NoStores; i++) 
            {
                this.InitSupplement(i, this.supplement[i].sName);
            }
            if (this.suppIdx > 0)
                this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
        }

        /// <summary>
        /// Initialises an entry in FValues describing a supplement.
        /// The TSupplement elements are set to default values depending
        /// on the name passed to the routine.      
        /// </summary>
        /// <param name="idx">
        /// Index at which to initialise the new supplement.
        /// </param>
        /// <param name="newName">
        /// Name of the new supplement.  If this name matches an entry
        /// in grazSUPP.DefaultSuppConsts, supplement properties are
        /// copied from there; otherwise properties are left unchanged
        /// </param>
        private void InitSupplement(int idx, string newName)
        {
            int suppNo = TSupplementLibrary.DefaultSuppConsts.IndexOf(newName);
            if (suppNo >= 0)
            {
                double oldAmount = this.supplement[idx].Amount;
                this.supplement[idx].Assign(TSupplementLibrary.DefaultSuppConsts[suppNo]);
                this.supplement[idx].sName = newName;
                this.supplement[idx].Amount = oldAmount;
            }
        }

        /// <summary>
        /// Handle an event from the view indicating that the value for some
        /// supplement attribute has changed
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">
        /// Event argument holding a record with an integer value indicating the attribute
        /// that has been changed, and a double value indicating its new value
        /// </param>
        private void OnSuppAttrChanged(object sender, TSuppAttrArgs e)
        {
            int attr = e.attr;
            if (attr == -2)
            {
                this.supplement[this.suppIdx].IsRoughage = e.attrVal != 0.0;
            }
            else if (attr == -1)
            {
                this.supplement[this.suppIdx].Amount = e.attrVal;
            }
            else if (attr >= 0)
            {
                TSupplement.TSuppAttribute tagEnum = (TSupplement.TSuppAttribute)e.attr;
                switch (tagEnum)
                {
                    case TSupplement.TSuppAttribute.spaDMP:
                        this.supplement[this.suppIdx].DM_Propn = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaDMD:
                        this.supplement[this.suppIdx].DM_Digestibility = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaMEDM:
                        this.supplement[this.suppIdx].ME_2_DM = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaEE:
                        this.supplement[this.suppIdx].EtherExtract = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaCP:
                        this.supplement[this.suppIdx].CrudeProt = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaDG:
                        this.supplement[this.suppIdx].DgProt = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaADIP:
                        this.supplement[this.suppIdx].ADIP_2_CP = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaPH:
                        this.supplement[this.suppIdx].Phosphorus = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaSU:
                        this.supplement[this.suppIdx].Sulphur = e.attrVal;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Handling supplement name changed events from the view
        /// </summary>
        /// <param name="sender">The sending object (not used)</param>
        /// <param name="e">
        /// Event argument holding a string which is a new name for the 
        /// currently selected supplement
        /// </param>
        private void OnSuppNameChanged(object sender, TStringArgs e)
        {
            this.supplement[this.suppIdx].sName = e.name;
        }

        /// <summary>
        /// Provide the view with default supplement names.
        /// </summary>
        private void PopulateDefaultNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < TSupplementLibrary.DefaultSuppConsts.Count; i++)
              names.Add(TSupplementLibrary.DefaultSuppConsts[i].sName);
            this.supplementView.DefaultSuppNames = names.ToArray();
        }

        /// <summary>
        /// Populate the view with supplement names.
        /// </summary>
        private void PopulateSupplementNames()
        {
            List<string> names = new List<string>();

            // We skip element 0; that's reserved for fodder
            for (int i = 1; i < this.supplement.NoStores; i++)
            {
                if (string.IsNullOrWhiteSpace(this.supplement[i].sName))
                {
                    names.Add("Supplement " + i.ToString());
                }
                else
                {
                    names.Add(this.supplement[i].sName);
                }
            }
            this.supplementView.SupplementNames = names.ToArray();

            if (names.Count > 0 && this.suppIdx <= this.supplement.NoStores)
            {
                this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
            }
        }

        /// <summary>
        /// Setup up the initial values for the view
        /// </summary>
        private void PopulateView()
        {
            this.suppIdx = 0;
            this.PopulateDefaultNames();
            this.PopulateSupplementNames();
            if (this.supplement.NoStores > 1)
            {
                this.suppIdx = 1;
                this.supplementView.SelectedSupplementValues = this.supplement[this.suppIdx];
                this.supplementView.SelectedSupplementIndex = this.suppIdx - 1; // Offset by 1 to skip fodder
            }
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == this.supplement)
                this.PopulateView();
        }
    }
}
