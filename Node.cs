using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Open.Hierarchy
{
	public sealed partial class Node<T> : INode<Node<T>>, IElement<T>
	{
		public Node<T>? Parent { get; private set; }
		object? IChild.Parent => Parent;

		#region IParent<Node<T>> Implementation
		private readonly List<Node<T>> _children;
		private readonly IReadOnlyList<Node<T>> _childrenReadOnly;

		/// <inheritdoc />
		public IReadOnlyList<Node<T>> Children => EnsureChildrenMapped();
		/// <inheritdoc />
		IReadOnlyList<object> IParent.Children => EnsureChildrenMapped();
		#endregion

#pragma warning disable IDE0044 // Add readonly modifier
		private bool _recycled;
#pragma warning restore IDE0044 // Add readonly modifier
		void AssertNotRecycled()
		{
			if (_recycled)
				throw new InvalidOperationException("Attempting to modify a node that has been recyled.");
		}

#if NETSTANDARD2_1
		[AllowNull]
#endif
		private T _value;

		/// <summary>
		/// The value for the node to hold on to.
		/// </summary>
#if NETSTANDARD2_1
		[AllowNull]
#endif
		public T Value
		{
			get => _value;
			set
			{
				AssertNotRecycled();
				Unmapped = false;
				_value = value;
			}
		}

		private readonly Factory _factory;

		/// <summary>
		/// Indicates that this node is in a state of deferred mapping.
		/// Will be false if not created by calling Factory.Map or if the value has been mapped.
		/// Since querying the chidren of this node will cause the value to be mapped, it can be useful to query this value before attempting traversal.
		/// </summary>
		public bool Unmapped { get; private set; }

		IReadOnlyList<Node<T>> EnsureChildrenMapped()
		{
			if (!Unmapped)
				return _childrenReadOnly;

			// Need to avoid double mapping and this method is primarily called when 'reading' from the node and contention will only occur if mapping is needed.
			lock (_children)
			{
				if (!Unmapped) return _childrenReadOnly;
				if (_value is IParent<T> p)
				{
					foreach (var child in p.Children)
					{
						var childNode = _factory.Map(child);
						childNode.Parent = this;
						_children.Add(childNode);
					}

				}
				Unmapped = false;
			}

			return _childrenReadOnly;
		}

		Node(Factory factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_children = new List<Node<T>>();
			_childrenReadOnly = _children.AsReadOnly();
			_value = default!;
		}

		// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

		#region IList<Node<T>> Implementation

		public int IndexOf(Node<T> child)
		{
			EnsureChildrenMapped();
			return _children.IndexOf(child);
		}

		void AssertOkToJoinFamily(Node<T> child)
		{
			if (child.Parent is null) return;
			if (child.Parent == this)
				throw new InvalidOperationException("Provided node already belongs to this parent.");
			throw new InvalidOperationException("Provided node belongs to another parent.");
		}

		public void Insert(int index, Node<T> child)
		{
			if (child is null) throw new ArgumentNullException(nameof(child));
			Contract.EndContractBlock();

			AssertNotRecycled();
			AssertOkToJoinFamily(child);

			EnsureChildrenMapped(); // Adding to potentially existing nodes.
			child.Parent = this;
			_children.Add(child);
		}

		// ReSharper disable once UnusedMethodReturnValue.Global
		public Node<T> RemoveAt(int index)
		{
			if (index < 0 && index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
			Contract.EndContractBlock();

			AssertNotRecycled();
			var child = this[index];
			_children.Remove(child);

			Unmapped = false;
			child.Parent = null; // Need to be very careful about retaining parent references as it may cause a 'leak' per-se.
			return child;
		}

		void IList<Node<T>>.RemoveAt(int index)
			=> RemoveAt(index);

		public Node<T> this[int index]
		{
			get => EnsureChildrenMapped()[index];
			set => Replace(EnsureChildrenMapped()[index], value);
		}

		#region ICollection<Node<T>> Implementation
		/// <inheritdoc />
		public bool IsReadOnly => false;

		/// <inheritdoc />
		public bool Contains(Node<T> child)
		{
			if (child is null) throw new ArgumentNullException(nameof(child));
			Contract.EndContractBlock();

			return _children.Contains(child);
		}

		/// <inheritdoc />
		public bool Remove(Node<T> child)
		{
			if (child is null) throw new ArgumentNullException(nameof(child));
			Contract.EndContractBlock();

			if (!_children.Remove(child)) return false;

			AssertNotRecycled();
			Unmapped = false;
			child.Parent = null; // Need to be very careful about retaining parent references as it may cause a 'leak' per-se.
			return true;
		}

		/// <inheritdoc />
		public void Add(Node<T> child)
		{
			if (child is null) throw new ArgumentNullException(nameof(child));
			Contract.EndContractBlock();

			AssertNotRecycled();
			AssertOkToJoinFamily(child);

			EnsureChildrenMapped(); // Adding to potentially existing nodes.
			child.Parent = this;
			_children.Add(child);
		}

		/// <inheritdoc />
		public void Clear()
		{
			if (_children.Count == 0) return;

			AssertNotRecycled();
			Unmapped = false;
			foreach (var c in _children)
				c.Parent = null;
			_children.Clear();
		}

		/// <inheritdoc />
		public int Count => EnsureChildrenMapped().Count;

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
		#endregion

		/// <summary>
		/// Gets a new node with the provided value and adds it as a child.
		/// </summary>
		/// <param name="value">The value of the new child.</param>
		/// <param name="asUnmapped">If true adds the node if it has been prepared for mapping but not yet checked if the value is an IParent.  If value is not an IParent, then this flag does nothing.</param>
		public void AddValue(T value, bool asUnmapped = false)
		{
			AssertNotRecycled();
			Add(_factory.GetNodeWithValue(value, asUnmapped));
		}

		/// <summary>
		/// Replaces an existing node within it's tree with another node.
		/// </summary>
		/// <param name="node">The node to be replaced.</param>
		/// <param name="replacement">The node to use as a replacement.</param>
		public void Replace(Node<T> node, Node<T> replacement)
		{
			if (node is null) throw new ArgumentNullException(nameof(node));
			if (replacement is null) throw new ArgumentNullException(nameof(replacement));
			Contract.EndContractBlock();

			if (replacement.Parent != null)
				throw new InvalidOperationException("Replacement node belongs to another parent.");
			var i = _children.IndexOf(node);
			if (i == -1)
				throw new InvalidOperationException("Node being replaced does not belong to this parent.");

			AssertNotRecycled();
			Unmapped = false;
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
				Node<T>? parent;
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
			Unmapped = false;
			Value = default!;
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
			Unmapped = false;
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
#if NETSTANDARD2_1
		[return: MaybeNull]
#endif
		// ReSharper disable once UnusedMethodReturnValue.Global
		public T Recycle()
		{
			AssertNotRecycled(); // Avoid double entry in the pool.
			Unmapped = false;
			var value = Value;
			Value = default!;
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

			Unmapped = false;
			foreach (var c in _children)
			{
				c.Parent = null; // Don't initiate a 'Detach' (which does a lookup) since we are clearing here;
				_factory.RecycleInternal(c);
			}
			_children.Clear();
		}


	}

}
