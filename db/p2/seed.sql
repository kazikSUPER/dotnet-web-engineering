-- ============================================================================
-- ПРОЄКТ №2: Ідемпотентне початкове наповнення Каталогу пекарні
-- Стек: SQL Server (T-SQL)
-- ============================================================================

SET NOCOUNT ON;

-- 1. Країни
IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE IsoCode = 'UA')
    INSERT INTO dbo.Countries (Name, IsoCode) VALUES (N'Україна', 'UA');
IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE IsoCode = 'FR')
    INSERT INTO dbo.Countries (Name, IsoCode) VALUES (N'Франція', 'FR');
IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE IsoCode = 'IT')
    INSERT INTO dbo.Countries (Name, IsoCode) VALUES (N'Італія', 'IT');

-- 2. Категорії
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'bread')
    INSERT INTO dbo.Categories (Name, Slug, Description) VALUES (N'Хліб ремісничий', 'bread', N'Хліб на живій заквасці тривалої ферментації');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'croissants')
    INSERT INTO dbo.Categories (Name, Slug, Description) VALUES (N'Круасани та листкова випічка', 'croissants', N'Традиційна французька листкова випічка');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'desserts')
    INSERT INTO dbo.Categories (Name, Slug, Description) VALUES (N'Десерти та чізкейки', 'desserts', N'Крафтові баскські чізкейки та тарти');

-- 3. Постачальники
DECLARE @UaId INT = (SELECT Id FROM dbo.Countries WHERE IsoCode = 'UA');
DECLARE @FrId INT = (SELECT Id FROM dbo.Countries WHERE IsoCode = 'FR');

IF @UaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Distributors WHERE TaxNumber = 'UA-38291044')
    INSERT INTO dbo.Distributors (CompanyName, TaxNumber, ContactPhone, CountryId, Address)
    VALUES (N'Млинопром Поділля', 'UA-38291044', '+380382554433', @UaId, N'м. Хмельницький, вул. Зарічанська 12');

IF @FrId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Distributors WHERE TaxNumber = 'FR-99482103')
    INSERT INTO dbo.Distributors (CompanyName, TaxNumber, ContactPhone, CountryId, Address)
    VALUES (N'Les Grands Moulins de Paris', 'FR-99482103', '+33140506070', @FrId, N'Paris, Quai de la Gare 71');

-- 4. Товари
DECLARE @CatBread INT = (SELECT Id FROM dbo.Categories WHERE Slug = 'bread');
DECLARE @CatCroiss INT = (SELECT Id FROM dbo.Categories WHERE Slug = 'croissants');
DECLARE @CatDessert INT = (SELECT Id FROM dbo.Categories WHERE Slug = 'desserts');

IF @CatCroiss IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = 'CR-001')
    INSERT INTO dbo.Products (Name, Sku, Description, Price, WeightGram, CategoryId)
    VALUES (N'Круасан класичний масляний', 'CR-001', N'Круасан на вершковому маслі 82.5% жирності', 60.00, 85, @CatCroiss);

IF @CatBread IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = 'BR-002')
    INSERT INTO dbo.Products (Name, Sku, Description, Price, WeightGram, CategoryId)
    VALUES (N'Багет традиційний французький', 'BR-002', N'Хрусткий багет з пористою текстурою', 40.00, 250, @CatBread);

IF @CatDessert IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = 'DS-003')
    INSERT INTO dbo.Products (Name, Sku, Description, Price, WeightGram, CategoryId)
    VALUES (N'Чізкейк Сан-Себастьян', 'DS-003', N'Карамелізований баскський запечений чізкейк', 160.00, 180, @CatDessert);

-- 5. Інгредієнти
DECLARE @DistrUa INT = (SELECT Id FROM dbo.Distributors WHERE TaxNumber = 'UA-38291044');
DECLARE @DistrFr INT = (SELECT Id FROM dbo.Distributors WHERE TaxNumber = 'FR-99482103');

IF @DistrUa IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ingredients WHERE Name = N'Борошно пшеничне цільнозернове')
    INSERT INTO dbo.Ingredients (Name, CaloriesPer100g, DistributorId, OriginCountryId, IsAllergen)
    VALUES (N'Борошно пшеничне цільнозернове', 340, @DistrUa, @UaId, 1);

IF @DistrFr IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ingredients WHERE Name = N'Масло солодковершкове Extra 82.5%')
    INSERT INTO dbo.Ingredients (Name, CaloriesPer100g, DistributorId, OriginCountryId, IsAllergen)
    VALUES (N'Масло солодковершкове Extra 82.5%', 748, @DistrFr, @FrId, 1);

-- 6. Рецептура (ProductIngredients з власним полем AmountGram)
DECLARE @ProdCroiss INT = (SELECT Id FROM dbo.Products WHERE Sku = 'CR-001');
DECLARE @IngFlour INT = (SELECT Id FROM dbo.Ingredients WHERE Name = N'Борошно пшеничне цільнозернове');
DECLARE @IngButter INT = (SELECT Id FROM dbo.Ingredients WHERE Name = N'Масло солодковершкове Extra 82.5%');

IF @ProdCroiss IS NOT NULL AND @IngFlour IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM dbo.ProductIngredients WHERE ProductId = @ProdCroiss AND IngredientId = @IngFlour)
BEGIN
    INSERT INTO dbo.ProductIngredients (ProductId, IngredientId, AmountGram, StepOrder)
    VALUES (@ProdCroiss, @IngFlour, 55.00, 1);
END

IF @ProdCroiss IS NOT NULL AND @IngButter IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM dbo.ProductIngredients WHERE ProductId = @ProdCroiss AND IngredientId = @IngButter)
BEGIN
    INSERT INTO dbo.ProductIngredients (ProductId, IngredientId, AmountGram, StepOrder)
    VALUES (@ProdCroiss, @IngButter, 35.00, 2);
END
