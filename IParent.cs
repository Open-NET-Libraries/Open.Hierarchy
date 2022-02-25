using System.Collections.Generic;

namespace Open.Hierarchy;

public interface IParent
{
	/// <summary>
	/// Read only access to the contained children.
	/// </summary>
	// ReSharper disable once ReturnTypeCanBeEnumerable.Global
	IReadOnlyList<object> Children { get; }
}

public interface IParent<out TChild> : IParent
{
	/// <summary>
	/// Generic read only access to the contained children.
	/// </summary>
	new IReadOnlyList<TChild> Children { get; }
}
