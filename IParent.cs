namespace Open.Hierarchy;

/// <summary>
/// Represents something that can have children (<see cref="IParent.Children"/>).
/// </summary>
public interface IParent
{
	/// <summary>
	/// Read only access to the contained children.
	/// </summary>
	// ReSharper disable once ReturnTypeCanBeEnumerable.Global
	IReadOnlyList<object> Children { get; }
}

/// <summary>
/// Represents something that can have children (<see cref="IParent{TChild}.Children"/>).
/// </summary>
public interface IParent<out TChild> : IParent
{
	/// <summary>
	/// Generic read only access to the contained children.
	/// </summary>
	new IReadOnlyList<TChild> Children { get; }
}
