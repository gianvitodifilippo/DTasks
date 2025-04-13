//using DTasks.Infrastructure.Marshaling;
//using System.Diagnostics;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace DTasks.Serialization.Json.Converters;

//internal sealed class MarshalableConverter<TMarshalable> : JsonConverter<TMarshalable>
//{
//    private IDAsyncSurrogator? _surrogator;

//    public override TMarshalable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//    {
//        if (_surrogator is null)
//            return (TMarshalable?)JsonSerializer.Deserialize(ref reader, typeToConvert, options);

//    }

//    public override void Write(Utf8JsonWriter writer, TMarshalable value, JsonSerializerOptions options)
//    {
//        throw new NotImplementedException();
//    }

//    public SurrogatorScope UseSurrogator(IDAsyncMarshaler surrogator)
//    {
//        Debug.Assert(_surrogator is null);

//        _surrogator = surrogator;
//        return new SurrogatorScope(this);
//    }

//    public readonly ref struct SurrogatorScope(MarshalableConverter<TMarshalable> converter)
//    {
//        public void Dispose()
//        {
//            Debug.Assert(converter._surrogator is not null);
//            converter._surrogator = null;
//        }
//    }
//}


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json.Converters;

public class MarshalableCollectionConverterFactory<TMarshalable> : JsonConverterFactory
{
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type typeToConvert)
    {
        throw new NotImplementedException();
    }
}

public class MyClass;

public class MyConverterFactory : JsonConverterFactory
{
    public bool _canConvert = true;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new MyConverter(this);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return _canConvert;
    }
}

public class MyConverter(MyConverterFactory factory) : JsonConverter<MyClass>
{
    public override MyClass? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, MyClass value, JsonSerializerOptions options)
    {
        var converter = (JsonConverter<MyClass>)options.GetTypeInfo(typeof(MyClass)).Converter;
        converter.Write(writer, value, options);
    }
}