IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fct_Orders]') AND type in (N'U'))
DROP TABLE [dbo].[fct_Orders];
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[dimMember]') AND type in (N'U'))
DROP TABLE [dbo].[dimMember];
CREATE TABLE dbo.dimMember(
	MemberID int IDENTITY(1,1) NOT NULL,
	MemberName varchar(30) NOT NULL,
PRIMARY KEY CLUSTERED (MemberID ASC));
CREATE TABLE fct_Orders
 (
	 Order_ID int IDENTITY(1,1) NOT NULL,
	 MemberId int,
	 Amount float NOT NULL,
	 FOREIGN KEY (MemberId) REFERENCES dimMember(MemberID)
  );
insert dimMember
select 'Bob'
union all
select 'Sam';
insert fct_Orders
select 1,10
union all
select 1,5
union all
select 2,20
union all
select 2,10;
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MemberTotalOrderAmount]') AND type in (N'U'))
DROP TABLE [dbo].[MemberTotalOrderAmount];
CREATE TABLE dbo.MemberTotalOrderAmount 
  ( 
     "[Dim Member].[Member Name].[Member Name].[MEMBER_CAPTION]" NVARCHAR(255) NULL 
	 ,"[Measures].[Amount]"	MONEY NULL 
  );
