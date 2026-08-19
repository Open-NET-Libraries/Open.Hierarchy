using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.Hierarchy.Tests;

[TestClass]
public class CycleGuardTests
{
	static (Node<string>.Factory factory, Node<string> root, Node<string> child, Node<string> grandchild) Chain()
	{
		var factory = new Node<string>.Factory();
		var root = factory.GetBlankNode(); root.Value = "root";
		var child = factory.GetBlankNode(); child.Value = "child";
		var grandchild = factory.GetBlankNode(); grandchild.Value = "grandchild";
		root.Add(child);
		child.Add(grandchild);
		return (factory, root, child, grandchild);
	}

	[TestMethod]
	public void Add_RejectsAnAncestor_AndSelf()
	{
		var (factory, root, child, grandchild) = Chain();
		using (factory)
		{
			// The root is parentless, so the old guard let it through -- and made a cycle.
			Assert.ThrowsException<InvalidOperationException>(() => grandchild.Add(root));
			Assert.ThrowsException<InvalidOperationException>(() => grandchild.Add(grandchild));
			Assert.ThrowsException<InvalidOperationException>(() => grandchild.Insert(0, root));
			Assert.AreEqual(0, grandchild.Count, "nothing was attached");
		}
	}

	[TestMethod]
	public void Replace_RejectsAnAncestorAsReplacement()
	{
		var (factory, root, child, grandchild) = Chain();
		using (factory)
		{
			Assert.ThrowsException<InvalidOperationException>(() => child.Replace(grandchild, root));
			Assert.AreSame(grandchild, child[0], "the original child is untouched");
		}
	}

	[TestMethod]
	public void LegitimateAttaches_StillWork()
	{
		var (factory, root, child, grandchild) = Chain();
		using (factory)
		{
			// A detached node, a sibling sub-tree, and a replacement from elsewhere are all fine.
			var other = factory.GetBlankNode(); other.Value = "other";
			grandchild.Add(other);
			Assert.AreSame(grandchild, other.Parent);

			var detached = factory.GetBlankNode(); detached.Value = "detached";
			child.Replace(grandchild, detached);
			Assert.AreSame(child, detached.Parent);
			Assert.IsNull(grandchild.Parent);

			// The formerly-attached sub-tree can be re-homed under a different branch.
			root.Add(grandchild);
			Assert.AreSame(root, grandchild.Parent);

			// And the whole thing still traverses finitely.
			Assert.AreEqual(4, root.GetDescendants().Count());
		}
	}
}
