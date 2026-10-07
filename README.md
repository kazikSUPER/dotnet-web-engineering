# Craft Bakery Platform — Платформа крафтової пекарні

Навчальний семестровий проєкт із дисципліни **«Технології .NET у веб-інженерії»**.
Викладач: **Томка Ю.Я.** | Студент: **Казімір В.І.** (група 443-2).

---

## 1. Опис предметної області та архітектури

Проєкт являє собою розподілену систему для крафтової пекарні **«Craft Bakery»**, що забезпечує автоматизацію прийому онлайн-замовлень свіжої випічки, управління асортиментом та технологічними картами (рецептурою), а також збір і модерацію відгуків покупців.

Система спроєктована на базі патерну **Database-per-Service** і розділена на три незалежні контексти:
1. **Orders Context (Замовлення та клієнти):** реляційна БД `BakeryOrdersDB` (SQL Server, доступ через чистий ADO.NET та Dapper).
   - **Сутності:** `Customers`, `Orders`, `OrderItems`, `Payments`, `Products` (локальна репліка довідника товарів).
2. **Catalog Context (Каталог та рецептура):** реляційна БД `BakeryCatalogDB` (SQL Server, доступ через Entity Framework Core).
   - **Сутності:** `Countries`, `Categories`, `Products`, `ProductDetails` (1:1), `Distributors`, `Ingredients`, `ProductIngredients` (M:N).
3. **Reviews Context (Відгуки покупців):** документоорієнтована NoSQL БД `BakeryReviewsDB` (MongoDB).
   - **Колекції:** `reviews`, `product_ratings`, `moderation_logs`.

> [!IMPORTANT]
> Між базами даних **відсутні фізичні зв'язки FOREIGN KEY**. Усі міжсервісні посилання здійснюються за числовими ідентифікаторами (`Id`), що гарантує повну незалежність розгортання та автономність кожного мікросервісу.

---

## 2. Політика дублювання даних між базами (Data Duplication Policy)

| Поле / Дані | База-власник (Master) | Де дублюється (Replica) | Мета дублювання | Механізм підтримки узгодженості |
|---|---|---|---|---|
| `UnitPrice` (Ціна товару) | `BakeryCatalogDB.Products` | `BakeryOrdersDB.OrderItems` | Фіксація суми покупки: зміна ціни в каталозі не впливає на старі чеки | **Знімок (Snapshot):** копіюється в момент створення замовлення |
| `ProductName` (Назва товару) | `BakeryCatalogDB.Products` | `BakeryOrdersDB.OrderItems` | Автономність друку чеків та звітів без мережевих запитів до каталогу | **Знімок (Snapshot):** копіюється в момент покупки |
| `Products.Name, Price` | `BakeryCatalogDB.Products` | `BakeryOrdersDB.Products` | Локальна копія довідника товарів для швидкого оформлення кошика та перевірки наявності | **Подієва узгодженість (Eventual Consistency):** подія `ProductCatalogUpdatedEvent` |
| `ProductName` (Назва товару) | `BakeryCatalogDB.Products` | `BakeryReviewsDB.reviews` | Швидка видача списку відгуків клієнту без міжсервісних Join | **Подієва узгодженість (Eventual Consistency):** подія `ProductRenamedEvent` |
| `CustomerName` (Ім'я клієнта) | `BakeryOrdersDB.Customers` | `BakeryReviewsDB.reviews` | Відображення автора відгуку без звернення до бази клієнтів | **Подієва узгодженість (Eventual Consistency):** подія `CustomerProfileUpdatedEvent` |

---

## 3. Діаграми сутностей (ERD)

### 3.1. Діаграма Проєкту №1 (Orders Context — BakeryOrdersDB)
Зв'язки: 1:1 (`Orders` $\leftrightarrow$ `Payments`), 1:N (`Customers` $\rightarrow$ `Orders`), M:N (`Orders` $\leftrightarrow$ `Products` через `OrderItems` з двома FK).

```mermaid
erDiagram
    Customers ||--o{ Orders : "places (1:N)"
    Orders ||--|{ OrderItems : "contains (1:N)"
    Products ||--o{ OrderItems : "referenced (1:N)"
    Orders ||--|| Payments : "has (1:1)"

    Customers {
        int Id PK
        nvarchar FirstName
        nvarchar LastName
        nvarchar Email UK
        nvarchar PhoneNumber
        char Gender
        int Age
        datetime2 CreatedAt
        bit IsDeleted
        rowversion RowVersion
    }

    Products {
        int Id PK
        nvarchar Name
        decimal Price
        int Stock
        datetime2 CreatedAt
        bit IsDeleted
        rowversion RowVersion
    }

    Orders {
        int Id PK
        nvarchar OrderNumber UK
        int CustomerId FK
        nvarchar Status
        decimal TotalAmount
        datetime2 OrderDate
        nvarchar Notes
        datetime2 CreatedAt
        bit IsDeleted
        rowversion RowVersion
    }

    OrderItems {
        int Id PK
        int OrderId FK
        int ProductId FK
        nvarchar ProductName
        decimal UnitPrice
        int Quantity
        decimal TotalPrice
        datetime2 CreatedAt
        bit IsDeleted
    }

    Payments {
        int OrderId PK, FK
        nvarchar TransactionNumber UK
        nvarchar PaymentMethod
        decimal Amount
        datetime2 PaymentDate
        nvarchar Status
        datetime2 CreatedAt
        bit IsDeleted
        rowversion RowVersion
    }
```

### 3.2. Діаграма Проєкту №2 (Catalog Context — BakeryCatalogDB)
Зв'язки: 1:1 (`Products` $\leftrightarrow$ `ProductDetails`), 1:N (`Categories` $\rightarrow$ `Products`, `Countries` $\rightarrow$ `Distributors`), M:N (`Products` $\leftrightarrow$ `Ingredients` через `ProductIngredients` з власним полем `AmountGram`).

```mermaid
erDiagram
    Categories ||--o{ Products : "categorizes (1:N)"
    Products ||--|| ProductDetails : "details (1:1)"
    Countries ||--o{ Distributors : "origin (1:N)"
    Distributors ||--o{ Ingredients : "supplies (1:N)"
    Products ||--|{ ProductIngredients : "has (1:N)"
    Ingredients ||--|{ ProductIngredients : "used_in (1:N)"

    Categories {
        int Id PK
        nvarchar Name
        nvarchar Slug UK
        nvarchar Description
        bit IsActive
    }

    Products {
        int Id PK
        nvarchar Name
        nvarchar Sku UK
        nvarchar Description
        decimal Price
        int WeightGram
        int CategoryId FK
        bit IsAvailable
        datetime2 CreatedAt
    }

    ProductDetails {
        int ProductId PK, FK
        nvarchar StorageTemperature
        int ShelfLifeHours
        nvarchar NutritionalInfo
        datetime2 CreatedAt
    }

    Distributors {
        int Id PK
        nvarchar CompanyName
        nvarchar TaxNumber UK
        nvarchar ContactPhone
        int CountryId FK
        nvarchar Address
    }

    Countries {
        int Id PK
        nvarchar Name UK
        char IsoCode UK
    }

    Ingredients {
        int Id PK
        nvarchar Name
        int CaloriesPer100g
        int DistributorId FK
        int OriginCountryId FK
        bit IsAllergen
    }

    ProductIngredients {
        int ProductId PK, FK
        int IngredientId PK, FK
        decimal AmountGram
        int StepOrder
        datetime2 AddedAt
    }
```

---

## 4. Інструкція з розгортання баз даних

Скрипти баз даних згруповані у каталозі `db/`:
```text
db/
├── p1/    # Проєкт №1 (Orders): schema.sql · procedures.sql · seed.sql
├── p2/    # Проєкт №2 (Catalog): schema.sql · seed.sql
└── p3/    # Проєкт №3 (MongoDB): collections.js · seed.js
```

### Порядок виконання для SQL Server:
1. **Проєкт №1 (BakeryOrdersDB):**
   ```sql
   -- 1. Створення таблиць, зв'язків 1:1, 1:N, M:N (з двома FK) та індексів
   :r db/p1/schema.sql
   -- 2. Створення збережуваних процедур (транзакційна зміна статусу зі State Machine, CRUD)
   :r db/p1/procedures.sql
   -- 3. Ідемпотентне наповнення тестовими даними (IF NOT EXISTS)
   :r db/p1/seed.sql
   ```
2. **Проєкт №2 (BakeryCatalogDB):**
   ```sql
   -- 1. Створення каталогу, зв'язку 1:1 (ProductDetails) та M:N з полем AmountGram
   :r db/p2/schema.sql
   -- 2. Ідемпотентний seed товарів, деталей та інгредієнтів
   :r db/p2/seed.sql
   ```

### Порядок виконання для MongoDB:
3. **Проєкт №3 (BakeryReviewsDB):**
   ```bash
   # Ініціалізація валідації колекцій та індексів
   mongosh BakeryReviewsDB db/p3/collections.js
   # Ідемпотентне наповнення тестовими відгуками через upsert (безпечний повторний запуск)
   mongosh BakeryReviewsDB db/p3/seed.js
   ```
