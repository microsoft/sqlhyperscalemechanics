-- Run separately in each scheduling database as the Entra administrator.
IF DB_NAME() NOT IN (N'caldova-scheduling', N'northbrook-scheduling', N'ridgeview-scheduling')
    THROW 50000, 'Connect to one of the three Caldova scheduling databases.', 1;

IF SUSER_SID(N'app-caldova-network-0efc44') IS NULL
    THROW 50001, 'Create the Entra app login in master first.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app-caldova-network-0efc44')
    CREATE USER [app-caldova-network-0efc44] FROM LOGIN [app-caldova-network-0efc44];

IF EXISTS (
    SELECT 1 FROM sys.database_principals
    WHERE name = N'app-caldova-network-0efc44'
      AND sid <> SUSER_SID(N'app-caldova-network-0efc44')
)
    THROW 50002, 'Existing user is not mapped to the expected login; inspect the mapping before continuing.', 1;

GRANT SELECT, UPDATE ON OBJECT::dbo.AppointmentSlot TO [app-caldova-network-0efc44];
IF DB_NAME() = N'caldova-scheduling'
BEGIN
    EXEC(N'GRANT SELECT ON OBJECT::network.ProviderClinic TO [app-caldova-network-0efc44];');
    EXEC(N'GRANT SELECT ON OBJECT::network.AppointmentAvailability TO [app-caldova-network-0efc44];');
END;
GO
