namespace Open.Hierarchy;

/// <summary>
/// Represents something that has a root (<see cref="IHaveRoot.Root"/>).
/// </summary>
public interface IHaveRoot
{
	/// <summary>
	/// The root of this.
	/// </summary>
	object Root { get; }
}

/// <summary>
/// Represents something that has a root (<see cref="IHaveRoot{TRoot}.Root"/>).
/// </summary>
public interface IHaveRoot<out TRoot> : IHaveRoot
{
	/// <summary>
	/// The generic root of this.
	/// </summary>
	new TRoot Root { get; }
}
