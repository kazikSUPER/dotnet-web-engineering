using Bakery.Reviews.Domain.ValueObjects;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Bakery.Reviews.Infrastructure.Persistence.Serialization;

public sealed class PersonNameSerializer : SerializerBase<PersonName>
{
    public override PersonName Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new PersonName(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PersonName value)
    {
        context.Writer.WriteString(value.Value);
    }
}

public sealed class ReviewCommentSerializer : SerializerBase<ReviewComment>
{
    public override ReviewComment Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new ReviewComment(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ReviewComment value)
    {
        context.Writer.WriteString(value.Text);
    }
}
