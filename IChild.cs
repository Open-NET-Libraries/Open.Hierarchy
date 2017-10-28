using System.Collections.Generic;

namespace Open.Hierarchy
{
	public interface IChild
	{
		object Parent { get; }
	}

	public interface IChild<out TParent> : IChild
    {
		new TParent Parent { get; }
    }

	public static class ChildExtensions
	{
		public static IEnumerable<TNode> GetAncestors<TNode>(
			this TNode node)
			where TNode : IChild<TNode>
		{
			TNode parent;
			while ((parent = node.Parent) != null)
			{
				yield return parent;
				node = parent;
			}
		}

		public static TNode GetRoot<TNode>(
			this TNode node)
			where TNode : IChild<TNode>
		{
			TNode parent;
			while ((parent = node.Parent) != null)
			{
				node = parent;
			}
			return node;
		}
	}

}
