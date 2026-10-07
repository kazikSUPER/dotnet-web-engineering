// ============================================================================
// ПРОЄКТ №3: Ідемпотентне початкове наповнення MongoDB (Upsert / Find-or-Create)
// Стек: MongoDB (mongosh / JavaScript)
// Повторний запуск не створює дублікатів і не видаляє наявні дані
// ============================================================================

db = db.getSiblingDB('BakeryReviewsDB');

// 1. Додавання відгуків з вкладеними об'єктами (Embedding) та денормалізацією через upsert
db.reviews.updateOne(
    { orderId: 1, productId: 1 },
    {
        $setOnInsert: {
            orderId: 1,
            customerId: 1,
            customerName: 'Олена Коваль',
            productId: 1,
            productName: 'Круасан класичний масляний',
            comment: 'Неймовірно ніжний та хрусткий круасан! Відчувається справжнє якісне вершкове масло.',
            ratings: {
                taste: 5,
                freshness: 5,
                aroma: 5
            },
            tags: ['хрусткий', 'вершковий', 'рекомендую'],
            adminResponse: {
                moderatorId: 'admin_petro',
                message: 'Дякуємо за теплий відгук, Олено! Печемо для вас щоранку з любов\'ю.',
                respondedAt: new Date('2026-09-28T12:00:00Z')
            },
            createdAt: new Date('2026-09-28T10:00:00Z')
        }
    },
    { upsert: true }
);

db.reviews.updateOne(
    { orderId: 1, productId: 2 },
    {
        $setOnInsert: {
            orderId: 1,
            customerId: 1,
            customerName: 'Олена Коваль',
            productId: 2,
            productName: 'Багет традиційний французький',
            comment: 'Чудовий багет, пористий м\'якуш і хрумка скоринка. Ідеально до сніданку.',
            ratings: {
                taste: 5,
                freshness: 4,
                aroma: 5
            },
            tags: ['традиційний', 'хліб'],
            createdAt: new Date('2026-09-28T10:05:00Z')
        }
    },
    { upsert: true }
);

db.reviews.updateOne(
    { orderId: 2, productId: 3 },
    {
        $setOnInsert: {
            orderId: 2,
            customerId: 2,
            customerName: 'Тарас Шевчук',
            productId: 3,
            productName: 'Чізкейк Сан-Себастьян',
            comment: 'Кремова ніжна текстура, приємний баланс солодкого та карамельного смаку.',
            ratings: {
                taste: 5,
                freshness: 5,
                aroma: 4
            },
            tags: ['десерт', 'чізкейк', 'карамель'],
            createdAt: new Date('2026-09-28T11:30:00Z')
        }
    },
    { upsert: true }
);

// 2. Додавання агрегованих оцінок через upsert за productId
db.product_ratings.updateOne(
    { productId: 1 },
    {
        $setOnInsert: {
            productId: 1,
            totalReviews: 1,
            averageScore: 5.0,
            lastUpdated: new Date('2026-09-28T10:00:00Z')
        }
    },
    { upsert: true }
);

db.product_ratings.updateOne(
    { productId: 2 },
    {
        $setOnInsert: {
            productId: 2,
            totalReviews: 1,
            averageScore: 4.67,
            lastUpdated: new Date('2026-09-28T10:05:00Z')
        }
    },
    { upsert: true }
);

db.product_ratings.updateOne(
    { productId: 3 },
    {
        $setOnInsert: {
            productId: 3,
            totalReviews: 1,
            averageScore: 4.67,
            lastUpdated: new Date('2026-09-28T11:30:00Z')
        }
    },
    { upsert: true }
);

// 3. Журнал модерації (Referencing на reviewId через find-or-create)
const sampleReview = db.reviews.findOne({ orderId: 1, productId: 1 });
if (sampleReview) {
    db.moderation_logs.updateOne(
        { reviewId: sampleReview._id, action: 'Approved' },
        {
            $setOnInsert: {
                reviewId: sampleReview._id,
                action: 'Approved',
                moderator: 'admin_petro',
                notes: 'Відгук відповідає правилам спільноти',
                timestamp: new Date('2026-09-28T10:30:00Z')
            }
        },
        { upsert: true }
    );
}
