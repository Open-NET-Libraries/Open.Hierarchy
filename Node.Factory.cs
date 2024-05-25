using Open.Disposable;

namespace Open.Hierarchy;

/// <inheritdoc />
public sealed partial class Node<T>
{
	/// <summary>
	/// Used for mapping a unidirectional tree which do not have access to their parent nodes.
	/// </summary>
	public sealed class Factory : DisposableBase
	{
		#region Creation, Recycling, and Disposal
		/// <summary>
		/// Constructs a <see cref="Node{T}.Factory"/> for building a hierarchy.
		/// </summary>
		[SuppressMessage("Performance",
			"HAA0302:Display class allocation to capture closure",
			Justification = "Must be done this way.")]
		[SuppressMessage("Performance",
			"HAA0301:Closure Allocation Source",
			Justification = "Must be done this way.")]
		public Factory()
		{
			_pool = new(() => new(this), PrepareForPool, null, ushort.MaxValue);
		}

		/// <inheritdoc />
		protected override void OnDispose() => DisposeOf(ref _pool);

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
		public Node<T> GetNodeWithValue(T value, bool asUnmapped = false)
		{
			var n = GetBlankNode();
			n.Value = value;
			n.Unmapped = asUnmapped;
			return n;
		}

		/// <summary>
		/// Recycles the node into the object pool, returning the value contained.
		/// </summary>
		/// <param name="node">The node to be recycled.</param>
		/// <returns>The value contained in the node.</returns>
#if NETSTANDARD2_1
		[return: MaybeNull]
#endif
		public T Recycle(Node<T> node)
		{
			if (node is null) throw new ArgumentNullException(nameof(node));
			if (node.Source != this)
				throw new ArgumentException("The node being provided for recycling does not belong to this factory.", nameof(node));
			Contract.EndContractBlock();

			return RecycleInternal(node);
		}

#if NETSTANDARD2_1
		[return: MaybeNull]
#endif
		internal T RecycleInternal(Node<T> n)
		{
			var value = n.Value;
			var p = _pool;
			if (p is null) n.Teardown();
			else p.Give(n);
			return value;
		}

		static readonly Action<Node<T>> PrepareForPool = (Node<T> n) =>
		{
			n.Recycle();
			n._recycled = true;
		};

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
			if (root is null) throw new ArgumentNullException(nameof(root));
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
			where TRoot : T
			=> container is null
				? throw new ArgumentNullException(nameof(container))
				: Map(container.Root);
	}
}
