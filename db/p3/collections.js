// ============================================================================
// ПРОЄКТ №3: База даних відгуків та модерації (BakeryReviewsDB)
// Стек: MongoDB (mongosh / JavaScript)
// Архітектурні патерни: Embedding, Referencing, Hybrid approach
// ============================================================================

db = db.getSiblingDB('BakeryReviewsDB');

// 1. Колекція відгуків (reviews) - Гібридний підхід
// Включає Embedding (вкладений об'єкт оцінок ratings, масив тегів)
// Та Referencing (посилання на orderId та customerId без FK)
// Також денормалізовані поля: productName, customerName
db.createCollection('reviews', {
    validator: {
        $jsonSchema: {
            bsonType: 'object',
            required: ['orderId', 'customerId', 'customerName', 'productId', 'productName', 'ratings', 'createdAt'],
            properties: {
                orderId: { bsonType: 'int', description: 'Логічне посилання на OrdersDB' },
                customerId: { bsonType: 'int', description: 'Логічне посилання на Customers' },
                customerName: { bsonType: 'string', description: 'Денормалізоване ім\'я згідно політики дублювання' },
                productId: { bsonType: 'int', description: 'Логічне посилання на CatalogDB' },
                productName: { bsonType: 'string', description: 'Денормалізована назва товару' },
                comment: { bsonType: 'string', maxLength: 1000 },
                ratings: {
                    bsonType: 'object',
                    required: ['taste', 'freshness', 'aroma'],
                    properties: {
                        taste: { bsonType: 'int', minimum: 1, maximum: 5 },
                        freshness: { bsonType: 'int', minimum: 1, maximum: 5 },
                        aroma: { bsonType: 'int', minimum: 1, maximum: 5 }
                    }
                },
                tags: {
                    bsonType: 'array',
                    items: { bsonType: 'string' }
                },
                adminResponse: {
                    bsonType: 'object',
                    properties: {
                        moderatorId: { bsonType: 'string' },
                        message: { bsonType: 'string' },
                        respondedAt: { bsonType: 'date' }
                    }
                },
                createdAt: { bsonType: 'date' }
            }
        }
    }
});

// 2. Колекція агрегованих рейтингів товарів (product_ratings) - Referencing + Precomputed aggregates
db.createCollection('product_ratings', {
    validator: {
        $jsonSchema: {
            bsonType: 'object',
            required: ['productId', 'totalReviews', 'averageScore'],
            properties: {
                productId: { bsonType: 'int' },
                totalReviews: { bsonType: 'int' },
                averageScore: { bsonType: 'double' },
                lastUpdated: { bsonType: 'date' }
            }
        }
    }
});

// 3. Журнал аудиту та модерації (moderation_logs) - Time-series / Referencing
db.createCollection('moderation_logs', {
    validator: {
        $jsonSchema: {
            bsonType: 'object',
            required: ['reviewId', 'action', 'moderator', 'timestamp'],
            properties: {
                reviewId: { bsonType: 'objectId' },
                action: { enum: ['Approved', 'Rejected', 'Flagged', 'Replied'] },
                moderator: { bsonType: 'string' },
                notes: { bsonType: 'string' },
                timestamp: { bsonType: 'date' }
            }
        }
    }
});

// Індекси для швидкого пошуку відгуків за товаром та клієнтом
db.reviews.createIndex({ productId: 1, createdAt: -1 });
db.reviews.createIndex({ customerId: 1 });
db.product_ratings.createIndex({ productId: 1 }, { unique: true });
