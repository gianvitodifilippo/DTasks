using System.Diagnostics.CodeAnalysis;

namespace DTasks.Hosting;

public interface IDTaskScope
{
    bool CanProvide(object reference, [NotNullWhen(true)] out object? token);

    object GetReference(object token);
}
