using System;
using APSIM.Shared.Graphing;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// This class describes requirements that a particular series may have of an axis.
    /// For example, a series may require that an axis be a category axis in order
    /// to display string data.
    /// </summary>
    public class AxisRequirements
    {
        /// <summary>
        /// Field name for the axis - stored for error reporting purposes.
        /// </summary>
        /// <value></value>
        public string FieldName { get; private set; }

        /// <summary>
        /// Type of axis required by the series. If null, the series doesn't
        /// have any particular required axis type (possibly because it contains
        /// no data).
        /// </summary>
        public AxisType? AxisKind { get; private set; }

        /// <summary>
        /// Create a new <see cref="AxisRequirements"/> instance.
        /// </summary>
        /// <param name="type">Required axis type.</param>
        /// <param name="field">Field name for the axis, used for error reporting purposes.</param>
        public AxisRequirements(AxisType? type, string field)
        {
            AxisKind = type;
            FieldName = field;
        }

        /// <summary>
        /// Ensure that this set of axis requirements is compatible with another.
        /// Throw if not.
        /// </summary>
        /// <param name="other">Another set of axis requirements.</param>
        public void ThrowIfIncompatibleWith(AxisRequirements other)
        {
            try
            {
                if (AxisKind != other.AxisKind && AxisKind != null && other.AxisKind != null)
                    throw new Exception($"Axis type {AxisKind} is incompatible with {other.AxisKind}");
            }
            catch (Exception err)
            {
                throw new Exception($"Axis required by {FieldName} is incompatible with axis required by {other.FieldName}", err);
            }
        }
    }
}
