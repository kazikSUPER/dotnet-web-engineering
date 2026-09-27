// ============================================================================
// ПРОЄКТ №3: Ідемпотентне початкове наповнення MongoDB
// Стек: MongoDB (mongosh / JavaScript)
// ============================================================================

db = db.getSiblingDB('BakeryReviewsDB');

// Очищення перед наповненням
db.reviews.deleteMany({});
db.product_ratings.deleteMany({});
db.moderation_logs.deleteMany({});

// 1. Додавання відгуків з вкладеними об'єктами (Embedding) та денормалізацією
const review1 = db.reviews.insertOne({
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
});

const review2 = db.reviews.insertOne({
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
});

const review3 = db.reviews.insertOne({
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
});

// 2. Додавання агрегованих оцінок
db.product_ratings.insertMany([
    {
        productId: 1,
        totalReviews: 1,
        averageScore: 5.0,
        lastUpdated: new Date('2026-09-28T10:00:00Z')
    },
    {
        productId: 2,
        totalReviews: 1,
        averageScore: 4.67,
        lastUpdated: new Date('2026-09-28T10:05:00Z')
    },
    {
        productId: 3,
        totalReviews: 1,
        averageScore: 4.67,
        lastUpdated: new Date('2026-09-28T11:30:00Z')
    }
]);

// 3. Журнал модерації (Referencing на reviewId)
db.moderation_logs.insertOne({
    reviewId: review1.insertedId,
    action: 'Approved',
    moderator: 'admin_petro',
    notes: 'Відгук відповідає правилам спільноти',
    timestamp: new Date('2026-09-28T10:30:00Z')
});
