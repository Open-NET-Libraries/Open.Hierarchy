using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Open.Disposable;

namespace Open.Hierarchy
{
	public sealed class Node<T> : INode<Node<T>>
	{
		#region IChild<Node<T>> Implementation
		Node<T> _parent;
		public Node<T> Parent => _parent;
		object IChild.Parent => _parent;
		#endregion

		#region IParent<Node<T>> Implementation
		readonly List<Node<T>> _children;
		public IReadOnlyList<Node<T>> Children { get; private set; }
		IReadOnlyList<object> IParent.Children => Children;
		#endregion

		public T Value { get; set; }

		Node()
		{
			_children = new List<Node<T>>();
			Children = _children.AsReadOnly();
		}

		// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

		#region ICollection<Node<T>> Implementation
		public bool IsReadOnly => false;

		public bool Contains(Node<T> node)
		{
			return _children.Contains(node);
		}

		public bool Remove(Node<T> node)
		{
			if (_children.Remove(node))
			{
				node._parent = null; // Need to be very careful about retaining parent references as it may cause a 'leak' per-se.
				return true;
			}
			return false;
		}

		public void Add(Node<T> node)
		{
			if (node._parent != null)
			{
				if (node._parent == this)
					throw new InvalidOperationException("Adding a child node more than once.");
				throw new InvalidOperationException("Adding a node that belongs to another parent.");
			}
			node._parent = this;
			_children.Add(node);
		}

		public void Clear()
		{
			foreach (var c in _children)
				c._parent = null;
			_children.Clear();
		}


		public int Count => _children.Count;

		public IEnumerator<Node<T>> GetEnumerator()
		{
			return _children.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void CopyTo(Node<T>[] array, int arrayIndex)
		{
			_children.CopyTo(array, arrayIndex);
		}
		#endregion

		public void Replace(Node<T> node, Node<T> replacement)
		{
			if (replacement._parent != null)
				throw new InvalidOperationException("Replacement node belongs to another parent.");
			var i = _children.IndexOf(node);
			if (i == -1)
				throw new InvalidOperationException("Node being replaced does not belong to this parent.");
			_children[i] = replacement;
			node._parent = null;
			replacement._parent = this;
		}

		public void Detatch()
		{
			_parent?.Remove(this);
			_parent = null;
		}


		/// <summary>
		/// Finds the root node of this tree.
		/// </summary>
		public Node<T> Root
		{
			get
			{
				Node<T> current = this;
				Node<T> parent;
				while ((parent = current._parent) != null)
				{
					current = parent;
				}
				return current;
			}
		}

		object IHaveRoot.Root => Root;

		internal void Teardown()
		{
			Value = default(T);
			Detatch(); // If no parent then this does nothing...
			foreach (var c in _children)
			{
				c._parent = null; // Don't initiate a 'Detach' (which does a lookup) since we are clearing here;
				c.Teardown();
			}
			_children.Clear();
		}

		internal void Recycle(Factory factory)
		{
			if (factory == null) throw new ArgumentNullException("factory");

			Value = default(T);
			Detatch(); // If no parent then this does nothing...
			foreach (var c in _children)
			{
				c._parent = null; // Don't initiate a 'Detach' (which does a lookup) since we are clearing here;
				factory.RecycleInternal(c);
			}
			_children.Clear();
		}


		/// <summary>
		/// Used for mapping a tree of evaluations which do not have access to their parent nodes.
		/// </summary>
		public class Factory : DisposableBase
		{
			#region Creation, Recycling, and Disposal
			public Factory()
			{
				Pool = new ConcurrentQueueObjectPool<Node<T>>(
					() => new Node<T>(), PrepareForPool, ushort.MaxValue);
			}

			protected override void OnDispose(bool calledExplicitly)
			{
				if (calledExplicitly)
				{
					DisposeOf(ref Pool);
				}
			}

			ConcurrentQueueObjectPool<Node<T>> Pool;

			public void Recycle(Node<T> n)
			{
				AssertIsAlive();

				RecycleInternal(n);
			}

			internal void RecycleInternal(Node<T> n)
			{
				var p = Pool;
				if (p == null) n.Teardown();
				else p.Give(n);
			}

			void PrepareForPool(Node<T> n)
			{
				n.Recycle(this);
			}
			#endregion

			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="target">The node to replicate.</param>
			/// <param name="newParentForClone">If a parent is specified it will use that node as its parent.  By default it ends up being detatched.</param>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(
				Node<T> target,
				Node<T> newParentForClone = null,
				Action<Node<T>, Node<T>> onNodeCloned = null)
			{
				AssertIsAlive();

				var clone = Pool.Take();
				clone.Value = target.Value;
				newParentForClone?.Add(clone);

				foreach (var child in target._children)
					clone.Add(Clone(child, clone, onNodeCloned));

				onNodeCloned?.Invoke(target, clone);

				return clone;
			}


			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="target">The node to replicate.</param>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(Node<T> target, Action<Node<T>, Node<T>> onNodeCloned)
			{
				return Clone(target, null, onNodeCloned);
			}

			/// <summary>
			/// Create's a clone of the entire tree but only returns the clone of this node.
			/// </summary>
			/// <returns>A clone of this node as part of a newly cloned tree.</returns>
			public Node<T> CloneTree(Node<T> target)
			{
				Node<T> node = null;
				Clone(target.Root, (n, clone) =>
				{
					if (n == target) node = clone;
				});
				return node;
			}

			/// <summary>
			/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <typeparam name="T">Child type.</typeparam>
			/// <typeparam name="TRoot">The type of the root.</typeparam>
			/// <param name="root">The root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map<TRoot>(TRoot root)
				where TRoot : T
			{
				AssertIsAlive();

				var current = Pool.Take();
				current.Value = root;

				if (root is IParent<T> parent)
				{
					foreach (var child in parent.Children)
					{
						current.Add(Map<T>(child));
					}
				}

				return current;
			}

			/// <summary>
			/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <typeparam name="T">The type of the root.</typeparam>
			/// <param name="root">The root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map(T root)
			{
				return Map<T>(root);
			}

			/// <summary>
			/// Generates a full hierarchy if the root of the container is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <typeparam name="T">The type of the root.</typeparam>
			/// <param name="container">The container of the root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map<TRoot>(IHaveRoot<TRoot> container)
				where TRoot : T
			{
				return Map(container.Root);
			}

		}

	}

}