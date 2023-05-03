
namespace UserInterface.Presenters
{
    using System.Collections.Generic;

    /// <summary>
    /// Class used to create the heirachy of the the categories and subcategories from the 
    /// [Category] attribute added to the properties of a model.
    /// </summary>
    public class CategoryItem
    {
        public string Name;

        /// <summary>
        /// Subcategories of this category
        /// </summary>
        public List<string> SubcategoryNames;

        /// <summary>
        /// Constructor 
        /// </summary>
        public  CategoryItem(string name)
        {
            this.Name = name;
            this.SubcategoryNames = new List<string>();
        }

        public void AddSubcategoryName(string name)
        {
            //is subcategory name already in the list
            bool subcatExists = this.SubcategoryNames.Exists(subcatname => subcatname == name);
            // if it isn't then add it.
            if (!subcatExists)
            {
                this.SubcategoryNames.Add(name);
            }
        }
    }
}
