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
    using System.Text;
    using Views;
    using Models;
    using Models.Core;
    using Models.Grazplan;
    using System.Reflection;
    using Interfaces;

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

        private int suppIdx;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The initial supplement model</param>
        /// <param name="view">The supplement view to work with</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
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

        private void OnSupplementSelected(object sender, TIntArgs e)
        {
            suppIdx = e.value + 1;
            supplementView.SelectedSupplementValues = supplement[suppIdx];
        }

        private void OnSupplementAdded(object sender, TStringArgs e)
        {
            suppIdx = supplement.Add(e.name);
            PopulateSupplementNames();
            supplementView.SelectedSupplementValues = supplement[suppIdx];
            supplementView.SelectedSupplementIndex = suppIdx - 1;
        }

        private void OnSupplementDeleted(object sender, System.EventArgs e)
        {
            if (suppIdx > 0)
            {
                supplement.Delete(suppIdx);
                suppIdx = Math.Min(suppIdx, supplement.NoStores - 1);
                PopulateSupplementNames();
                supplementView.SelectedSupplementValues = supplement[suppIdx];
                supplementView.SelectedSupplementIndex = suppIdx - 1;
            }
        }

        /// <summary>
        /// Resets the composition values for the current supplement,
        /// provided its name matches a default supplement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSupplementReset(object sender, System.EventArgs e)
        {
            if (suppIdx >= 0)
            {
                InitSupplement(suppIdx, supplement[suppIdx].sName);
                supplementView.SelectedSupplementValues = supplement[suppIdx];
            }
        }

        /// <summary>
        /// Resets the composition values for all supplements which have
        /// a name which matches a default supplement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAllSupplementsReset(object sender, System.EventArgs e)
        {
            for (int i = 0; i < supplement.NoStores; i++)
            {
                InitSupplement(i, supplement[i].sName);
            }
            supplementView.SelectedSupplementValues = supplement[suppIdx];
        }

        /// <summary>
        /// Initialises an entry in FValues describing a supplement. The Amount field is set to
        /// zero (why???), while the TSupplement elements are set to default values depending
        /// on the name passed to the routine.      
        /// </summary>
        /// <param name="idx">
        /// Index at which to initialise the new supplement.
        ///</param>
        /// <param name="sNewName">
        /// Name of the new supplement.  If this name matches an entry
        /// in grazSUPP.DefaultSuppConsts, supplement properties are
        /// copied from there; otherwise properties are left unchanged
        /// </param>
        private void InitSupplement(int idx, string sNewName)
        {
            int iSuppNo = TSupplementLibrary.DefaultSuppConsts.IndexOf(sNewName);
            if (iSuppNo >= 0)
            {
                supplement[idx].Assign(TSupplementLibrary.DefaultSuppConsts[iSuppNo]);
                supplement[idx].sName = sNewName;
                //supplement[idx].Amount = 0.0;
            }
        }

        private void OnSuppAttrChanged(object sender, TSuppAttrArgs e)
        {
            int attr = e.attr;
            if (attr == -2)
            {
                supplement[suppIdx].IsRoughage = e.attrVal != 0.0;
            }
            else if (attr == -1)
            {
                supplement[suppIdx].Amount = e.attrVal;
            }
            else if (attr < 0)
            {
                TSupplement.TSuppAttribute tagEnum = (TSupplement.TSuppAttribute)e.attr;
                switch (tagEnum)
                {
                    case TSupplement.TSuppAttribute.spaDMP:
                        supplement[suppIdx].DM_Propn = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaDMD:
                        supplement[suppIdx].DM_Digestibility = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaMEDM:
                        supplement[suppIdx].ME_2_DM = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaEE:
                        supplement[suppIdx].EtherExtract = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaCP:
                        supplement[suppIdx].CrudeProt = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaDG:
                        supplement[suppIdx].DgProt = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaADIP:
                        supplement[suppIdx].ADIP_2_CP = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaPH:
                        supplement[suppIdx].Phosphorus = e.attrVal;
                        break;
                    case TSupplement.TSuppAttribute.spaSU:
                        supplement[suppIdx].Sulphur = e.attrVal;
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnSuppNameChanged(object sender, TStringArgs e)
        {
            supplement[suppIdx].sName = e.name;
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
            for (int i = 1; i < supplement.NoStores; i++ )  // SKIP element 0; that's reserved for fodder
            {
                if (string.IsNullOrWhiteSpace(supplement[i].sName))
                {
                    names.Add("Supplement " + i.ToString());
                }
                else
                {
                    names.Add(supplement[i].sName);
                }
            }
            this.supplementView.SupplementNames = names.ToArray();

            if (names.Count > 0 && suppIdx <= supplement.NoStores)
            {
                supplementView.SelectedSupplementValues = supplement[suppIdx];
            }
        }

        void PopulateView()
        {
            suppIdx = 0;
            PopulateDefaultNames();
            PopulateSupplementNames();
            if (supplement.NoStores > 1)
            {
                suppIdx = 1;
                supplementView.SelectedSupplementValues = supplement[suppIdx];
                supplementView.SelectedSupplementIndex = suppIdx - 1;
            }
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        void OnModelChanged(object changedModel)
        {
            if (changedModel == supplement)
                PopulateView();
        }
    
    }
}
