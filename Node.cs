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
		private readonly IReadOnlyList<Node<T>> _childrenReadOnly;

		/// <inheritdoc />
		public IReadOnlyList<Node<T>> Children => EnsureChildrenMapped();
		/// <inheritdoc />
		IReadOnlyList<object> IParent.Children => EnsureChildrenMapped();
		#endregion

		private bool _recycled;
		void AssertNotRecycled()
		{
			if (_recycled)
				throw new InvalidOperationException("Attempting to modify a node that has been recyled.");
		}

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		/// <summary>
		/// The value for the node to hold on to.
		/// </summary>
		private T _value;
		public T Value
		{
			get => _value;
			set
			{
				AssertNotRecycled();
				_needsMapping = false;
				_value = value;
			}
		}

		private readonly Factory _factory;
		private bool _needsMapping;

		IReadOnlyList<Node<T>> EnsureChildrenMapped()
		{
			if (!_needsMapping)
				return _childrenReadOnly;

			// Need to avoid double mapping and this method is primarily called when 'reading' from the node and contention will only occur if mapping is needed.
			lock (_children)
			{
				if (!_needsMapping) return _childrenReadOnly;
				if (_value is IParent<T> p)
				{
					foreach (var child in p.Children)
						_children.Add(_factory.Map(child));
				}
				_needsMapping = false;
			}

			return _childrenReadOnly;
		}

		internal Node(Factory factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_children = new List<Node<T>>();
			_childrenReadOnly = _children.AsReadOnly();
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

			AssertNotRecycled();
			_needsMapping = false;
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

			AssertNotRecycled();
			EnsureChildrenMapped(); // Adding to potentially existing nodes.
			node.Parent = this;
			_children.Add(node);
		}

		/// <inheritdoc />
		public void Clear()
		{
			if (_children.Count == 0) return;

			AssertNotRecycled();
			_needsMapping = false;
			foreach (var c in _children)
				c.Parent = null;
			_children.Clear();
		}


		/// <inheritdoc />
		public int Count => _children.Count;

		/// <inheritdoc />
		public IEnumerator<Node<T>> GetEnumerator()
			=> Children.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <inheritdoc />
		public void CopyTo(Node<T>[] array, int arrayIndex)
		{
			EnsureChildrenMapped();
			_children.CopyTo(array, arrayIndex);
		}
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

			AssertNotRecycled();
			_needsMapping = false;
			_children[i] = replacement;
			node.Parent = null;
			replacement.Parent = this;
		}

		/// <summary>
		/// Removes this node from its parent if it has one.
		/// </summary>
		public void Detatch()
		{
			AssertNotRecycled();
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
			_needsMapping = false;
			Value = default;
			Detatch(); // If no parent then this does nothing...
			TeardownChildren();
		}

		/// <summary>
		/// Cleans out all the child nodes and tears them down.
		/// </summary>
		public void TeardownChildren()
		{
			if (_children.Count == 0) return;

			AssertNotRecycled();
			_needsMapping = false;
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
		// ReSharper disable once UnusedMethodReturnValue.Global
		public T Recycle()
		{
			AssertNotRecycled(); // Avoid double entry in the pool.
			_needsMapping = false;
			var value = Value;
			Value = default;
			Detatch(); // If no parent then this does nothing...
			RecycleChildren();
			return value;
		}

		/// <summary>
		/// Recycles all the children of this node.
		/// </summary>
		public void RecycleChildren()
		{
			if (_children.Count == 0) return;

			_needsMapping = false;
			foreach (var c in _children)
			{
				c.Parent = null; // Don't initiate a 'Detach' (which does a lookup) since we are clearing here;
				_factory.RecycleInternal(c);
			}
			_children.Clear();
		}

	}

}
