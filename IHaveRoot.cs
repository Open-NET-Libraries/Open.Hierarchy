namespace Open.Hierarchy;

public interface IHaveRoot
{
	/// <summary>
	/// The root of this.
	/// </summary>
	object Root { get; }
}

public interface IHaveRoot<out TRoot> : IHaveRoot
{
	/// <summary>
	/// The generic root of this.
	/// </summary>
	new TRoot Root { get; }
}
