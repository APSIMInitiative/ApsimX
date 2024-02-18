
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;

    public class CategoryTree
    {
        public List<CategoryItem> CategoryItems;

        public CategoryTree()
        {
            CategoryItems = new List<CategoryItem>();
        }

        public CategoryItem FindCategoryInTree(string catName)
        {
            return CategoryItems.Find(item => item.Name == catName);
        }

        public void AddCategoryToTree(String catName)
        {
            bool catExists = CategoryItems.Exists(item => item.Name == catName);
            if (!catExists)
                CategoryItems.Add(new CategoryItem(catName));
        }

        public void OrderBy(Comparison<CategoryItem> comparison)
        {
            CategoryItems.Sort(comparison);
        }
    }
}
