using Open.Disposable;
using System;
using System.Diagnostics.Contracts;

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
					() => new Node<T>(this), PrepareForPool, null, ushort.MaxValue);
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
				var p = _pool;
				AssertIsAlive();

				var n = p.Take();
				n._recycled = false;
				return n;
			}

			/// <summary>
			/// Gets a blank node.
			/// </summary>
			/// <returns>A blank node.</returns>
			public Node<T> GetNodeWithValue(T value)
			{
				var n = GetBlankNode();
				n.Value = value;
				return n;
			}

			/// <summary>
			/// Recycles the node into the object pool, returning the value contained.
			/// </summary>
			/// <param name="node">The node to be recycled.</param>
			/// <returns>The value contained in the node.</returns>
			public T Recycle(Node<T> node)
			{
				if (node == null) throw new ArgumentNullException(nameof(node));
				if (node._factory != this)
					throw new ArgumentException("The node being provided for recycling does not belong to this factory.", nameof(node));
				Contract.EndContractBlock();

				return RecycleInternal(node);
			}

			internal T RecycleInternal(Node<T> n)
			{
				var value = n.Value;
				var p = _pool;
				if (p == null) n.Teardown();
				else p.Give(n);
				return value;
			}

			static void PrepareForPool(Node<T> n)
			{
				n.Recycle();
				n._recycled = true;
			}

			#endregion

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

				var current = GetBlankNode();
				current.Value = root;

				// Mapping is deferred and occurs on demand.
				// If values or children change in the node, mapping is disregarded.
				current.Unmapped = true;


				return current;
			}

			/// <summary>
			/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <param name="root">The root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map(T root) => Map<T>(root);

			/// <summary>
			/// Generates a full hierarchy if the root of the container is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <typeparam name="TRoot">The type of the root.</typeparam>
			/// <param name="container">The container of the root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map<TRoot>(IHaveRoot<TRoot> container)
				where TRoot : T => Map(container.Root);

		}

	}

}
