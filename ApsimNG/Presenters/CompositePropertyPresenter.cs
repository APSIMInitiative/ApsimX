using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Core;
using UserInterface.Classes;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A property presenter which also displays properties for child models.
    /// </summary>
    public class CompositePropertyPresenter : SimplePropertyPresenter
    {
        /// <summary>
        /// Override the GetProperties method to return properties from child models as well.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override PropertyGroup GetProperties(object obj)
        {
            PropertyGroup result = base.GetProperties(obj);
            List<PropertyGroup> childProperties = result.SubModelProperties.ToList();
            if (obj is IModel model)
                foreach (IModel child in model.Children)
                    childProperties.Add(base.GetProperties(child));

            return new PropertyGroup(result.Name, result.Properties, childProperties);
        }
    }
}