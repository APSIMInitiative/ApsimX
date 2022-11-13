namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewNameAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the view class
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewNameAttribute" /> class.
        /// </summary>
        /// <param name="name">Name of the user interface view class to use</param>
        public ViewNameAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the name of the view.
        /// </summary>
        /// <returns>The name of the view class</returns>
        public override string ToString()
        {
            return this.name;
        }
    } 
}
