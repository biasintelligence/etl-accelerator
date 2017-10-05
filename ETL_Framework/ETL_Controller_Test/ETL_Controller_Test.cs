using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.AnalysisServices.AdomdClient;

namespace ETL_Controller_Test
{
    [TestClass]
    public class PrcExecuteTest
    {
        private TestContext testContextInstance;
        private static string s_cs;

        private static string TestFilesDirPath = @"C:\Builds\TestFiles\";

        [TestInitialize()]
        public void PrcExecuteTestInitialize()
        {
            s_cs = ConfigurationManager.ConnectionStrings["ETLControllerDB"].ConnectionString;

        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }


        [TestMethod, Timeout(500000)]
        public void Loop()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'Loop';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);
        }



        [TestMethod, Timeout(500000)]
        public void Call_SP()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'Call_SP';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);
        }


        [TestMethod, Timeout(500000)]
        public void Call_Powershell()
        {
            string cs = s_cs;
            int ret = -1;

            //string PowershellScriptFile = TestFilesDirPath + "CreateFile.ps1";
            ////System.IO.File.Delete(PowershellScriptFile);

            //string contents = "param ([string]$FilePath ) New-Item $FilePath -type file -force";
            //System.IO.File.WriteAllText(PowershellScriptFile, contents);

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'Call_Powershell','debug';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                    //cmd.CommandTimeout = 500000;
                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);

            //System.IO.File.Delete(PowershellScriptFile);

        }


        [TestMethod, Timeout(500000)]
        public void FileCheck_Constraint_Met()
        {
            string cs = s_cs;
            int ret = -1;

            string FileCheckTestFile = TestFilesDirPath + "FileCheckTest.txt";
            string contents = "FileCheckTest";
            System.IO.File.WriteAllText(FileCheckTestFile, contents);

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'FileCheck','debug';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);

            System.IO.File.Delete(FileCheckTestFile);

        }


        [TestMethod, Timeout(500000)]
        public void FileCheck_Constraint_Not_Met()
        {
            string cs = s_cs;
            int ret = -1;
            int PrevRunId = 0;
            int LaterRunId = 0;
            string sqlString = "";

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();

                try
                {

                    string FileCheckTestFile = TestFilesDirPath + "FileCheckTest.txt";
                    System.IO.File.Delete(FileCheckTestFile);

                    sqlString = @"
                        select IsNull(max(RunId),0)
                        from ETLStepRunHistoryLog l with (nolock)
                        join ETLBatch b	with (nolock)
                        on l.BatchID = b.BatchID
                        and b.BatchName = 'FileCheck';";
                    SqlCommand cmd = new SqlCommand(sqlString, connection);
                    PrevRunId = (int)cmd.ExecuteScalar();

                    sqlString = @"exec dbo.prc_Execute 'FileCheck','debug';";
                    cmd.CommandText = sqlString;
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                    cmd.CommandTimeout = 500000;
                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
                catch
                {
                    sqlString = @"
                        select IsNull(max(RunId),0)
                        from ETLStepRunHistoryLog l with (nolock)
                        join ETLBatch b	with (nolock)
                        on l.BatchID = b.BatchID
                        and b.BatchName = 'FileCheck'
                        and l.LogMessage like '%ERROR Constraint=1 was not met'
                        and l.Err = 50107
                        and RunID > " + Convert.ToString(PrevRunId) + ";";
                    SqlCommand sql_cmd = new SqlCommand(sqlString, connection);
                    LaterRunId = (int)sql_cmd.ExecuteScalar();

                    if (LaterRunId > PrevRunId)
                    {
                        Assert.AreNotEqual(ret, 0);
                    }
                    else
                    {
                        Assert.Fail();
                    }

                }

            }

        }


        [TestMethod, Timeout(500000)]
        public void BatchConstraint_Met()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();

                // remove any test SQL data and objects
                string sqlDropString = @"
                    delete ETLProcess
                    where Process in ('dbo.prc_MaxRunTimeCheck','dbo.prc_EventCheck_Process2');                  
                    delete	e
                    from	ETL_Event.dbo.[Event] e
                    join	ETL_Event.dbo.EventType et
                    on		e.EventTypeID = et.EventTypeID
                    and		et.EventTypeName in ('Process1_FINISHED','Process2_FINISHED'); 
                    delete	l
                    from	ETL_Event.dbo.EventLog l
                    join	ETL_Event.dbo.EventType et
                    on		l.EventTypeID = et.EventTypeID
                    and		et.EventTypeName in ('Process1_FINISHED','Process2_FINISHED'); 
                    delete	ETL_Event.dbo.EventType
                    where	EventTypeName in ('Process1_FINISHED','Process2_FINISHED');
                    IF  exists (select * from sys.objects where object_id = OBJECT_ID(N'dbo.prc_MaxRunTimeCheck') AND type in (N'P', N'PC'))
                        drop procedure dbo.prc_MaxRunTimeCheck;
                    IF  exists (select * from sys.objects where object_id = OBJECT_ID(N'dbo.prc_EventCheck_Process2') AND type in (N'P', N'PC'))
                        drop procedure dbo.prc_EventCheck_Process2;";

                // create test SQL data and objects
                string sqlString = @"
                    set identity_insert dbo.ETLProcess on;
                    insert ETLProcess (ProcessID,Process,[Param],ScopeID)
                    select 100,'dbo.prc_MaxRunTimeCheck',NULL,12;
                    insert ETLProcess (ProcessID,Process,[Param],ScopeID)
                    select 101,'dbo.prc_EventCheck_Process2',NULL,12;
                    set identity_insert dbo.ETLProcess off;                    
                    insert ETL_Event.dbo.EventType(EventTypeID,EventTypeName,EventArgsSchema,SourceName,LogRetention,CreateDT,ModifyDT)
                    select NEWID(),'Process1_FINISHED','EventArgs.XSD','',100,dateadd(mi,-1, getdate()),dateadd(mi,-1, getdate());
                    insert ETL_Event.dbo.EventType(EventTypeID,EventTypeName,EventArgsSchema,SourceName,LogRetention,CreateDT,ModifyDT)
                    select NEWID(),'Process2_FINISHED','EventArgs.XSD','',100,GETDATE(),GETDATE();";

                SqlCommand sql_cmd = new SqlCommand(sqlDropString + sqlString, connection);
                sql_cmd.ExecuteNonQuery();

                // create test SP
                sqlString = System.IO.File.ReadAllText(TestFilesDirPath + "prc_MaxRunTimeCheck.sql");
                sql_cmd.CommandText = sqlString;
                sql_cmd.ExecuteNonQuery();

                // create 2nd test SP
                sqlString = System.IO.File.ReadAllText(TestFilesDirPath + "prc_EventCheck_Process2.sql");
                sql_cmd.CommandText = sqlString;
                sql_cmd.ExecuteNonQuery();

                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'Process1';exec dbo.prc_Execute 'Process2';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }

                Assert.AreEqual(ret, 0);

                // remove test SQL data and objects
                sql_cmd.CommandText = sqlDropString;
                sql_cmd.ExecuteNonQuery();

            }

        }


        [TestMethod, Timeout(500000)]
        public void BatchConstraint_Not_Met()
        {
            string cs = s_cs;
            int ret = -1;
            int PrevRunId = 0;
            int LaterRunId = 0;
            string sqlString = "";
            string sqlDropString = "";

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();

                try
                {

                    // remove any test SQL data and objects
                    sqlDropString = @"
                    delete ETLProcess
                    where Process in ('dbo.prc_MaxRunTimeCheck','dbo.prc_EventCheck_Process2');                  
                    delete	e
                    from	ETL_Event.dbo.[Event] e
                    join	ETL_Event.dbo.EventType et
                    on		e.EventTypeID = et.EventTypeID
                    and		et.EventTypeName in ('Process1_FINISHED','Process2_FINISHED'); 
                    delete	l
                    from	ETL_Event.dbo.EventLog l
                    join	ETL_Event.dbo.EventType et
                    on		l.EventTypeID = et.EventTypeID
                    and		et.EventTypeName in ('Process1_FINISHED','Process2_FINISHED'); 
                    delete	ETL_Event.dbo.EventType
                    where	EventTypeName in ('Process1_FINISHED','Process2_FINISHED');
                    IF  exists (select * from sys.objects where object_id = OBJECT_ID(N'dbo.prc_MaxRunTimeCheck') AND type in (N'P', N'PC'))
                        drop procedure dbo.prc_MaxRunTimeCheck;
                    IF  exists (select * from sys.objects where object_id = OBJECT_ID(N'dbo.prc_EventCheck_Process2') AND type in (N'P', N'PC'))
                        drop procedure dbo.prc_EventCheck_Process2;";

                    // create test SQL data and objects
                    sqlString = @"
                    set identity_insert dbo.ETLProcess on;
                    insert ETLProcess (ProcessID,Process,[Param],ScopeID)
                    select 100,'dbo.prc_MaxRunTimeCheck',NULL,12;
                    insert ETLProcess (ProcessID,Process,[Param],ScopeID)
                    select 101,'dbo.prc_EventCheck_Process2',NULL,12;
                    set identity_insert dbo.ETLProcess off;                    
                    insert ETL_Event.dbo.EventType(EventTypeID,EventTypeName,EventArgsSchema,SourceName,LogRetention,CreateDT,ModifyDT)
                    select NEWID(),'Process1_FINISHED','EventArgs.XSD','',100,GETDATE(),GETDATE();
                    insert ETL_Event.dbo.EventType(EventTypeID,EventTypeName,EventArgsSchema,SourceName,LogRetention,CreateDT,ModifyDT)
                    select NEWID(),'Process2_FINISHED','EventArgs.XSD','',100,GETDATE(),GETDATE();";

                    SqlCommand sql_cmd = new SqlCommand(sqlDropString + sqlString, connection);
                    sql_cmd.ExecuteNonQuery();

                    // create test SP
                    sqlString = System.IO.File.ReadAllText(TestFilesDirPath + "prc_MaxRunTimeCheck.sql");
                    sql_cmd.CommandText = sqlString;
                    sql_cmd.ExecuteNonQuery();

                    // create 2nd test SP
                    sqlString = System.IO.File.ReadAllText(TestFilesDirPath + "prc_EventCheck_Process2.sql");
                    sql_cmd.CommandText = sqlString;
                    sql_cmd.ExecuteNonQuery();

                    sqlString = @"
                    select IsNull(max(RunId),0)
                    from ETLStepRunHistoryLog l with (nolock)
                    join ETLBatch b	with (nolock)
                    on l.BatchID = b.BatchID
                    and b.BatchName = 'Process2';";
                    sql_cmd.CommandText = sqlString;
                    PrevRunId = (int)sql_cmd.ExecuteScalar();

                    sqlString = @"exec dbo.prc_Execute 'Process2';";
                    sql_cmd.CommandText = sqlString;
                    sql_cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                    sql_cmd.CommandTimeout = 500000;
                    sql_cmd.ExecuteNonQuery();
                    ret = (int)sql_cmd.Parameters["@RETVAL"].Value;

                }
                catch
                {
                    sqlString = @"
                        select IsNull(max(RunId),0)
                        from ETLStepRunHistoryLog l with (nolock)
                        join ETLBatch b	with (nolock)
                        on l.BatchID = b.BatchID
                        and b.BatchName = 'Process2'
                        and l.LogMessage like '%ERROR Constraint=2 was not met'
                        and l.Err = 50107
                        and RunID > " + Convert.ToString(PrevRunId) + ";";
                    SqlCommand sql_cmd = new SqlCommand(sqlString, connection);
                    LaterRunId = (int)sql_cmd.ExecuteScalar();

                    if (LaterRunId > PrevRunId)
                    {
                        Assert.AreNotEqual(ret, 0);
                    }
                    else
                    {
                        Assert.Fail();
                    }

                    // remove test SQL data and objects
                    sql_cmd.CommandText = sqlDropString;
                    sql_cmd.ExecuteNonQuery();

                }

            }

        }


        [TestMethod, Timeout(500000)]
        public void MoveData_TableToFile()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'MoveData_TableToFile';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);
        }



        [TestMethod, Timeout(500000)]
        public void MoveData_TableToTable()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'MoveData_TableToTable';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);
        }


        //[TestMethod, Timeout(500000)]
        //public void MoveData_Excel()
        //{
        //    string cs = s_cs;
        //    int ret = -1;

        //    Excel.Application excelApp = new Excel.Application();
        //    Excel.Workbook wb = excelApp.Workbooks.Add();
        //    Excel.Worksheet WithHeaderSheet = wb.Sheets[1];

        //    WithHeaderSheet.Name = "WithHeaders";
        //    excelApp.Cells[1, 1] = "BatchID";
        //    excelApp.Cells[1, 2] = "BatchName";

        //    Excel.Worksheet WithOutHeaderSheet = wb.Sheets[2];
        //    WithOutHeaderSheet.Name = "WithOutHeaders";

        //    string workbookPath = @"c:\builds\ExcelBatchTest.xls";
        //    if (System.IO.File.Exists(workbookPath))
        //        System.IO.File.Delete(workbookPath);

        //    wb.SaveAs(
        //        workbookPath,
        //        Excel.XlFileFormat.xlWorkbookNormal, "", "", false, false,
        //        Excel.XlSaveAsAccessMode.xlNoChange,
        //        Excel.XlSaveConflictResolution.xlLocalSessionChanges, false, "", "", true);

        //    wb.Close();

        //    using (SqlConnection connection = new SqlConnection(cs))
        //    {
        //        connection.Open();
        //        using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'MoveData_Excel';", connection))
        //        {
        //            cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

        //            cmd.ExecuteNonQuery();
        //            ret = (int)cmd.Parameters["@RETVAL"].Value;
        //        }
        //    }

        //    Assert.AreEqual(ret, 0);

        //    if (System.IO.File.Exists(workbookPath))
        //        System.IO.File.Delete(workbookPath);

        //}



        [TestMethod, Timeout(500000)]
        public void SeqGroup()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'SeqGroup';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);
        }


        [TestMethod]
        public void Call_BCP()
        {
            string cs = s_cs;
            int ret = -1;

            using (SqlConnection connection = new SqlConnection(cs))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("exec dbo.prc_Execute 'Call_BCP';", connection))
                {
                    cmd.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    cmd.ExecuteNonQuery();
                    ret = (int)cmd.Parameters["@RETVAL"].Value;
                }
            }

            Assert.AreEqual(ret, 0);
        }

        /*
        [TestMethod, Timeout(500000)]
        public void QueryType_MDX()
        {
            string cs = s_cs;
            int ret = -1;
            
            using (SqlConnection connSql = new SqlConnection(cs))
            {
                connSql.Open();

                string strSql = System.IO.File.ReadAllText(TestFilesDirPath + "Create_QueryType_MDX_Tables.sql");
                SqlCommand cmdSql = new SqlCommand(strSql,connSql);
                cmdSql.ExecuteNonQuery();

                AdomdConnection connCube = new AdomdConnection("Provider=MSOLAP;Data Source=.");
                connCube.Open();

                AdomdCommand cmdCube = connCube.CreateCommand();
                cmdCube.CommandType = CommandType.Text;

                cmdCube.CommandText = System.IO.File.ReadAllText(TestFilesDirPath + "Create_Cube.xmla");
                cmdCube.ExecuteNonQuery();

                cmdCube.CommandText = System.IO.File.ReadAllText(TestFilesDirPath + "Process_Cube.xmla");
                cmdCube.ExecuteNonQuery();

                strSql = "exec dbo.prc_Execute 'QueryType_MDX';";
                cmdSql.CommandText = strSql;
                cmdSql.Parameters.Add("@RETVAL", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                cmdSql.ExecuteNonQuery();
                ret = (int)cmdSql.Parameters["@RETVAL"].Value;

                Assert.AreEqual(ret, 0);

                cmdCube.CommandText = System.IO.File.ReadAllText(TestFilesDirPath + "Delete_Cube.xmla");
                cmdCube.ExecuteNonQuery();

                connCube.Close();

                strSql = System.IO.File.ReadAllText(TestFilesDirPath + "Drop_QueryType_MDX_Tables.sql");
                cmdSql.CommandText = strSql;
                cmdSql.ExecuteNonQuery();

            }
      
        }*/
    }
}
