-- ============================================================================
-- ПРОЄКТ №2: База даних каталогу та рецептури пекарні (BakeryCatalogDB)
-- Стек: SQL Server (T-SQL) для Entity Framework Core
-- ============================================================================

IF OBJECT_ID('dbo.ProductIngredients', 'U') IS NOT NULL DROP TABLE dbo.ProductIngredients;
IF OBJECT_ID('dbo.Ingredients', 'U') IS NOT NULL DROP TABLE dbo.Ingredients;
IF OBJECT_ID('dbo.Distributors', 'U') IS NOT NULL DROP TABLE dbo.Distributors;
IF OBJECT_ID('dbo.ProductDetails', 'U') IS NOT NULL DROP TABLE dbo.ProductDetails;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Countries', 'U') IS NOT NULL DROP TABLE dbo.Countries;

-- 1. Довідник країн (Countries)
CREATE TABLE dbo.Countries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsoCode CHAR(2) NOT NULL,
    CONSTRAINT UQ_Countries_Name UNIQUE (Name),
    CONSTRAINT UQ_Countries_IsoCode UNIQUE (IsoCode)
);

-- 2. Категорії товарів (Categories)
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    Description NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Categories_Slug UNIQUE (Slug)
);

-- 3. Товари пекарні (Products)
-- Зв'язок 1:N з Categories
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Sku NVARCHAR(50) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18, 2) NOT NULL,
    WeightGram INT NOT NULL,
    CategoryId INT NOT NULL,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT UQ_Products_Sku UNIQUE (Sku),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) 
        REFERENCES dbo.Categories(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Products_Price CHECK (Price > 0.00),
    CONSTRAINT CHK_Products_Weight CHECK (WeightGram > 0)
);

-- 4. Деталі товару та умови зберігання (ProductDetails)
-- Зв'язок 1:1 з Products (FK ProductId є водночас PRIMARY KEY таблиці)
CREATE TABLE dbo.ProductDetails (
    ProductId INT PRIMARY KEY,
    StorageTemperature NVARCHAR(50) NOT NULL DEFAULT '+15..+22 C',
    ShelfLifeHours INT NOT NULL DEFAULT 48,
    NutritionalInfo NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_ProductDetails_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_ProductDetails_ShelfLife CHECK (ShelfLifeHours > 0)
);

-- 4. Постачальники інгредієнтів (Distributors)
-- Зв'язок 1:N з Countries
CREATE TABLE dbo.Distributors (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName NVARCHAR(150) NOT NULL,
    TaxNumber NVARCHAR(30) NOT NULL,
    ContactPhone NVARCHAR(20) NOT NULL,
    CountryId INT NOT NULL,
    Address NVARCHAR(200) NOT NULL,
    
    CONSTRAINT UQ_Distributors_TaxNumber UNIQUE (TaxNumber),
    CONSTRAINT FK_Distributors_Countries FOREIGN KEY (CountryId) 
        REFERENCES dbo.Countries(Id) ON DELETE NO ACTION
);

-- 5. Інгредієнти та сировина (Ingredients)
-- Зв'язок 1:N з Distributors та Countries
CREATE TABLE dbo.Ingredients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CaloriesPer100g INT NOT NULL,
    DistributorId INT NOT NULL,
    OriginCountryId INT NOT NULL,
    IsAllergen BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_Ingredients_Distributors FOREIGN KEY (DistributorId) 
        REFERENCES dbo.Distributors(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Ingredients_Countries FOREIGN KEY (OriginCountryId) 
        REFERENCES dbo.Countries(Id) ON DELETE NO ACTION,
    CONSTRAINT CHK_Ingredients_Calories CHECK (CaloriesPer100g >= 0)
);

-- 6. Проміжна таблиця рецептури (ProductIngredients)
-- Зв'язок M:N між Products та Ingredients з власними полями (AmountGram, AddedAt, StepOrder)
CREATE TABLE dbo.ProductIngredients (
    ProductId INT NOT NULL,
    IngredientId INT NOT NULL,
    AmountGram DECIMAL(10, 2) NOT NULL DEFAULT 100.0,
    StepOrder INT NOT NULL DEFAULT 1,
    AddedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_ProductIngredients PRIMARY KEY (ProductId, IngredientId),
    CONSTRAINT FK_ProductIngredients_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductIngredients_Ingredients FOREIGN KEY (IngredientId) 
        REFERENCES dbo.Ingredients(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_ProductIngredients_Amount CHECK (AmountGram > 0.0)
);

-- Індекси для оптимізації фільтрації меню
CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON dbo.Products(CategoryId) INCLUDE (Price, Name);
CREATE NONCLUSTERED INDEX IX_Products_Price ON dbo.Products(Price ASC);
CREATE NONCLUSTERED INDEX IX_Ingredients_IsAllergen ON dbo.Ingredients(IsAllergen);
