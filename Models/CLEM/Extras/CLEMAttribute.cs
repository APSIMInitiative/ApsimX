using System;

namespace Models.CLEM
{
    /// <summary>Specifies the models that this class can sit under in the user interface./// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class MinimumTimeStepPermittedAttribute : Attribute
    {
        /// <summary>Allowable parent type.</summary>
        public TimeStepTypes TimeStep { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MinimumTimeStepPermittedAttribute()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="timestep">Specified the minimum timestep permitted.</param>
        public MinimumTimeStepPermittedAttribute(TimeStepTypes timestep)
        {
            TimeStep = timestep;
        }
    }
}
