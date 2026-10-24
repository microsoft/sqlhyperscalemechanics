-- Connect directly to master as the Microsoft Entra administrator.
-- This is the App Service system-assigned managed identity's display name.
IF DB_NAME() <> N'master'
    THROW 50000, 'Connect directly to master before creating the app login.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'app-caldova-network-0efc44')
    CREATE LOGIN [app-caldova-network-0efc44] FROM EXTERNAL PROVIDER;
GO
