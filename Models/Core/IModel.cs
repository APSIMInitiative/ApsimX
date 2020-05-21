
namespace Models.Core
{
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
        /// Find a sibling of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        T Sibling<T>() where T : IModel;

        /// <summary>
        /// Find a descendant of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        T Descendant<T>() where T : IModel;

        /// <summary>
        /// Find an ancestor of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        T Ancestor<T>() where T : IModel;

        /// <summary>
        /// Find a model of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of model to find.</typeparam>
        T Find<T>() where T : IModel;

        /// <summary>
        /// Find all siblings of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> Siblings<T>() where T : IModel;

        /// <summary>
        /// Find all descendants of the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        IEnumerable<T> Descendants<T>() where T : IModel;

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        IEnumerable<T> Ancestors<T>() where T : IModel;

        /// <summary>
        /// Find all models of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        IEnumerable<T> FindAll<T>() where T : IModel;

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
        IEnumerable<IModel> FindAll();

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        void OnCreated();
    }
}