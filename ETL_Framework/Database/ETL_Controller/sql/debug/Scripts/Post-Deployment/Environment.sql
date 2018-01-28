--Environment script
--select * from dbo.systemparameters
--select dbo.fn_systemparameter('Environment','BuildLocation','DEV')
begin try
-------------------------------------------------------------------
-- CURRENT ENV
-------------------------------------------------------------------
exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'Current'
	,@ParameterValue = 'DEV'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Current Environment'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'ALL';

-------------------------------------------------------------------
-- DEV
-------------------------------------------------------------------
exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'BuildLocation'
	,@ParameterValue = 'c:\Builds'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Build location'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'DW'
	,@ParameterValue = 'DW'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Datawarehouse'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'Staging'
	,@ParameterValue = 'ETL_Staging'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Staging DB'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'DW_Server'
	,@ParameterValue = '.'
	,@ParameterDefault = ''
	,@ParameterDesc = 'DW Server'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';


--EventServer 
exec prc_SystemParameterLet 
	@ParameterType = 'Environment',
	@ParameterName = 'EventServer',
	@ParameterValue = '.',
	@ParameterDesc = 'Event Server ',
	@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';


--EventDB 
exec prc_SystemParameterLet 
	@ParameterType = 'Environment',
	@ParameterName = 'EventDB',
	@ParameterValue = 'ETL_Event', 
	@ParameterDesc = 'Event database',
	@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

-------------------------------------------------------------------
-- TEST
-------------------------------------------------------------------
-------------------------------------------------------------------
-- UAT
-------------------------------------------------------------------
-------------------------------------------------------------------
-- PROD
-------------------------------------------------------------------

end try
begin catch
   declare @msg nvarchar(1000);
   set @msg = error_message();
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg);
end catch
