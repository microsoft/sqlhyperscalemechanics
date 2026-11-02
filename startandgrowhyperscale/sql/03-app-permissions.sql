-- Run separately in each scheduling database as the Entra administrator.
-- SQLCMD variable AppLoginSid comes from the app login's sys.server_principals.sid in master.
IF DB_NAME() NOT IN (N'caldova-scheduling', N'northbrook-scheduling', N'ridgeview-scheduling')
    THROW 50000, 'Connect to one of the three Caldova scheduling databases.', 1;

DECLARE @AppLoginSid varbinary(85) = $(AppLoginSid);
IF @AppLoginSid IS NULL OR DATALENGTH(@AppLoginSid) <> 16
    THROW 50001, 'Supply the expected 16-byte Entra app login SID from master.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app-caldova-network-0efc44')
    CREATE USER [app-caldova-network-0efc44] FROM LOGIN [app-caldova-network-0efc44];

IF EXISTS (
    SELECT 1 FROM sys.database_principals
    WHERE name = N'app-caldova-network-0efc44'
      AND (type NOT IN ('E', 'X') OR sid <> @AppLoginSid + 0xAADE)
)
    THROW 50002, 'Existing user is not mapped to the expected login; inspect the mapping before continuing.', 1;

GRANT SELECT, UPDATE ON OBJECT::dbo.AppointmentSlot TO [app-caldova-network-0efc44];
GRANT INSERT ON OBJECT::dbo.Appointment TO [app-caldova-network-0efc44];
IF OBJECT_ID(N'demo.StorageSize', N'V') IS NOT NULL
    GRANT SELECT ON OBJECT::demo.StorageSize TO [app-caldova-network-0efc44];
IF OBJECT_ID(N'demo.FillStorageBatch', N'P') IS NOT NULL
    GRANT EXECUTE ON OBJECT::demo.FillStorageBatch TO [app-caldova-network-0efc44];
IF DB_NAME() = N'caldova-scheduling'
BEGIN
    EXEC(N'GRANT SELECT ON OBJECT::network.ProviderClinic TO [app-caldova-network-0efc44];');
    EXEC(N'GRANT SELECT ON OBJECT::network.AppointmentAvailability TO [app-caldova-network-0efc44];');
END;
GO
