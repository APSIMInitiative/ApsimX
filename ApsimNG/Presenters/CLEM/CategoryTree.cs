
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;

    public class CategoryTree
    {
        public List<CategoryItem> CategoryItems;

        public CategoryTree()
        {
            this.CategoryItems = new List<CategoryItem>();
        }

        public CategoryItem FindCategoryInTree(string catName)
        {
            return this.CategoryItems.Find(item => item.Name == catName);
        }

        public void AddCategoryToTree(String catName)
        {
            bool catExists = this.CategoryItems.Exists(item => item.Name == catName);
            if (!catExists)
                this.CategoryItems.Add(new CategoryItem(catName));
        }
    }
}
