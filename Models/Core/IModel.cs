using System;
using System.Collections.Generic;
using APSIM.Core;

namespace Models.Core
{

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

        /// <summary>The name of the resource.</summary>
        string ResourceName { get; set; }

        /// <summary>The associated node for the model.</summary>
        Node Node { get; }

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

        /// <summary>
        /// Called immediately before a simulation has its links resolved and is run.
        /// It provides an opportunity for a simulation to restructure itself
        /// e.g. add / remove models.
        /// </summary>
        void OnPreLink();

    }
}
