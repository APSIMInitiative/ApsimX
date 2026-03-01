using System;

namespace APSIM.Shared;

/// <summary>
/// An attribute that is applied to a class to stop instances of it from being cloned.
/// </summary>
public class NonSerializedClassAttribute : Attribute
{
}
