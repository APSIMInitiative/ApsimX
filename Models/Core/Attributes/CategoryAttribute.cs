namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies the category and subcategory that the related property belongs to,
    /// so that you can group properties by category and subcategory.
    /// Grouping is useful when a model has large numbers of properties that need to be parameterised by the user. 
    /// The PropertyExplorerPresenter can be used to display the properties in a ExplorerReadOnlyView. 
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CategoryAttribute : System.Attribute
    {
        private string category;
        private string subcategory;

        /// <summary>
        /// Constructor with only Category specified
        /// </summary>
        /// <param name="Category"></param>
        public CategoryAttribute(string Category)
        {
            this.category = Category;
            this.subcategory = "Unspecified";
        }

        /// <summary>
        /// Constructor with but Category and Subcategory specified
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="Subcategory"></param>
        public CategoryAttribute(string Category, string Subcategory)
        {
            this.category = Category;
            this.subcategory = Subcategory;
        }

        /// <summary>
        /// Gets or sets the Category
        /// </summary>
        public string Category { get { return category; } }

        /// <summary>
        /// Gets or sets the Subcategory
        /// </summary>
        public string Subcategory { get { return subcategory; } }
    } 
}
