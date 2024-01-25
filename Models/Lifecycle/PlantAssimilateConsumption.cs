﻿using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;

namespace Models.LifeCycle
{
    /// <summary>
    /// For Pests/Diseases that take assimilate dirrect from the vessels. 
    /// for example aphids
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantAssimilateConsumption : Model
    {
        /// <summary> Select host plant that Pest/Disease may bother </summary>
        [Description("Select host plant that Pest/Disease may bother")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IPlantDamage))]
        public IPlantDamage HostPlant { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            throw new NotImplementedException("Removal of assimilate is not implemented");
        }
    }
}
