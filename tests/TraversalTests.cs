using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.Hierarchy.Tests;

[TestClass]
public class TraversalTests
{
	// Builds:  root
	//          ├── a
	//          │   ├── a1
	//          │   └── a2
	//          └── b
	//              └── b1
	static (Node<string>.Factory factory, Node<string> root) Tree()
	{
		var factory = new Node<string>.Factory();
		var root = factory.GetBlankNode(); root.Value = "root";
		var a = factory.GetBlankNode(); a.Value = "a";
		var b = factory.GetBlankNode(); b.Value = "b";
		var a1 = factory.GetBlankNode(); a1.Value = "a1";
		var a2 = factory.GetBlankNode(); a2.Value = "a2";
		var b1 = factory.GetBlankNode(); b1.Value = "b1";
		root.Add(a); root.Add(b);
		a.Add(a1); a.Add(a2);
		b.Add(b1);
		return (factory, root);
	}

	private static readonly string[] Expected01 = ["a1", "a2", "a", "b1", "b"];
	private static readonly string[] Expected02 = ["a", "b", "a1", "a2", "b1"];
	private static readonly string[] Expected03 = ["a1", "a2", "a", "b1", "b", "root"];

	[TestMethod]
	public void DepthFirst_YieldsEachDescendantOnce_PostOrder()
	{
		var (factory, root) = Tree();
		using (factory)
		{
			var values = root.GetDescendants(TraversalMode.DepthFirst).Cast<Node<string>>().Select(n => n.Value).ToArray();
			// Post-order: a's children, then a; b's child, then b. Nothing repeated.
			CollectionAssert.AreEqual(Expected01, values);
			Assert.AreEqual(values.Length, values.Distinct().Count(), "every descendant exactly once");
		}
	}

	[TestMethod]
	public void BreadthFirst_YieldsEachDescendantOnce()
	{
		var (factory, root) = Tree();
		using (factory)
		{
			var values = root.GetDescendants(TraversalMode.BreadthFirst).Cast<Node<string>>().Select(n => n.Value).ToArray();
			CollectionAssert.AreEquivalent(Expected02, values);
			Assert.AreEqual(values.Length, values.Distinct().Count());
		}
	}

	[TestMethod]
	public void GetNodes_DepthFirst_EndsWithRoot_NoDuplicates()
	{
		var (factory, root) = Tree();
		using (factory)
		{
			var values = root.GetNodes(TraversalMode.DepthFirst).Cast<Node<string>>().Select(n => n.Value).ToArray();
			CollectionAssert.AreEqual(Expected03, values);
		}
	}
}
