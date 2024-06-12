
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Security;
    using System.Reflection;
    using global::UserInterface.Interfaces;
    using global::UserInterface.Views;
    using Models.CLEM;
    using Models.Core;

    /// <summary>
    /// This presenter class is responsible for wrapping the SimplePropertyPresenter in a view that includes a 
    /// tree view of category and sub-category property attributes that define the filter rule used and 
    /// which properties are displayed. This was developed to help the user when a large number of properties are
    /// provided with a model, and you want to distinguish the type of user relevant to the properties
    /// This presenter uses the PropertyView and PropertyMultiModelView views.
    /// </summary>
    public class PropertyCategorisedPresenter : IPresenter
    {
        private readonly Dictionary<string, int> sortOrderList = new ()
        {
            { "Simulation", 0 },
            { "Farm", 0 },
            { "Breed", 1 },
            { "Pasture", 1 },
            { "Core", 2 },
            { "Unspecified", 0 }
        };

        protected int userLevel = 0;
        protected CategoryTree categoryTree = null;

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
            get { return treeview.TreeWidth; }
            set { treeview.TreeWidth = value; }
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
                return propertyPresenter;
            }
        }

        /// <summary>Gets the path of the current selected node in the tree.</summary>
        /// <value>The current node path.</value>
        public string CurrentNodePath
        {
            get
            {
                return treeview.SelectedNode;
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

            userLevel = (int)((model as IModel).FindInScope<ZoneCLEM>()?.UserType?? CLEMUserType.General);

            treeview = view as IPropertyCategorisedView;
            if(view is null)
                throw new ArgumentException($"The view must be an PropertyCategorisedView instance");

            treeview.SelectedNodeChanged += OnNodeSelected;
            this.explorerPresenter = explorerPresenter;

            //Fill in the nodes in the tree view
            propertyView = new PropertyView(treeview as ViewBase);

            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            categoryTree = GetPropertyCategories();
            RefreshTreeView();

            //Initialise the Right Hand View
            ShowRightHandView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            treeview.SelectedNodeChanged -= OnNodeSelected;

            HideRightHandView();
            (treeview as ViewBase).Dispose();
        }

        /// <summary>
        /// Refresh the treeview.
        /// </summary>
        public void RefreshTreeView()
        {
            treeview.Refresh(GetNodeDescription(categoryTree));
        }

        /// <summary>Select a node in the view.</summary>
        /// <param name="nodePath">Path to node</param>
        public void SelectNode(string nodePath)
        {
            treeview.SelectedNode = nodePath;
            HideRightHandView();
            ShowRightHandView();
        }

        /// <summary>Hide the right hand panel.</summary>
        public void HideRightHandView()
        {
            if (propertyPresenter != null)
            {
                try
                {
                    propertyPresenter.Detach();
                }
                catch (Exception err)
                {
                    throw new Exception(err.Message);
                }
            }

            selectedCategory = "";
            selectedSubCategory = "";
            treeview.AddRightHandView(null); //add an Empty right hand view
        }

        /// <summary>Display a view on the right hand panel in view.</summary>
        public void ShowRightHandView()
        {
            (propertyPresenter as PropertyPresenter).Filter = IsPropertyVisibleToUser;
            if (treeview.SelectedNode != string.Empty)
            {
                string[] path = treeview.SelectedNode.Split('.');
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
                selectedCategory = category;
                selectedSubCategory = subcategory;
                if(selectedCategory != "")
                    (propertyPresenter as PropertyPresenter).Filter = IsPropertySelected;
            }
            else
            {
                //this will show all the properties in the model based categories available for the current user level
                selectedCategory = "";
                selectedSubCategory = "";
            }

            CreateAndAttachRightPanel();
        }

        public virtual void CreateAndAttachRightPanel()
        {
            // create a new grid view to be added as a RightHandView
            // nb. the grid view is owned by the tree view not by this presenter.
            propertyView = new PropertyView(treeview as ViewBase);

            // disable right pane if user is not authorised
            if((selectedCategory ?? "") != "")
            {
                int authLevel;
                bool authOk = true;
                if (sortOrderList.TryGetValue(selectedCategory, out authLevel))
                    authOk = (userLevel >= authLevel);

                (propertyView as PropertyView).MainWidget.SetProperty("sensitive", new GLib.Value(authOk));
            }

            propertyPresenter.Attach(model, propertyView, explorerPresenter);
            treeview.AddRightHandView(propertyView);
        }

        /// <summary>A node has been selected (whether by user or undo/redo)</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Node arguments</param>
        protected void OnNodeSelected(object sender, NodeSelectedArgs e)
        {
            HideRightHandView();
            ShowRightHandView();
        }

        public virtual IModel ModelForProperties()
        {
            return model;
        }

        /// <summary>
        /// Returns the Category Tree created from the Category Attributes on the properties in the model.
        /// </summary>
        /// <returns></returns>
        private CategoryTree GetPropertyCategories()
        {
            CategoryTree categories = new CategoryTree();

            foreach (var propertyPair in (propertyPresenter as PropertyPresenter).GetPropertyMap.Values)
            {

                if(propertyPair.Category is not null)
                { 
                    int catIndex = 0;
                    foreach (var catLabel in (propertyPair.Category.Category.Split(':')))
                    {
                        if (catLabel != "*")
                        {
                            categories.AddCategoryToTree(catLabel);
                            //add the subcategory name to the list of subcategories for the category
                            CategoryItem catItem = categories.FindCategoryInTree(catLabel);
                            var subLabels = propertyPair.Category.Subcategory.Split(':');
                            if (subLabels.Length >= catIndex + 1)
                                if (subLabels[catIndex] != "*")
                                    catItem.AddSubcategoryName(subLabels[catIndex]);
                        }
                        catIndex++;
                    }
                }
                else
                {
                    // If there is no [Category] attribute at all on the property in the model add it to the "Unspecified" Category, and "Unspecified" Subcategory
                    categories.AddCategoryToTree("Unspecified");
                    CategoryItem catItem = categories.FindCategoryInTree("Unspecified");
                    catItem.AddSubcategoryName("Unspecified");
                }
            }

            categories.CategoryItems = categories.CategoryItems.OrderBy(x => sortOrderList.Keys.ToList().IndexOf(x.Name)).ToList();
            return categories;
        }
 
        /// <summary>
        /// A helper function for creating a node description object for the category hierarchy.
        /// </summary>
        /// <param name="categoryTree"></param>
        /// <returns>The description</returns>
        private TreeViewNode GetNodeDescription(CategoryTree categoryTree)
        {
            TreeViewNode root = new();
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
            if ((selectedCategory??"") != "") // a category has been selected
            {
                if (Attribute.IsDefined(property, typeof(CategoryAttribute), false))
                {
                    CategoryAttribute catAtt = (CategoryAttribute)Attribute.GetCustomAttribute(property, typeof(CategoryAttribute));
                    if (Array.Exists(catAtt.Category.Split(':'), element => element == selectedCategory))
                    {
                        if ((selectedSubCategory ?? "") != "") // a sub category has been selected
                            return (Array.Exists(catAtt.Subcategory.Split(':'), element => element == selectedSubCategory));
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // if we are filtering on "Unspecified" category then there is no Category Attribute on the property in the model.
                    return (selectedCategory == "Unspecified");
                }
            }
            return true;
        }

        protected bool IsPropertyVisibleToUser(PropertyInfo property)
        {
            if (Attribute.IsDefined(property, typeof(CategoryAttribute), false))
            {
                CategoryAttribute catAtt = (CategoryAttribute)Attribute.GetCustomAttribute(property, typeof(CategoryAttribute));
                foreach (var subCat in catAtt.Category.Split(':'))
                {
                    if (sortOrderList.ContainsKey(subCat))
                    {
                        if (sortOrderList[subCat] <= userLevel)
                            { return true; }
                    }
                    else
                        { return true; }
                }
                return false;
            }
            return true;
        }
    }
}
