using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Open.Hierarchy
{
	public interface IChild
	{
		/// <summary>
		/// The parent of this child.
		/// </summary>
		object? Parent { get; }
	}

	public interface IChild<out TParent> : IChild
		where TParent : class
	{
		/// <summary>
		/// The generic parent of this child.
		/// </summary>
		new TParent? Parent { get; }
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Null check properly implemented.")]
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
			if (node is null) throw new ArgumentNullException(nameof(node));
			TNode? parent;
			while ((parent = node.Parent) != null)
			{
				yield return parent;
				node = parent;
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

}
