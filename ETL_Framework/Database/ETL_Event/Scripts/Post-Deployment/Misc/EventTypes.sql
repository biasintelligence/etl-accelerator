insert EventType
select NEWID(),'Process1_FINISHED','EventArgs.XSD','RROD',100,getdate(), getdate()
union
select NEWID(),'Process2_FINISHED','EventArgs.XSD','RROD',100,getdate(), getdate()