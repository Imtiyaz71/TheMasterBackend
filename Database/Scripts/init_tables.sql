-- ====================================================================================
-- THE MASTER ENTERPRISE PLATFORM - DATABASE INITIALIZATION SCRIPT
-- Target Database: themasterdb (SQL Server 2025 / 2022 / 2019)
-- Designed to be 100% IDEMPOTENT: Safe to run multiple times without duplicating or failing.
-- ====================================================================================

USE [themasterdb];
GO

-- 1. USERS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FullName] NVARCHAR(150) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [PhoneNumber] NVARCHAR(30) NULL,
        [Role] NVARCHAR(50) NOT NULL DEFAULT 'Student',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL
    );

    CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
    PRINT '>> Table [Users] created successfully.';
END
ELSE
BEGIN
    PRINT '>> Table [Users] already exists. Skipping.';
END
GO

-- 2. COURSES TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Courses' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Courses] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] NVARCHAR(250) NOT NULL,
        [Slug] NVARCHAR(250) NOT NULL,
        [Subject] NVARCHAR(150) NOT NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        [ThumbnailUrl] NVARCHAR(MAX) NULL,
        [Price] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [DiscountPrice] DECIMAL(18,2) NULL,
        [ValidityDays] INT NOT NULL DEFAULT 365,
        [TotalVideos] INT NOT NULL DEFAULT 0,
        [DriveFolderLink] NVARCHAR(1000) NOT NULL,
        [IsPublished] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL
    );

    CREATE UNIQUE INDEX [IX_Courses_Slug] ON [dbo].[Courses]([Slug]);
    PRINT '>> Table [Courses] created successfully.';
END
ELSE
BEGIN
    PRINT '>> Table [Courses] already exists. Skipping.';
END
GO

-- 3. LESSONS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Lessons' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Lessons] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CourseId] INT NOT NULL,
        [LessonNumber] INT NOT NULL DEFAULT 1,
        [Title] NVARCHAR(250) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [DurationMinutes] INT NOT NULL DEFAULT 0,
        [DriveVideoLink] NVARCHAR(1000) NULL,
        [IsFreePreview] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Lessons_Courses] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Lessons_CourseId] ON [dbo].[Lessons]([CourseId]);
    PRINT '>> Table [Lessons] created successfully.';
END
ELSE
BEGIN
    PRINT '>> Table [Lessons] already exists. Skipping.';
END
GO

-- 4. ORDERS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderNumber] NVARCHAR(100) NOT NULL,
        [UserId] INT NOT NULL,
        [CourseId] INT NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [DriveAccessStatus] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [DriveAccessGrantedAt] DATETIME2 NULL,
        [PaymentMethod] NVARCHAR(50) NULL,
        [TransactionId] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Orders_Courses] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses]([Id])
    );

    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [dbo].[Orders]([OrderNumber]);
    CREATE INDEX [IX_Orders_UserId] ON [dbo].[Orders]([UserId]);
    CREATE INDEX [IX_Orders_CourseId] ON [dbo].[Orders]([CourseId]);
    PRINT '>> Table [Orders] created successfully.';
END
ELSE
BEGIN
    PRINT '>> Table [Orders] already exists. Skipping.';
END
GO

-- 5. PAYMENT TRANSACTIONS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[PaymentTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [TranId] NVARCHAR(150) NOT NULL,
        [ValId] NVARCHAR(150) NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [CardType] NVARCHAR(100) NULL,
        [BankTranId] NVARCHAR(100) NULL,
        [CardIssuer] NVARCHAR(100) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
        [TranDate] DATETIME2 NULL,
        [RawResponse] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_PaymentTransactions_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_PaymentTransactions_TranId] ON [dbo].[PaymentTransactions]([TranId]);
    PRINT '>> Table [PaymentTransactions] created successfully.';
END
ELSE
BEGIN
    PRINT '>> Table [PaymentTransactions] already exists. Skipping.';
END
GO

-- 6. ENROLLMENTS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Enrollments' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Enrollments] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [CourseId] INT NOT NULL,
        [OrderId] INT NOT NULL,
        [EnrolledAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ExpiresAt] DATETIME2 NULL,
        [DriveAccessGranted] BIT NOT NULL DEFAULT 0,
        [DriveAccessGrantedAt] DATETIME2 NULL,
        [Notes] NVARCHAR(500) NULL,
        CONSTRAINT [FK_Enrollments_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Enrollments_Courses] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses]([Id]),
        CONSTRAINT [FK_Enrollments_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id])
    );

    CREATE INDEX [IX_Enrollments_UserId] ON [dbo].[Enrollments]([UserId]);
    CREATE INDEX [IX_Enrollments_CourseId] ON [dbo].[Enrollments]([CourseId]);
    PRINT '>> Table [Enrollments] created successfully.';
END
ELSE
BEGIN
    PRINT '>> Table [Enrollments] already exists. Skipping.';
END
GO

-- 7. SEED INITIAL SUPER ADMIN USER (Password: Admin@123)
-- BCrypt Hash for 'Admin@123': $2a$11$w1hU3aVjWlEwS7d6k4k89eXqE4aJ5nZ7m9jK2lO5pP6qR7sT8uV0w
IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Email] = 'admin@themaster.com')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Email], [PasswordHash], [PhoneNumber], [Role], [IsActive], [CreatedAt])
    VALUES ('Super Admin', 'admin@themaster.com', '$2a$11$A/RqFhU0PNzZAnN0ZwK3oe7k89E0tuDAft1uPgstR/KJ2K2bn2Tm.', '01700000000', 'Admin', 1, SYSUTCDATETIME());
    PRINT '>> Default Super Admin created: admin@themaster.com';
END
GO

PRINT '=======================================================';
PRINT '>> Database schema verification & setup completed successfully.';
PRINT '=======================================================';
