IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fct_Orders]') AND type in (N'U'))
DROP TABLE [dbo].[fct_Orders];
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[dimMember]') AND type in (N'U'))
DROP TABLE [dbo].[dimMember];
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MemberTotalOrderAmount]') AND type in (N'U'))
DROP TABLE [dbo].[MemberTotalOrderAmount];
