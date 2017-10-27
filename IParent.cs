using System.Collections.Generic;

namespace Open.Hierarchy
{
	public interface IParent
	{
		IReadOnlyList<object> Children { get; }
	}

	public interface IParent<out TChild> : IParent
	{
		new IReadOnlyList<TChild> Children { get; }
	}

}