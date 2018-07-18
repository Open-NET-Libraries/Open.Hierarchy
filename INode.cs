using System.Collections.Generic;

namespace Open.Hierarchy
{
	public interface INode<TNode> : IList<TNode>, IChild<TNode>, IParent<TNode>, IHaveRoot<TNode>
		where TNode : INode<TNode>
	{
	}

}
