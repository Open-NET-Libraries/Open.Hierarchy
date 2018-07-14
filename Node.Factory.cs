using Open.Disposable;
using System;

namespace Open.Hierarchy
{
	public sealed partial class Node<T>
	{
		/// <summary>
		/// Used for mapping a tree of evaluations which do not have access to their parent nodes.
		/// </summary>
		public sealed class Factory : DisposableBase
		{
			#region Creation, Recycling, and Disposal
			public Factory()
			{
				_pool = new ConcurrentQueueObjectPool<Node<T>>(
					() => new Node<T>(), PrepareForPool, null, ushort.MaxValue);
			}

			protected override void OnDispose(bool calledExplicitly)
			{
				if (calledExplicitly)
				{
					DisposeOf(ref _pool);
				}
			}

			ConcurrentQueueObjectPool<Node<T>> _pool;

			/// <summary>
			/// Gets a blank node.
			/// </summary>
			/// <returns>A blank node.</returns>
			public Node<T> GetBlankNode()
			{
				AssertIsAlive();

				return _pool?.Take();
			}

			/// <summary>
			/// Recycles the node into the object pool, returning the value contained.
			/// </summary>
			/// <param name="n">The node to be recycled.</param>
			/// <returns>The value contained in the node.</returns>
			public T Recycle(Node<T> n)
			{
				AssertIsAlive();

				return RecycleInternal(n);
			}

			internal T RecycleInternal(Node<T> n)
			{
				var value = n.Value;
				var p = _pool;
				if (p == null) n.Teardown();
				else p.Give(n);
				return value;
			}

			void PrepareForPool(Node<T> n)
				=> n.Recycle(this);
			#endregion

			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="target">The node to replicate.</param>
			/// <param name="newParentForClone">
			/// If a parent is specified it will use that node as its parent.
			/// By default it ends up being detatched.
			/// </param>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(
				Node<T> target,
				Node<T> newParentForClone = null,
				Action<Node<T>, Node<T>> onNodeCloned = null)
			{
				AssertIsAlive();

				var clone = _pool.Take();
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
			public Node<T> Clone(
				Node<T> target,
				Action<Node<T>, Node<T>> onNodeCloned)
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
			/// <typeparam name="TRoot">The type of the root.</typeparam>
			/// <param name="root">The root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map<TRoot>(TRoot root)
				where TRoot : T
			{
				AssertIsAlive();

				var current = _pool.Take();
				current.Value = root;

				if (!(root is IParent<T> parent)) return current;
				foreach (var child in parent.Children)
				{
					current.Add(Map<T>(child));
				}

				return current;
			}

			/// <summary>
			/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
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
			/// <typeparam name="TRoot">The type of the root.</typeparam>
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
