using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Common.AspNet.ModelBinding;

/// <summary>
/// Used to create a single request class which contains parameters from: path, query.
/// Does not support header, since the header is a greedy binding source -
///   it binds a model in a single operation, without decomposing the model into sub-properties 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromMultiSourceAttribute : Attribute, IBindingSourceMetadata
{
    public BindingSource BindingSource { get; } = CompositeBindingSource.Create(
        new[] { BindingSource.Path, BindingSource.Query },
        nameof(FromMultiSourceAttribute));
}
