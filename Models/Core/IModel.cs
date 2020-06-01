
namespace Models.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The IModel interface specifies the properties and methods that all
    /// models must have. 
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the parent model. Can be null if model has no parent.
        /// </summary>
        IModel Parent { get; set; }

        /// <summary>
        /// Gets of sets the child models. Can be empty array but never null.
        /// </summary>
        List<IModel> Children { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in the auto-doc documentation.
        /// </summary>
        bool IncludeInDocumentation { get; set; }

        /// <summary>
        /// Gets or sets whether the model is enabled
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether the model is readonly.
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Full path to the model.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// Find a sibling with a given name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        IModel Sibling(string name);

        /// <summary>
        /// Find a descendant with a given name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        IModel Descendant(string name);

        /// <summary>
        /// Find an ancestor with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        IModel Ancestor(string name);

        /// <summary>
        /// Find a model in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        IModel InScope(string name);

        /// <summary>
        /// Find a sibling with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        T Sibling<T>() where T : IModel;

        /// <summary>
        /// Find a descendant with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        T Descendant<T>() where T : IModel;

        /// <summary>
        /// Find an ancestor with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        T Ancestor<T>() where T : IModel;

        /// <summary>
        /// Find a model in scope with a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of model to find.</typeparam>
        T InScope<T>() where T : IModel;

        /// <summary>
        /// Find a sibling with a given type and name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        T Sibling<T>(string name) where T : IModel;

        /// <summary>
        /// Find a descendant model with a given type and name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        T Descendant<T>(string name) where T : IModel;

        /// <summary>
        /// Find an ancestor with a given type and name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        T Ancestor<T>(string name) where T : IModel;

        /// <summary>
        /// Find a model in scope with a given type and name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        /// <typeparam name="T">Type of model to find.</typeparam>
        T InScope<T>(string name) where T : IModel;

        /// <summary>
        /// Find all siblings with a given name.
        /// </summary>
        /// <param name="name">Name of the siblings.</param>
        IEnumerable<IModel> Siblings(string name);

        /// <summary>
        /// Find all descendants with a given name.
        /// </summary>
        /// <param name="name">Name of the descendants.</param>
        IEnumerable<IModel> Descendants(string name);

        /// <summary>
        /// Find all ancestors with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestors.</param>
        IEnumerable<IModel> Ancestors(string name);

        /// <summary>
        /// Find all models in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the models.</param>
        IEnumerable<IModel> InScopeAll(string name);

        /// <summary>
        /// Find all siblings of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> Siblings<T>() where T : IModel;

        /// <summary>
        /// Find all descendants of the given type.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        IEnumerable<T> Descendants<T>() where T : IModel;

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> Ancestors<T>() where T : IModel;

        /// <summary>
        /// Find all models of the given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> InScopeAll<T>() where T : IModel;

        /// <summary>
        /// Find all siblings with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        /// <param name="name">Name of the siblings.</param>
        IEnumerable<T> Siblings<T>(string name) where T : IModel;

        /// <summary>
        /// Find all descendants with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        /// <param name="name">Name of the descendants.</param>
        IEnumerable<T> Descendants<T>(string name) where T : IModel;

        /// <summary>
        /// Find all ancestors with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of ancestors to return.</typeparam>
        /// <param name="name">Name of the ancestors.</param>
        IEnumerable<T> Ancestors<T>(string name) where T : IModel;

        /// <summary>
        /// Find all models with the given type and name in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        /// <param name="name">Name of the models.</param>
        IEnumerable<T> InScopeAll<T>(string name) where T : IModel;

        /// <summary>
        /// Returns all ancestor models.
        /// </summary>
        IEnumerable<IModel> Ancestors();

        /// <summary>
        /// Returns all descendant models.
        /// </summary>
        IEnumerable<IModel> Descendants();

        /// <summary>
        /// Returns all sibling models.
        /// </summary>
        IEnumerable<IModel> Siblings();

        /// <summary>
        /// Returns all models which are in scope.
        /// </summary>
        IEnumerable<IModel> InScopeAll();

        /// <summary>
        /// Return true iff a model with the given type can be added to the model.
        /// </summary>
        /// <param name="type">The child type.</param>
        bool IsChildAllowable(Type type);

        /// <summary>
        /// Parent all descendant models.
        /// </summary>
        void ParentAllDescendants();

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        void OnCreated();
    }
}