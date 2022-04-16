using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Open.Hierarchy;

/// <summary>
/// Extensions for traversing a hierarchy (tree).
/// </summary>
public static class TraversalExtensions
{
	private const string DescendantIsNotExpectedType = "Descendant is not of expected generic type and may create inconsistent results.  May need to use non-generic node.GetDescenants method.";

	// NOTE: not recursive, but could produce a large stack while traversing, possibly could be better, but this is a unidirectional hierarchy so it's not so easy to avoid recursion.

	/// <summary>
	/// Returns all descendant nodes.
	/// </summary>
	/// <param name="root">The root node to begin traversal.</param>
	/// <param name="traversal">The mode by which the tree is traversed.</param>
	/// <returns>The enumeration of all the descendant nodes.</returns>
	public static IEnumerable<TNode> GetDescendantsOfType<TNode>(
		this IParent<TNode> root, TraversalMode traversal = TraversalMode.BreadthFirst)
	{
		foreach (var descendant in root.GetDescendants(traversal))
		{
			yield return descendant is not TNode n
				? throw new InvalidCastException(DescendantIsNotExpectedType)
				: n;
		}
	}

	/// <summary>
	/// Returns all descendant nodes and includes this node.
	/// Breadth first returns this node first.  Depth first returns this node last.
	/// </summary>
	/// <param name="root">The root node to begin traversal.</param>
	/// <param name="traversal">The mode by which the tree is traversed.</param>
	/// <returns>The enumeration of all the descendant nodes including this one.</returns>
	public static IEnumerable<TNode> GetNodesOfType<TNode, TRoot>(
		this TRoot root, TraversalMode traversal = TraversalMode.BreadthFirst)
		where TRoot : TNode, IParent<TNode>
	{
		if (traversal == TraversalMode.BreadthFirst)
			yield return root;
		foreach (var descendant in root.GetDescendantsOfType(traversal))
			yield return descendant;
		if (traversal == TraversalMode.DepthFirst)
			yield return root;
	}

	/// <summary>
	/// Returns all descendant nodes and includes this node.
	/// Breadth first returns this node first.  Depth first returns this node last.
	/// </summary>
	/// <param name="root">The root node to begin traversal.</param>
	/// <param name="traversal">The mode by which the tree is traversed.</param>
	/// <returns>The enumeration of all the descendant nodes including this one.</returns>
	public static IEnumerable<TNode> GetNodesOfType<TNode>(
		this TNode root, TraversalMode traversal = TraversalMode.BreadthFirst)
		where TNode : IParent<TNode>
		=> root.GetNodesOfType<TNode, TNode>(traversal);

	/// <summary>
	/// Returns all descendant nodes.
	/// </summary>
	/// <param name="root">The root node to begin traversal.</param>
	/// <param name="traversal">The mode by which the tree is traversed.</param>
	/// <returns>The enumeration of all the descendant nodes.</returns>
	public static IEnumerable<object> GetDescendants(
		this IParent root, TraversalMode traversal = TraversalMode.BreadthFirst)
	{
		return root is null
			? throw new ArgumentNullException(nameof(root))
			: GetDescendantsCore(root, traversal);

		static IEnumerable<object> GetDescendantsCore(IParent root, TraversalMode traversal)
		{
			// Attempt to be more breadth first.
			switch (traversal)
			{
				case TraversalMode.BreadthFirst:

					// Step 1: return the children.
					foreach (var child in root.Children)
						yield return child;

					var grandchildren = root.Children
						.OfType<IParent>()
						.SelectMany(c => c.Children)
						.ToArray();

					// Step 2: return the children.
					foreach (var grandchild in grandchildren)
						yield return grandchild;

					// Step 3: return the other descendants.  (not truly breadth first, but sufficient for now)
					foreach (var descendant in grandchildren
						.OfType<IParent>()
						.SelectMany(c => c.GetDescendants()))
					{
						yield return descendant;
					}

					break;

				case TraversalMode.DepthFirst:

					// Simply walk the tree to the leaves recursively.  Starting with the leaves and ending with the child.
					foreach (var child in root.Children)
					{
						foreach (var descendant in root.Children
							.OfType<IParent>()
							.SelectMany(c => c.GetDescendants(TraversalMode.DepthFirst)))
						{
							yield return descendant;
						}

						yield return child;
					}

					break;
			}
		}
	}

	/// <summary>
	/// Returns all descendant nodes and includes this node.
	/// Breadth first returns this node first.  Depth first returns this node last.
	/// </summary>
	/// <param name="root">The root node to begin traversal.</param>
	/// <param name="traversal">The mode by which the tree is traversed.</param>
	/// <returns>The enumeration of all the descendant nodes including this one.</returns>
	public static IEnumerable<object> GetNodes(
		this IParent root, TraversalMode traversal = TraversalMode.BreadthFirst)
	{
		if (traversal == TraversalMode.BreadthFirst)
			yield return root;
		foreach (var descendant in root.GetDescendants(traversal))
			yield return descendant;
		if (traversal == TraversalMode.DepthFirst)
			yield return root;
	}
}
