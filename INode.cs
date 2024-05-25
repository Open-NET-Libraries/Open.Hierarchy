namespace Open.Hierarchy;

/// <summary>
/// Represents a node in a hierarchy that can have children and may have parents.
/// </summary>
/// <remarks>Does not inherently define if this node contains a value.</remarks>
public interface INode<TNode>
	: IList<TNode>, IChild<TNode>, IParent<TNode>, IHaveRoot<TNode>
	where TNode : class, INode<TNode>
{
}
