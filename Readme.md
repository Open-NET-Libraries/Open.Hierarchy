# Open.Hierarchy

[![NuGet](https://img.shields.io/nuget/v/Open.Hierarchy.svg)](https://www.nuget.org/packages/Open.Hierarchy/)

## Introduction

Open.Hierarchy is a versatile library designed for creating and manipulating tree structures with a focus on efficiency and ease of use. With the `Node<T>` class and its supporting components, this library enables users to create tree structures that have a unidirectional parent-to-child relationship, enabling more efficient use of memory and simplified garbage collection.

## `Node<T>`

`Node<T>` is a versatile class that represents a node in a bidirectional tree. It can be used as-is or to act as a bidirectional map of a unidirectional tree. In a bidirectional tree, it is crucial to ensure that a node cannot occur multiple times, although its value can occur any number of times. This approach enables the creation of 'value sub-trees' with duplicate references but not duplicate instances. By using `Node<T>` as a container, a single instance can exist multiple times in a tree but still be uniquely identifiable by its position.

## `Node<T>.Factory`

The `Node<T>.Factory` class provides useful methods for creating, mapping, cloning, and recycling `Node<T>` instances:

### Blank Instance

- `.GetBlankNode()`: Retrieves a blank node from the underlying object pool or creates a new one.

### Mapping

- `.Map(root)`: Generates a node hierarchy map based on whether the root or any of its children implement the `IParent` interface.

### Cloning

- `.Clone(node)`: Creates a copy of the node map.

### Recycling

- `Factory.Recycle(node)`: Recycles a `Node<T>` instance by tearing down the node itself and its children, returning them to an object pool.

**WARNING:** The `Factory.Recycle(node)` method is a potential source of issues if multiple references are retained. It must be used with care to avoid unexpected behavior.
