using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Open.Hierarchy;

public sealed partial class Node<T>
{
	/// <summary>
	/// Clones this node by recreating the tree and copying the values.
	/// The resultant clone does not belong to any tree (detached) unless <paramref name="newParentForClone"/> is specified.
	/// </summary>
	/// <param name="newParentForClone">
	/// If a parent is specified it will use that node as its parent.
	/// By default it ends up being detached.
	/// </param>
	/// <param name="onNodeCloned">A function that receives the old node and its clone.</param>
	/// <returns>The copy of the tree/branch.</returns>
	public Node<T> Clone(
		Node<T>? newParentForClone = null,
		Action<Node<T>, Node<T>>? onNodeCloned = null)
	{
		if (newParentForClone != null && newParentForClone._factory != _factory)
			throw new ArgumentException("The node being provided for cloning does not belong to this factory.", nameof(newParentForClone));
		Contract.EndContractBlock();

		AssertNotRecycled();

		var clone = _factory.GetBlankNode();
		clone.Value = Value;
		newParentForClone?.Add(clone);

		foreach (var child in _children)
			child.Clone(clone, onNodeCloned);

		clone.Unmapped = Unmapped;

		onNodeCloned?.Invoke(this, clone);

		return clone;
	}

	/// <summary>
	/// Clones a node by recreating the tree and copying the values.
	/// The resultant clone is detached (is its own root).
	/// </summary>
	/// <param name="onNodeCloned">A function that receives the old node and its clone.</param>
	/// <returns>The copy of the tree/branch.</returns>
	public Node<T> Clone(
		Action<Node<T>, Node<T>> onNodeCloned)
		=> Clone(null, onNodeCloned);

	/// <summary>
	/// Create's a clone of the entire tree but only returns the clone of this node.
	/// </summary>
	/// <returns>A clone of this node as part of a newly cloned tree.</returns>
	[SuppressMessage("Performance",
		"HAA0302:Display class allocation to capture closure",
		Justification = "Must be done this way.")]
	[SuppressMessage("Performance",
		"HAA0301:Closure Allocation Source",
		Justification = "Must be done this way.")]
	public Node<T> CloneTree()
	{
		Node<T>? node = null;
		_ = Root.Clone((original, clone) =>
		{
			if (original == this)
				node = clone;
		});

		Debug.Assert(node != null);
		return node!;
	}
}
