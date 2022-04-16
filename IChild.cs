using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Open.Hierarchy;

/// <summary>
/// Represents something that has a parent (<see cref="IChild.Parent"/>).
/// </summary>
public interface IChild
{
	/// <summary>
	/// The parent of this child.
	/// </summary>
	object? Parent { get; }
}

/// <summary>
/// Represents something that has a parent (<see cref="IChild{TParent}.Parent"/>).
/// </summary>
/// <typeparam name="TParent"></typeparam>
public interface IChild<out TParent> : IChild
	where TParent : class
{
	/// <summary>
	/// The generic parent of this child.
	/// </summary>
	new TParent? Parent { get; }
}

/// <summary>
/// Extensions for getting ancestors.
/// </summary>
public static class ChildExtensions
{
	/// <summary>
	/// Crawls the ancestor lineage and returns them.
	/// </summary>
	/// <typeparam name="TNode">The node type.</typeparam>
	/// <param name="node">The child node to use.</param>
	/// <returns>An enumerable of the ancestors.</returns>
	public static IEnumerable<TNode> GetAncestors<TNode>(
		this TNode node)
		where TNode : class, IChild<TNode>
	{
		return node is null
			? throw new ArgumentNullException(nameof(node))
			: GetAncestorsCore(node);

		static IEnumerable<TNode> GetAncestorsCore(TNode node)
		{
			TNode? parent;
			while ((parent = node.Parent) is not null)
			{
				yield return parent;
				node = parent;
			}
		}
	}

	/// <summary>
	/// Crawls the ancestor lineage returns the first node with no parent.
	/// </summary>
	/// <typeparam name="TNode">The node type.</typeparam>
	/// <param name="node">The child node to start with.</param>
	/// <returns>The root node.</returns>
	public static TNode GetRoot<TNode>(
		this TNode node)
		where TNode : class, IChild<TNode>
	{
		if (node is null) throw new ArgumentNullException(nameof(node));
		TNode? parent;
		while ((parent = node.Parent) != null)
		{
			node = parent;
		}

		return node;
	}
}
