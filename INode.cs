using System.Collections.Generic;

namespace Open.Hierarchy
{
	public interface INode<TNode> : ICollection<TNode>, IChild<TNode>, IParent<TNode>, IHaveRoot<TNode>
		where TNode : INode<TNode>
	{
    }
	
}
