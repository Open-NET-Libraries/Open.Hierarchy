namespace Open.Hierarchy
{
	public interface IHaveRoot
	{
		object Root { get; }
	}

	public interface IHaveRoot<out TRoot> : IHaveRoot
    {
		new TRoot Root { get; }
    }
}
