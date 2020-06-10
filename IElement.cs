using System.Diagnostics.CodeAnalysis;

namespace Open.Hierarchy
{
	public interface IElement<T>
	{
#if NETSTANDARD2_1
		[AllowNull]
#endif
		T Value { get; set; }
	}
}
