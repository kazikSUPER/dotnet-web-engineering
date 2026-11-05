using System.Reflection;
using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.ValueObjects;
using Bakery.Reviews.Infrastructure.Persistence.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Bakery.Reviews.Infrastructure.Persistence;

public static class MongoDbSerialization
{
    private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;

    private static bool _initialized;

    public static void Register()
    {
        if (_initialized)
        {
            return;
        }

        RegisterConventions();
        RegisterValueSerializers();
        RegisterClassMaps();

        _initialized = true;
    }

    private static void RegisterConventions()
    {
        var conventions = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
        };

        ConventionRegistry.Register("ReviewsConventions", conventions, type => type.Assembly == DomainAssembly);
    }

    private static void RegisterValueSerializers()
    {
        BsonSerializer.RegisterSerializer(new PersonNameSerializer());
        BsonSerializer.RegisterSerializer(new ReviewCommentSerializer());
        BsonSerializer.RegisterSerializer(new StringEnumSerializer<ReviewStatus>());
        BsonSerializer.RegisterSerializer(new StringEnumSerializer<ModerationAction>());
    }

    private static void RegisterClassMaps()
    {
        RegisterIfMissing<BaseEntity>(cm => cm.MapIdMember(x => x.Id));

        RegisterIfMissing<Rating>();
        RegisterIfMissing<AdminResponse>();

        RegisterIfMissing<Review>(cm => cm.MapMember(TagsField()).SetElementName("tags"));

        RegisterIfMissing<ProductRating>();
        RegisterIfMissing<ModerationLog>();
    }

    private static void RegisterIfMissing<TClass>(Action<BsonClassMap<TClass>>? configure = null)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TClass)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<TClass>(cm =>
        {
            cm.AutoMap();
            configure?.Invoke(cm);
        });
    }

    private static MemberInfo TagsField()
    {
        var field = typeof(Review).GetField("tags", BindingFlags.Instance | BindingFlags.NonPublic);

        return field ?? throw new InvalidOperationException("У сутності Review не знайдено приватного поля tags.");
    }
}
