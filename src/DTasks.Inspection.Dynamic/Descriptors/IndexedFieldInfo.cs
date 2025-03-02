using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal readonly record struct IndexedFieldInfo(FieldInfo Field, int Index);
