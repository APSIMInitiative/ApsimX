
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using global::UserInterface.Extensions;
    using Interfaces;
    using Models;
    using Models.Core;
    using Views;

    /// <summary>
    /// This presenter class is responsible for wrapping the SimplePropertyPresenter in a view that includes a 
    /// tree view of category and sub-category property attributes that define the filter rule used and 
    /// which properties are displayed. This was developed to hepl the user when a large number of properties are
    /// provided with a model, you you want to distinguish general and advanced properties
    /// This presenter uses the PropertyView and PropertyMultiModelView views.
    /// </summary>
    public class PropertyCategorisedPresenter : IPresenter
    {
        /// <summary>The visual instance</summary>
        protected IPropertyCategorisedView treeview;
        protected IPropertyView propertyView;

        /// <summary>Presenter for the component</summary>
        protected IPresenter propertyPresenter;

        /// <summary>
        /// The model we're going to examine for properties.
        /// </summary>
        protected Model model;

        /// <summary>
        /// The category name to filter for on the Category Attribute for the properties
        /// </summary>
        protected string selectedCategory { get; set; }

        /// <summary>
        /// The subcategory name to filter for on the Category Attribute for the properties
        /// </summary>
        protected string selectedSubCategory { get; set; }

        /// <summary>Gets or sets the width of the explorer tree panel</summary>
        /// <value>The width of the tree.</value>
        public int TreeWidth
        {
            get { return this.treeview.TreeWidth; }
            set { this.treeview.TreeWidth = value; }
        }

        /// <summary>
        /// The parent ExplorerPresenter.
        /// </summary>
        protected ExplorerPresenter explorerPresenter;

        /// <summary>Gets the current right hand presenter.</summary>
        /// <value>The current presenter.</value>
        public IPresenter PropertyPresenter
        {
            get
            {
                return this.propertyPresenter;
            }
        }

        /// <summary>Gets the path of the current selected node in the tree.</summary>
        /// <value>The current node path.</value>
        public string CurrentNodePath
        {
            get
            {
                return this.treeview.SelectedNode;
            }
        }

        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        /// <param name="model">The simulation model</param>
        /// <param name="view">The view used for display</param>
        /// <param name="explorerPresenter">The presenter for this object</param>
        public virtual void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.treeview = view as IPropertyCategorisedView;
            if(view is null)
                throw new ArgumentException($"The view must be an PropertyCategorisedView instance");

            this.treeview.SelectedNodeChanged += this.OnNodeSelected;
            this.explorerPresenter = explorerPresenter;

            //Fill in the nodes in the tree view
            this.RefreshTreeView();

            //Initialise the Right Hand View
            this.propertyPresenter = new PropertyPresenter();
            this.ShowRightHandView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.treeview.SelectedNodeChanged -= this.OnNodeSelected;

            this.HideRightHandView();
            (treeview as ViewBase).Dispose();
        }

        /// <summary>
        /// Refresh the treeview.
        /// </summary>
        public void RefreshTreeView()
        {
            CategoryTree categoryTree = this.GetPropertyCategories();
            this.treeview.Refresh(this.GetNodeDescription(categoryTree));
        }

        /// <summary>Select a node in the view.</summary>
        /// <param name="nodePath">Path to node</param>
        public void SelectNode(string nodePath)
        {
            this.treeview.SelectedNode = nodePath;
            this.HideRightHandView();
            this.ShowRightHandView();
        }

        /// <summary>Hide the right hand panel.</summary>
        public void HideRightHandView()
        {
            if (this.propertyPresenter != null)
            {
                try
                {
                    this.propertyPresenter.Detach();
                }
                catch (Exception err)
                {
                    throw new Exception(err.Message);
                }
            }

            this.selectedCategory = "";
            this.selectedSubCategory = "";
            this.treeview.AddRightHandView(null); //add an Empty right hand view
        }

        /// <summary>Display a view on the right hand panel in view.</summary>
        public void ShowRightHandView()
        {
            if (this.treeview.SelectedNode != string.Empty)
            {
                string[] path = this.treeview.SelectedNode.Split('.');
                string root = "";
                string category = "";
                string subcategory = "";

                //zero based but path[0] is always empty. 
                //(because SelectedNode path always starts with a ".")
                //true root ie. name of model is always path[1]
                switch (path.Length)
                {
                    case 1:
                        root = "";
                        category = "";
                        subcategory = "";
                        break;
                    case 2:
                        root = path[1];
                        category = "";
                        subcategory = "";
                        break;
                    case 3:
                        root = path[1];
                        category = path[2] ;
                        subcategory = "";
                        break;
                    case 4:
                        root = path[1];
                        category = path[2];
                        subcategory = path[3];
                        break;
                }
                this.selectedCategory = category;
                this.selectedSubCategory = subcategory;
                (this.propertyPresenter as PropertyPresenter).Filter = IsPropertySelected;
            }
            else
            {
                //this will show all the properties in the model 
                //there will be no filtering on Category and Subcategory.
                (this.propertyPresenter as PropertyPresenter).Filter = null;
                this.selectedCategory = "";
                this.selectedSubCategory = "";
            }

            CreateAndAttachRightPanel();
        }

        public virtual void CreateAndAttachRightPanel()
        {
            //create a new grid view to be added as a RightHandView
            //nb. the grid view is owned by the tree view not by this presenter.
            this.propertyView = new PropertyView(this.treeview as ViewBase);
            this.treeview.AddRightHandView(this.propertyView);
            this.propertyPresenter.Attach(this.model, this.propertyView, this.explorerPresenter);
        }

        /// <summary>A node has been selected (whether by user or undo/redo)</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Node arguments</param>
        protected void OnNodeSelected(object sender, NodeSelectedArgs e)
        {
            this.HideRightHandView();
            this.ShowRightHandView();
        }

        public virtual IModel ModelForProperties()
        {
            return this.model;
        }

        /// <summary>
        /// Returns the Category Tree created from the Category Attributes on the properties in the model.
        /// </summary>
        /// <returns></returns>
        private CategoryTree GetPropertyCategories()
        {
            CategoryTree categories = new CategoryTree();
            IModel modelToUse = ModelForProperties();

            if (modelToUse != null)
            {
                foreach (PropertyInfo property in modelToUse.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    // Properties must have a [Description], not be called Name, and be read/write.
                    bool hasDescription = property.IsDefined(typeof(DescriptionAttribute), false);
                    bool includeProperty = hasDescription &&
                                           property.Name != "Name" &&
                                           property.CanRead &&
                                           property.CanWrite;

                    // Only allow lists that are double[], int[] or string[]
                    if (includeProperty && property.PropertyType.GetInterface("IList") != null)
                        includeProperty = property.PropertyType == typeof(double[]) ||
                                          property.PropertyType == typeof(int[]) ||
                                          property.PropertyType == typeof(string[]);

                    if (includeProperty)
                    { 
                        // Those properties with a [Catagory] attribute 
                        bool hasCategory = property.IsDefined(typeof(CategoryAttribute), false);
                        if (hasCategory)
                        {
                            //get the attribute data
                            CategoryAttribute catAtt = (CategoryAttribute)property.GetCustomAttribute(typeof(CategoryAttribute));
                            // add the category name to the list of category items
                            // allow : separated list for multiple categories
                            int catIndex = 0;
                            foreach (var catLabel in catAtt.Category.Split(':'))
                            {
                                if (catLabel != "*")
                                {
                                    categories.AddCategoryToTree(catLabel);
                                    //add the subcategory name to the list of subcategories for the category
                                    CategoryItem catItem = categories.FindCategoryInTree(catLabel);
                                    var subLabels = catAtt.Subcategory.Split(':');
                                    if (subLabels.Length >= catIndex + 1)
                                        if (subLabels[catIndex] != "*")
                                            catItem.AddSubcategoryName(subLabels[catIndex]);
                                }
                                catIndex++;
                            }
                            //categories.AddCategoryToTree(catAtt.Category);
                            ////add the subcategory name to the list of subcategories for the category
                            //CategoryItem catItem = categories.FindCategoryInTree(catAtt.Category);
                            //catItem.AddSubcategoryName(catAtt.Subcategory);
                        }
                        else
                        {
                            //If there is not [Category] attribute at all on the property in the model.
                            //Add it to the "Unspecified" Category, and "Unspecified" Subcategory
                            categories.AddCategoryToTree("Unspecified");
                            CategoryItem catItem = categories.FindCategoryInTree("Unspecified");
                            catItem.AddSubcategoryName("Unspecified");
                        }
                    }
                }
            }
            return categories;
        }
 
        /// <summary>
        /// A helper function for creating a node description object for the category hierarchy.
        /// </summary>
        /// <param name="categoryTree"></param>
        /// <returns>The description</returns>
        private TreeViewNode GetNodeDescription(CategoryTree categoryTree)
        {
            TreeViewNode root = new TreeViewNode();
            root.Name =  model.Name;

            // find namespace and image name needed to find image file in the Resources of UserInterface project
            string nameSpace = model.GetType().FullName;
            nameSpace = nameSpace.Substring(7);
            root.ResourceNameForImage = $"ApsimNG.Resources.TreeViewImages.{nameSpace}.svg";

            foreach (CategoryItem cat in categoryTree.CategoryItems)
            {
                TreeViewNode description = new TreeViewNode();
                description.Name = cat.Name;
                description.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.Category.svg";
                description.Children = new List<TreeViewNode>();
                foreach (string subcat in cat.SubcategoryNames)
                {
                    TreeViewNode child = new TreeViewNode();
                    child.Name = subcat;
                    child.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.CategoryItem.svg";
                    description.Children.Add(child);
                }
                root.Children.Add(description);
            }
            return root;
        }

        protected bool IsPropertySelected(PropertyInfo property)
        {
            if ((this.selectedCategory??"") != "") // a category has been selected
            {
                if (Attribute.IsDefined(property, typeof(CategoryAttribute), false))
                {
                    CategoryAttribute catAtt = (CategoryAttribute)Attribute.GetCustomAttribute(property, typeof(CategoryAttribute));
                    if (catAtt.Category.StartsWith("*") || Array.Exists(catAtt.Category.Split(':'), element => element == this.selectedCategory))
                    {
                        if ((selectedSubCategory ?? "") != "") // a sub category has been selected
                        {
                            // The catAtt.Subcategory is by default given a value of 
                            // "Unspecified" if the Subcategory is not assigned in the Category Attribute.
                            // so this line below will also handle "Unspecified" subcategories.
                            return (catAtt.Subcategory.StartsWith("*") || Array.Exists(catAtt.Subcategory.Split(':'), element => element == selectedSubCategory));
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // if we are filtering on "Unspecified" category then there is no Category Attribute
                    // just a Description Attribute on the property in the model.
                    // So we still may need to include it in this case.
                    return (this.selectedCategory == "Unspecified");
                }
            }
            return true;
        }
    }
}
