using System.Reflection;

namespace Models.Core
{
    /// <summary>
    /// Represents a member (field or property) of one model which
    /// references another model.
    /// </summary>
    public class Reference
    {
        /// <summary>
        /// The member which references a model.
        /// </summary>
        public MemberInfo Member { get; set; }

        /// <summary>
        /// The model being referenced.
        /// </summary>
        public IModel Target { get; set; }

        /// <summary>
        /// The model referencing the target.
        /// </summary>
        public IModel Model { get; set; }
    }
}
