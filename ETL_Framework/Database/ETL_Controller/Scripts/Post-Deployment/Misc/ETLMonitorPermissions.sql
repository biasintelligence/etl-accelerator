--Create ETL Monitor role. Add users to this role to enable them to run the ETL_Monitor
--Set their default schema to [ETLMonitor]

IF  NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'ETLMonitor' AND type = 'R')
   CREATE ROLE [ETLMonitor] AUTHORIZATION [dbo]
GO
IF  NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'ETLMonitor')
   exec ('CREATE SCHEMA [ETLMonitor] AUTHORIZATION [ETLMonitor]');
GO
--Database level permissions
GRANT CREATE PROCEDURE TO [ETLMonitor];
GRANT SELECT TO [ETLMonitor];
GRANT EXECUTE ON OBJECT::dbo.prc_ETLCounterSet TO [ETLMonitor]; 
GRANT CREATE QUEUE TO [ETLMonitor];
GRANT CREATE SERVICE TO [ETLMonitor];
GRANT SUBSCRIBE QUERY NOTIFICATIONS TO [ETLMonitor];
GRANT VIEW DEFINITION TO [ETLMonitor];
 --Service Broker permissions
GRANT REFERENCES ON CONTRACT::[http://schemas.microsoft.com/SQL/Notifications/PostQueryNotification] TO [ETLMonitor];
GRANT RECEIVE ON dbo.QueryNotificationErrorsQueue TO [ETLMonitor];
