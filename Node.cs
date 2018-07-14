using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Open.Hierarchy
{
	public sealed partial class Node<T> : INode<Node<T>>
	{
		public Node<T> Parent { get; private set; }
		object IChild.Parent => Parent;

		#region IParent<Node<T>> Implementation
		private readonly List<Node<T>> _children;
		/// <inheritdoc />
		public IReadOnlyList<Node<T>> Children { get; }
		/// <inheritdoc />
		IReadOnlyList<object> IParent.Children => Children;
		#endregion

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		/// <summary>
		/// The value for the node to hold on to.
		/// </summary>
		public T Value { get; set; }

		private Node()
		{
			_children = new List<Node<T>>();
			Children = _children.AsReadOnly();
		}

		// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

		#region ICollection<Node<T>> Implementation
		/// <inheritdoc />
		public bool IsReadOnly => false;

		/// <inheritdoc />
		public bool Contains(Node<T> node)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			Contract.EndContractBlock();

			return _children.Contains(node);
		}

		/// <inheritdoc />
		public bool Remove(Node<T> node)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			Contract.EndContractBlock();

			if (!_children.Remove(node)) return false;
			node.Parent = null; // Need to be very careful about retaining parent references as it may cause a 'leak' per-se.
			return true;
		}

		/// <inheritdoc />
		public void Add(Node<T> node)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			Contract.EndContractBlock();

			if (node.Parent != null)
			{
				if (node.Parent == this)
					throw new InvalidOperationException("Adding a child node more than once.");
				throw new InvalidOperationException("Adding a node that belongs to another parent.");
			}
			node.Parent = this;
			_children.Add(node);
		}

		/// <inheritdoc />
		public void Clear()
		{
			foreach (var c in _children)
				c.Parent = null;
			_children.Clear();
		}


		/// <inheritdoc />
		public int Count => _children.Count;

		/// <inheritdoc />
		public IEnumerator<Node<T>> GetEnumerator()
			=> _children.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <inheritdoc />
		public void CopyTo(Node<T>[] array, int arrayIndex)
			=> _children.CopyTo(array, arrayIndex);
		#endregion

		/// <summary>
		/// Replaces an existing node within it's tree with another node.
		/// </summary>
		/// <param name="node">The node to be replaced.</param>
		/// <param name="replacement">The node to use as a replacement.</param>
		public void Replace(Node<T> node, Node<T> replacement)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (replacement == null) throw new ArgumentNullException(nameof(replacement));
			Contract.EndContractBlock();

			if (replacement.Parent != null)
				throw new InvalidOperationException("Replacement node belongs to another parent.");
			var i = _children.IndexOf(node);
			if (i == -1)
				throw new InvalidOperationException("Node being replaced does not belong to this parent.");
			_children[i] = replacement;
			node.Parent = null;
			replacement.Parent = this;
		}

		/// <summary>
		/// Removes this node from its parent if it has one.
		/// </summary>
		public void Detatch()
		{
			Parent?.Remove(this);
			Parent = null;
		}

		/// <inheritdoc />
		public Node<T> Root
		{
			get
			{
				var current = this;
				Node<T> parent;
				while ((parent = current.Parent) != null)
				{
					current = parent;
				}
				return current;
			}
		}

		object IHaveRoot.Root => Root;

		/// <summary>
		/// Tears down this node and its children.
		/// </summary>
		public void Teardown()
		{
			Value = default;
			Detatch(); // If no parent then this does nothing...
			TeardownChildren();
		}

		/// <summary>
		/// Cleans out all the child nodes and tears them down.
		/// </summary>
		public void TeardownChildren()
		{
			foreach (var c in _children)
			{
				c.Parent = null; // Don't initiate a 'Detach' (which does a lookup) since we are clearing here;
				c.Teardown();
			}
			_children.Clear();
		}

		/// <summary>
		/// Recycles this node.
		/// </summary>
		/// <param name="factory">The factory to use as a recycler.</param>
		// ReSharper disable once UnusedMethodReturnValue.Global
		public T Recycle(Factory factory)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));
			Contract.EndContractBlock();

			var value = Value;
			Value = default;
			Detatch(); // If no parent then this does nothing...
			RecycleChildren(factory);
			return value;
		}

		/// <summary>
		/// Recycles all the children of this node.
		/// </summary>
		/// <param name="factory">The factory to use as a recycler.</param>
		public void RecycleChildren(Factory factory)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));
			Contract.EndContractBlock();

			foreach (var c in _children)
			{
				c.Parent = null; // Don't initiate a 'Detach' (which does a lookup) since we are clearing here;
				factory.RecycleInternal(c);
			}
			_children.Clear();
		}

	}

}
