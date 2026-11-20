using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using Bakery.Reviews.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Seeding;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}

public sealed class MongoDataSeeder(
    MongoDbContext db,
    IOptions<MongoDbOptions> options,
    IProductRatingRepository ratings,
    ILogger<MongoDataSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!options.Value.SeedOnStartup)
        {
            logger.LogInformation("Початкове наповнення вимкнено налаштуванням MongoDb:SeedOnStartup");

            return;
        }

        var seed = BuildReviews();
        var inserted = 0;

        foreach (var review in seed)
        {
            if (await ExistsAsync(review.OrderId, review.ProductId, ct))
            {
                continue;
            }

            await db.Reviews.InsertOneAsync(db.Session, review, ct);
            inserted++;
        }

        foreach (var productId in seed.Select(x => x.ProductId).Distinct())
        {
            await ratings.RecalculateAsync(productId, ct);
        }

        await SeedModerationLogAsync(ct);

        logger.LogInformation(
            "Ідемпотентне наповнення MongoDB завершено: додано {Inserted} відгуків зі {Total} можливих",
            inserted,
            seed.Count);
    }

    private static List<Review> BuildReviews()
    {
        var croissant = Review.Create(
            1,
            1,
            "Олена Коваль",
            1,
            "Круасан класичний масляний",
            "Неймовірно ніжний та хрусткий круасан! Відчувається справжнє якісне вершкове масло.",
            5,
            5,
            5,
            ["хрусткий", "вершковий", "рекомендую"]);

        croissant.Approve();
        croissant.AddAdminResponse(new AdminResponse(
            "admin_petro",
            "Дякуємо за теплий відгук, Олено! Печемо для вас щоранку з любов'ю.",
            DateTime.UtcNow));

        var baguette = Review.Create(
            1,
            1,
            "Олена Коваль",
            2,
            "Багет традиційний французький",
            "Чудовий багет, пористий м'якуш і хрумка скоринка. Ідеально до сніданку.",
            5,
            4,
            5,
            ["традиційний", "хліб"]);

        baguette.Approve();

        var cheesecake = Review.Create(
            2,
            2,
            "Тарас Шевчук",
            3,
            "Чізкейк Сан-Себастьян",
            "Кремова ніжна текстура, приємний баланс солодкого та карамельного смаку.",
            5,
            5,
            4,
            ["десерт", "чізкейк", "карамель"]);

        return [croissant, baguette, cheesecake];
    }

    private async Task<bool> ExistsAsync(int orderId, int productId, CancellationToken ct)
    {
        var builder = Builders<Review>.Filter;

        return await db.Reviews
            .Find(db.Session, builder.Eq(x => x.OrderId, orderId) & builder.Eq(x => x.ProductId, productId))
            .AnyAsync(ct);
    }

    private async Task SeedModerationLogAsync(CancellationToken ct)
    {
        var builder = Builders<Review>.Filter;

        var review = await db.Reviews
            .Find(db.Session, builder.Eq(x => x.OrderId, 1) & builder.Eq(x => x.ProductId, 1))
            .FirstOrDefaultAsync(ct);

        if (review is null)
        {
            return;
        }

        var logBuilder = Builders<ModerationLog>.Filter;
        var exists = await db.ModerationLogs
            .Find(db.Session, logBuilder.Eq(x => x.ReviewId, review.Id) & logBuilder.Eq(x => x.Action, ModerationAction.Approved))
            .AnyAsync(ct);

        if (exists)
        {
            return;
        }

        await db.ModerationLogs.InsertOneAsync(
            db.Session,
            new ModerationLog(review.Id, ModerationAction.Approved, "admin_petro", "Відгук відповідає правилам спільноти"),
            ct);
    }
}
