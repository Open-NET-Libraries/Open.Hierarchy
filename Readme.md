# Open.Hierarchy

![.NET Core](https://github.com/Open-NET-Libraries/Open.Hierarchy/workflows/.NET%20Core/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Open.Hierarchy.svg)](https://www.nuget.org/packages/Open.Hierarchy/)

## `Node<T>`

One of the important abilities of `Node<T>` and its supporting classes is to allow for creating and modifying tree structures that only have a parent to child relationship.  It's important to ensure that a node cannot occur multiple times in a tree but an instance of its value can occur any number of times. This facilitates potential 'value sub-trees' that can have duplicate references but not duplicate instances.  By using `Node<T>` as a container, a single instance can exist multiple times in a tree but still be uniquely identifiable by its position.

## `Node<T>.Factory`

### Blank Instance

Calling `.GetBlankNode()` will retrieve a blank node from the underlying object pool or create a new one.

### Mapping

Calling `.Map(root)` generates node hierarchy map based upon if the root or any of its children implement `IParent`.

### Cloning

Calling `.Clone(node)` creates a copy of the node map.

### Recycling

`Node<T>` instances can be recycled by calling the `Factory.Recycle(node)` method.  The node itself and its children are torn down and recycled to an object pool.

***WARNING:*** The `Factory.Recycle(node)` method is the one point of potential trouble if multiple references are retained.  It must be used with care.
