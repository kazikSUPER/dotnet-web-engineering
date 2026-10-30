IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(50) NOT NULL,
    [LastName] nvarchar(50) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Gender] nvarchar(1) NOT NULL,
    [Age] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Customers_Age] CHECK ([Age] >= 14 AND [Age] <= 120)
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Stock] int NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Products_Price] CHECK ([Price] > 0),
    CONSTRAINT [CK_Products_Stock] CHECK ([Stock] >= 0)
);

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderNumber] nvarchar(36) NOT NULL,
    [CustomerId] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [Notes] nvarchar(250) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN ('Pending','Paid','Baking','Ready','Delivered','Cancelled')),
    CONSTRAINT [CK_Orders_TotalAmount] CHECK ([TotalAmount] >= 0),
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProductCategories] (
    [ProductId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([ProductId], [CategoryId]),
    CONSTRAINT [FK_ProductCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductCategories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProductDetails] (
    [ProductId] int NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Manufacturer] nvarchar(150) NULL,
    CONSTRAINT [PK_ProductDetails] PRIMARY KEY ([ProductId]),
    CONSTRAINT [FK_ProductDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProductImages] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [Url] nvarchar(500) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(100) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] > 0),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderStatusHistories] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ChangedAt] datetime2 NOT NULL,
    [Comment] nvarchar(250) NULL,
    CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Payments] (
    [OrderId] int NOT NULL,
    [TransactionNumber] nvarchar(50) NOT NULL,
    [PaymentMethod] nvarchar(30) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([OrderId]),
    CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] > 0),
    CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [Name])
VALUES (1, N'Хліб'),
(2, N'Випічка'),
(3, N'Десерти');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] OFF;

CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);

CREATE UNIQUE INDEX [IX_Customers_Email] ON [Customers] ([Email]);

CREATE INDEX [IX_Customers_PhoneNumber] ON [Customers] ([PhoneNumber]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);

CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);

CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);

CREATE INDEX [IX_OrderStatusHistories_OrderId_ChangedAt] ON [OrderStatusHistories] ([OrderId], [ChangedAt]);

CREATE UNIQUE INDEX [IX_Payments_TransactionNumber] ON [Payments] ([TransactionNumber]);

CREATE INDEX [IX_ProductCategories_CategoryId] ON [ProductCategories] ([CategoryId]);

CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [ProductImages] ([ProductId], [SortOrder]);

CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261025194200_InitialCreate', N'10.0.0');

COMMIT;
GO

