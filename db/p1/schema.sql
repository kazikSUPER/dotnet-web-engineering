-- ============================================================================
-- ПРОЄКТ №1: База даних замовлень пекарні (BakeryOrdersDB)
-- Стек: SQL Server (T-SQL) для ADO.NET та Dapper
-- ============================================================================

-- Очищення перед створенням (ідемпотентність для DDL)
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;

-- 1. Таблиця клієнтів (Customers)
CREATE TABLE dbo.Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Gender CHAR(1) NOT NULL,
    Age INT NOT NULL,
    
    -- Аудитні колонки
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    -- Оптимістична конкурентність
    RowVersion ROWVERSION NOT NULL,

    -- Обмеження цілісності (CHECK та UNIQUE)
    CONSTRAINT UQ_Customers_Email UNIQUE (Email),
    CONSTRAINT CHK_Customers_Gender CHECK (Gender IN ('M', 'F', 'O')),
    CONSTRAINT CHK_Customers_Age CHECK (Age >= 0 AND Age <= 120)
);

-- 2. Локальна таблиця товарів пекарні (Products)
-- Виступає локальною реплікою/довідником Каталогу для сервісу замовлень (eShop pattern)
CREATE TABLE dbo.Products (
    Id INT PRIMARY KEY,                    -- Той самий Id, що й у Каталозі (без крос-серверного FK)
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    Stock INT NOT NULL DEFAULT 0,
    
    -- Аудитні колонки
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT CHK_Products_Price CHECK (Price > 0.00),
    CONSTRAINT CHK_Products_Stock CHECK (Stock >= 0)
);

-- 3. Таблиця замовлень (Orders)
-- Зв'язок 1:N з Customers (FK на боці багатьох)
CREATE TABLE dbo.Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(36) NOT NULL,
    CustomerId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    TotalAmount DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    OrderDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Notes NVARCHAR(250) NULL,

    -- Аудитні колонки
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'Customer',
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    IsDeleted BIT NOT NULL DEFAULT 0,

    -- Оптимістична конкурентність
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) 
        REFERENCES dbo.Customers(Id) ON DELETE NO ACTION,
    CONSTRAINT CHK_Orders_Status CHECK (Status IN ('Pending', 'Paid', 'Baking', 'Ready', 'Delivered', 'Cancelled')),
    CONSTRAINT CHK_Orders_TotalAmount CHECK (TotalAmount >= 0.00)
);

-- 4. Таблиця позицій замовлення (OrderItems)
-- Зв'язок M:N між Orders та Products (проміжна таблиця з двома зовнішніми ключами)
CREATE TABLE dbo.OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,                -- Зовнішній ключ на локальну таблицю Products
    ProductName NVARCHAR(100) NOT NULL,    -- Знімок назви товару на момент покупки
    UnitPrice DECIMAL(18, 2) NOT NULL,     -- Знімок ціни товару на момент покупки
    Quantity INT NOT NULL DEFAULT 1,
    TotalPrice AS (Quantity * UnitPrice) PERSISTED,

    -- Аудитні колонки
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
    IsDeleted BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) 
        REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products(Id) ON DELETE NO ACTION,
    CONSTRAINT CHK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CHK_OrderItems_UnitPrice CHECK (UnitPrice > 0.00)
);

-- 4. Таблиця оплат (Payments)
-- Зв'язок 1:1 з Orders (FK OrderId є водночас PRIMARY KEY таблиці Payments)
CREATE TABLE dbo.Payments (
    OrderId INT NOT NULL,                  -- Водночас PRIMARY KEY і FOREIGN KEY
    TransactionNumber NVARCHAR(50) NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL DEFAULT 'CreditCard',
    Amount DECIMAL(18, 2) NOT NULL,
    PaymentDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Completed',

    -- Аудитні колонки
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'PaymentGateway',
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(50) NOT NULL DEFAULT 'PaymentGateway',
    IsDeleted BIT NOT NULL DEFAULT 0,

    -- Оптимістична конкурентність
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Payments PRIMARY KEY (OrderId),
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) 
        REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Payments_TransactionNumber UNIQUE (TransactionNumber),
    CONSTRAINT CHK_Payments_Amount CHECK (Amount > 0.00),
    CONSTRAINT CHK_Payments_Status CHECK (Status IN ('Pending', 'Completed', 'Failed', 'Refunded'))
);

-- ============================================================================
-- ІНДЕКСИ З ПОЯСНЕННЯМ ПРИЗНАЧЕННЯ ДЛЯ КОНКРЕТНИХ ПОШУКОВИХ ЗАПИТІВ
-- ============================================================================

-- Цей індекс оптимізує пошук клієнта за номером телефону при оформленні швидкого замовлення
CREATE NONCLUSTERED INDEX IX_Customers_PhoneNumber 
ON dbo.Customers (PhoneNumber) 
WHERE IsDeleted = 0;

-- Цей індекс оптимізує фільтрацію замовлень конкретного клієнта з сортуванням за датою
-- Запит: SELECT * FROM Orders WHERE CustomerId = @CustomerId AND IsDeleted = 0 ORDER BY OrderDate DESC
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_OrderDate 
ON dbo.Orders (CustomerId, OrderDate DESC) 
INCLUDE (Status, TotalAmount)
WHERE IsDeleted = 0;

-- Цей індекс оптимізує чергу замовлень для кухні та диспетчера за статусом
-- Запит: SELECT * FROM Orders WHERE Status IN ('Pending', 'Paid', 'Baking') AND IsDeleted = 0
CREATE NONCLUSTERED INDEX IX_Orders_Status 
ON dbo.Orders (Status) 
INCLUDE (OrderNumber, CustomerId, OrderDate)
WHERE IsDeleted = 0;

-- Цей індекс оптимізує вибірку всіх товарних позицій для конкретного замовлення
-- Запит: SELECT * FROM OrderItems WHERE OrderId = @OrderId AND IsDeleted = 0
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId 
ON dbo.OrderItems (OrderId) 
INCLUDE (ProductId, ProductName, Quantity, UnitPrice, TotalPrice)
WHERE IsDeleted = 0;

-- Цей індекс оптимізує перевірку статусу транзакції платіжного шлюзу
-- Запит: SELECT * FROM Payments WHERE TransactionNumber = @TransactionNumber
CREATE NONCLUSTERED INDEX IX_Payments_TransactionNumber 
ON dbo.Payments (TransactionNumber) 
INCLUDE (Amount, Status, PaymentDate);
