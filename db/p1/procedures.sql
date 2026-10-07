-- ============================================================================
-- ПРОЄКТ №1: Збережувані процедури для BakeryOrdersDB
-- Стек: SQL Server (T-SQL)
-- ============================================================================

-- 1. Процедура транзакційної зміни статусу з валідацією життєвого циклу
CREATE OR ALTER PROCEDURE dbo.sp_UpdateOrderStatus
    @OrderId INT,
    @NewStatus NVARCHAR(20),
    @UpdatedBy NVARCHAR(50) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Перевірка існування замовлення
        DECLARE @CurrentStatus NVARCHAR(20);
        SELECT @CurrentStatus = Status 
        FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            THROW 50004, N'Замовлення не знайдено або воно видалене.', 1;
        END

        -- 2. Валідація бізнес-правил переходів станів
        -- Заборона повторного переведення в той самий статус
        IF @CurrentStatus = @NewStatus
        BEGIN
            THROW 50002, N'Замовлення вже має вказаний статус.', 1;
        END

        -- Не можна змінювати статус завершеного або скасованого замовлення
        IF @CurrentStatus IN ('Delivered', 'Cancelled')
        BEGIN
            THROW 50001, N'Неможливо змінити статус замовлення, яке вже доставлено або скасовано.', 1;
        END

        -- Валідація допустимих переходів скінченного автомата (State Machine)
        -- Перехід у 'Baking', 'Ready', 'Delivered' для неоплаченого замовлення заборонено
        DECLARE @IsValidTransition BIT = 0;

        IF @CurrentStatus = 'Pending' AND @NewStatus IN ('Paid', 'Cancelled') SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = 'Paid' AND @NewStatus IN ('Baking', 'Cancelled') SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = 'Baking' AND @NewStatus = 'Ready' SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = 'Ready' AND @NewStatus = 'Delivered' SET @IsValidTransition = 1;

        IF @IsValidTransition = 0
        BEGIN
            THROW 50003, N'Неприпустимий перехід статусу замовлення за бізнес-правилами життєвого циклу.', 1;
        END

        -- 3. Оновлення статусу
        UPDATE dbo.Orders
        SET Status = @NewStatus,
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @UpdatedBy
        WHERE Id = @OrderId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 2. Процедура створення замовлення з товарними позиціями в транзакції
CREATE OR ALTER PROCEDURE dbo.sp_CreateOrderWithItems
    @OrderNumber NVARCHAR(36),
    @CustomerId INT,
    @Notes NVARCHAR(250) = NULL,
    @CreatedBy NVARCHAR(50) = 'Customer',
    @NewOrderId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Перевірка активності клієнта
        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0)
        BEGIN
            THROW 50005, N'Вказаного клієнта не існує або він деактивований.', 1;
        END

        -- Створення замовлення
        INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, OrderDate, Notes, CreatedBy, UpdatedBy)
        VALUES (@OrderNumber, @CustomerId, 'Pending', 0.00, SYSUTCDATETIME(), @Notes, @CreatedBy, @CreatedBy);

        SET @NewOrderId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 3. Процедура реєстрації 1:1 оплати замовлення
CREATE OR ALTER PROCEDURE dbo.sp_ProcessPayment
    @OrderId INT,
    @TransactionNumber NVARCHAR(50),
    @PaymentMethod NVARCHAR(30),
    @Amount DECIMAL(18, 2),
    @ProcessedBy NVARCHAR(50) = 'PaymentService'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Перевірка існування та суми замовлення
        DECLARE @OrderTotal DECIMAL(18, 2);
        DECLARE @OrderStatus NVARCHAR(20);

        SELECT @OrderTotal = TotalAmount, @OrderStatus = Status
        FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId AND IsDeleted = 0;

        IF @OrderTotal IS NULL
        BEGIN
            THROW 50006, N'Замовлення для оплати не знайдено.', 1;
        END

        IF @OrderStatus = 'Paid'
        BEGIN
            THROW 50007, N'Замовлення вже оплачено раніше.', 1;
        END

        -- Створення запису оплати (1:1 відношення)
        INSERT INTO dbo.Payments (OrderId, TransactionNumber, PaymentMethod, Amount, PaymentDate, Status, CreatedBy, UpdatedBy)
        VALUES (@OrderId, @TransactionNumber, @PaymentMethod, @Amount, SYSUTCDATETIME(), 'Completed', @ProcessedBy, @ProcessedBy);

        -- Автоматичне переведення статусу замовлення в 'Paid'
        UPDATE dbo.Orders
        SET Status = 'Paid',
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @ProcessedBy
        WHERE Id = @OrderId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 4. Процедура вибірки історії замовлень клієнта з пагінацією
CREATE OR ALTER PROCEDURE dbo.sp_GetCustomerOrderHistory
    @CustomerId INT,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        o.Id,
        o.OrderNumber,
        o.Status,
        o.TotalAmount,
        o.OrderDate,
        o.Notes,
        COUNT(i.Id) AS ItemsCount,
        p.TransactionNumber,
        p.PaymentMethod
    FROM dbo.Orders o
    LEFT JOIN dbo.OrderItems i ON o.Id = i.OrderId AND i.IsDeleted = 0
    LEFT JOIN dbo.Payments p ON o.Id = p.OrderId AND p.IsDeleted = 0
    WHERE o.CustomerId = @CustomerId AND o.IsDeleted = 0
    GROUP BY o.Id, o.OrderNumber, o.Status, o.TotalAmount, o.OrderDate, o.Notes, p.TransactionNumber, p.PaymentMethod
    ORDER BY o.OrderDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- 5. Процедура формування щоденного звіту продажів пекарні
CREATE OR ALTER PROCEDURE dbo.sp_GetDailySalesReport
    @ReportDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CAST(o.OrderDate AS DATE) AS SalesDate,
        COUNT(o.Id) AS TotalOrders,
        SUM(CASE WHEN o.Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledOrders,
        SUM(CASE WHEN o.Status != 'Cancelled' THEN o.TotalAmount ELSE 0.00 END) AS GrossRevenue,
        AVG(CASE WHEN o.Status != 'Cancelled' THEN o.TotalAmount ELSE NULL END) AS AverageCheck
    FROM dbo.Orders o
    WHERE CAST(o.OrderDate AS DATE) = @ReportDate AND o.IsDeleted = 0
    GROUP BY CAST(o.OrderDate AS DATE);
END;
GO
