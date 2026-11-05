using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Bakery.Reviews.Infrastructure.Persistence.Serialization;

public sealed class StringEnumSerializer<TValue> : SerializerBase<TValue>
    where TValue : struct, Enum
{
    public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        var text = bsonType switch
        {
            BsonType.String => context.Reader.ReadString(),
            BsonType.Int32 => context.Reader.ReadInt32().ToString(),
            _ => throw new FormatException($"Очікувався рядок для enum {typeof(TValue).Name}, отримано {bsonType}."),
        };

        if (!Enum.TryParse<TValue>(text, ignoreCase: false, out var value))
        {
            throw new FormatException($"Значення «{text}» не є допустимим для {typeof(TValue).Name}.");
        }

        return value;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
    {
        context.Writer.WriteString(value.ToString());
    }
}
