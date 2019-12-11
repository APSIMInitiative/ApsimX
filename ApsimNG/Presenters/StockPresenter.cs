// -----------------------------------------------------------------------
// <copyright file="StockPresenter.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using Interfaces;
    using Models.GrazPlan;
    using Views;

    /// <summary>
    /// A presenter class for the Stock model
    /// </summary>
    public class StockPresenter : IPresenter
    {
        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        private Stock stock;

        /// <summary>
        /// The initial Stock view;
        /// </summary>
        private IStockView stockView;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The initial stock model</param>
        /// <param name="view">The stock view to work with</param>
        /// <param name="explrPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explrPresenter)
        {
            stock = model as Stock;
            stockView = view as StockView;
            explorerPresenter = explrPresenter;

            stockView.GetGenoParams += OnGetGenoParams;

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

            stock.GenoTypes = stockView.Genotypes;  // copies back to the model

        }

        /// <summary>
        /// Populate the view object
        /// </summary>
        public void PopulateView()
        {
            PopulateGenotypes();
            stockView.SetValues();
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
            stockView.OnCalcNormalWeight += ValidFFWeightRange;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            stockView.OnCalcNormalWeight -= ValidFFWeightRange;
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == stock)
            {
                PopulateView();
            }
        }

        /// <summary>
        /// Initialise the list of genotypes
        /// </summary>
        private void PopulateGenotypes()
        {
            stockView.Genotypes = stock.GenoTypes;  // copies the init value array into the View
        }

        private void OnGetGenoParams(object sender, GenotypeInitArgs e)
        {
            AnimalParamSet tempParams = stock.ParamsFromGenotypeInits(e.ParamSet, e.Genotypes, e.Index);
            stockView.SetGenoParams(tempParams);
        }


        public readonly int[] MatureAge = { 2 * 365, 3 * 365 };
        public readonly double BC_STEP = 0.075;

        /// <summary>
        /// Returns the valid fleece-free weight range associated with stock of a given  
        /// age etc, along with expected basal weight assuming that the current weight
        ///
        /// is the highest weight reached so far(BWFA, or "base weight for age").       
        /// This is complicated in younger animals by the fact that a given condition    
        /// score may be relative to any of a range of normal weights.                   

        /// </summary>
        /// <param name="mainParams"></param>
        /// <param name="BreedInfo"></param>
        /// <param name="iGenotype"></param>
        /// <param name="Repro"></param>
        public void ValidFFWeightRange(AnimalParamSet mainParams,
                                      SingleGenotypeInits[] BreedInfo,
                                      int iGenotype,
                                      GrazType.ReproType Repro,
                                      int AgeDays,
                                      double dLowBC, double dHighBC,
                                      out double LowWt, out double HighWt)
        {
            GrazType.ReproType[] SexRepro = { GrazType.ReproType.Castrated, GrazType.ReproType.Empty };
            int[] MatureMonths = { 24, 36 };

            AnimalParamSet Genotype;
            double MaxNormWt,
            MinNormWt;

            Genotype = stock.ParamsFromGenotypeInits(mainParams, BreedInfo, iGenotype);
            MaxNormWt = AnimalGroup.GrowthCurve(AgeDays, Repro, Genotype);

            if ((AgeDays < MatureAge[(int)Genotype.Animal]) && (dLowBC <= 1.0))                     // Allow for the possibility of a sub-   
                MinNormWt = 0.7 * MaxNormWt;                                                        // optimal normal weight in young 
            else                                                                                    // animals                             
                MinNormWt = MaxNormWt;

            LowWt = MinNormWt * (dLowBC - 0.5 * BC_STEP);
            HighWt = MaxNormWt * (dHighBC + 0.499 * BC_STEP);
            if ((Repro == GrazType.ReproType.EarlyPreg) || (Repro == GrazType.ReproType.LatePreg))  // Allowance for conceptus in ewes & cows
                HighWt = HighWt * 1.15;
        }
    }
}
