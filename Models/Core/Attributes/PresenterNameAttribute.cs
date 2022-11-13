namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related class should use the user interface presenter
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PresenterNameAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the presenter class
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenterNameAttribute" /> class.
        /// </summary>
        /// <param name="name">Name of the user interface presenter class to use</param>
        public PresenterNameAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the name of the presenter.
        /// </summary>
        /// <returns>The name of the presenter class</returns>
        public override string ToString()
        {
            return this.name;
        }
    } 
}
