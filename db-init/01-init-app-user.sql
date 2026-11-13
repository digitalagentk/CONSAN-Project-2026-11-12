-- ============================================================
-- CONSAN® — Database User Initialization
-- ============================================================

USE master;
GO

-- 1. إنشاء Login للتطبيق
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'consan_app')
BEGIN
    CREATE LOGIN consan_app WITH PASSWORD = N'$(APP_PASSWORD)', 
        CHECK_POLICY = ON,
        CHECK_EXPIRATION = OFF;
    PRINT '✅ Created login: consan_app';
END
ELSE
BEGIN
    PRINT 'ℹ️ Login consan_app already exists';
END
GO

-- 2. إنشاء قاعدة البيانات إذا ما موجودة
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ConsanDb')
BEGIN
    CREATE DATABASE ConsanDb;
    PRINT '✅ Created database: ConsanDb';
END
ELSE
BEGIN
    PRINT 'ℹ️ Database ConsanDb already exists';
END
GO

USE ConsanDb;
GO

-- 3. إنشاء User داخل قاعدة البيانات
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'consan_app')
BEGIN
    CREATE USER consan_app FOR LOGIN consan_app;
    PRINT '✅ Created user: consan_app';
END
ELSE
BEGIN
    PRINT 'ℹ️ User consan_app already exists';
END
GO

-- 4. إعطاء صلاحيات محدودة
ALTER ROLE db_datareader ADD MEMBER consan_app;
ALTER ROLE db_datawriter ADD MEMBER consan_app;
PRINT '✅ Granted read/write to consan_app';
GO

GRANT EXECUTE TO consan_app;
PRINT '✅ Granted EXECUTE to consan_app';
GO

PRINT '========================================';
PRINT '✅ Database initialization complete';
PRINT '========================================';
GO