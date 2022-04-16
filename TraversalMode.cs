namespace Open.Hierarchy;

/// <summary>
/// Options for traversal.
/// </summary>
public enum TraversalMode
{
	/// <summary>
	/// Will crawl all the way to the leaves before moving on.
	/// </summary>
	DepthFirst,
	/// <summary>
	/// Will crawl the chidren first before crawling the grandchildren, and so-on.
	/// </summary>
	BreadthFirst
}
