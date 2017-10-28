using System.Collections.Generic;
using System.Linq;

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

	public static class ParentExtensions
	{

		/// <summary>
		/// Returns all descendant nodes.
		/// </summary>
		/// <param name="traversal">The mode by which the tree is traversed.</param>
		/// <returns>The enumeration of all the descendant nodes.</returns>
		public static IEnumerable<TNode> GetDescendants<TNode>(
			this TNode node, TraversalMode traversal = TraversalMode.BreadthFirst)
			where TNode : IParent<TNode>
		{
			// Attempt to be more breadth first.
			switch (traversal)
			{
				case TraversalMode.BreadthFirst:
					foreach (var child in node.Children)
						yield return child;

					var grandchildren = node.Children.SelectMany(c => c.Children);
					foreach (var grandchild in grandchildren)
						yield return grandchild;

					foreach (var descendant in grandchildren
						.SelectMany(c => c.GetDescendants()))
						yield return descendant;

					break;


				case TraversalMode.DepthFirst:

					foreach (var descendant in node.Children
						.SelectMany(c => c.GetDescendants(TraversalMode.DepthFirst)))
						yield return descendant;

					break;
			}

		}

		/// <summary>
		/// Returns all descendant nodes and includes this node.
		/// Breadth first returns this node first.  Depth first returns this node last.
		/// </summary>
		/// <param name="traversal">The mode by which the tree is traversed.</param>
		/// <returns>The enumeration of all the descendant nodes including this one.</returns>
		public static IEnumerable<TNode> GetNodes<TNode>(
			this TNode node, TraversalMode traversal = TraversalMode.BreadthFirst)
			where TNode : IParent<TNode>
		{
			if (traversal == TraversalMode.BreadthFirst)
				yield return node;
			foreach (var descendant in node.GetDescendants(traversal))
				yield return descendant;
			if (traversal == TraversalMode.DepthFirst)
				yield return node;
		}
	}
}