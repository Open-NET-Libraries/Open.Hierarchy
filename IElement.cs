using System.Diagnostics.CodeAnalysis;

namespace Open.Hierarchy;

/// <summary>
/// Represents something that contains a value (<see cref="IElement{T}.Value"/>).
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IElement<T>
{
	/// <summary>
	/// The value contained by the element.
	/// </summary>
#if NETSTANDARD2_1
	[AllowNull]
#endif
	T Value { get; set; }
}
