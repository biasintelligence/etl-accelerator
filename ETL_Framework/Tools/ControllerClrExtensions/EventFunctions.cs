using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Microsoft.SqlServer.Server;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Security.Principal;
using System.Net;
using System.Xml.Serialization;
using System.Security.Permissions;
using System.Diagnostics;
using System.IO;

namespace ETL_Framework.ControllerCLRExtensions
{
    public static class EventFunctions
    {

        public static void PostEvent(String ConnectionStr, SqlString EventType, SqlDateTime EventPosted, SqlXml EventArgs, SqlString Options)
        {
            using (SqlConnection cn = new SqlConnection(ConnectionStr))
            {
                try
                {
                    cn.Open();
                }
                catch (Exception e)
                {
                    throw e;
                }

                using (SqlCommand cmd = new SqlCommand("dbo.prc_EventPost",cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EventType", EventType);
                    cmd.Parameters.AddWithValue("@EventPosted", EventPosted);
                    cmd.Parameters.AddWithValue("@EventArgs", EventArgs);
                    cmd.Parameters.AddWithValue("@Options", Options);
                    cmd.CommandTimeout = 0;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

        }

        public static void ReceiveEvent(String ConnectionStr, out SqlGuid EventId, out SqlDateTime EventPosted, out SqlDateTime EventReceived, out SqlXml EventArgs, SqlString EventType, SqlString Options)
        {
            using (SqlConnection cn = new SqlConnection(ConnectionStr))
            {
                try
                {
                    cn.Open();
                }
                catch (Exception e)
                {
                    throw e;
                }

                using (SqlCommand cmd = new SqlCommand("dbo.prc_EventGet", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@EventID", SqlDbType.UniqueIdentifier).Value = SqlGuid.Null;
                    cmd.Parameters["@EventID"].Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@EventPosted", SqlDbType.DateTime).Value = SqlDateTime.Null;
                    cmd.Parameters["@EventPosted"].Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@EventReceived", SqlDbType.DateTime).Value = SqlDateTime.Null;
                    cmd.Parameters["@EventReceived"].Direction = ParameterDirection.Output;
                    cmd.Parameters.AddWithValue("@EventArgs", SqlDbType.Xml).Value = SqlXml.Null;
                    cmd.Parameters["@EventArgs"].Direction = ParameterDirection.Output;
                    cmd.Parameters.AddWithValue("@EventType", EventType);
                    cmd.Parameters.AddWithValue("@Options", Options);
                    cmd.CommandTimeout = 0;

                    try
                    {
                        cmd.ExecuteNonQuery();

                        EventId = (SqlGuid)cmd.Parameters["@EventID"].SqlValue;
                        EventPosted = (SqlDateTime)cmd.Parameters["@EventPosted"].SqlValue;
                        EventReceived = (SqlDateTime)cmd.Parameters["@EventReceived"].SqlValue;
                        EventArgs = (SqlXml)cmd.Parameters["@EventArgs"].SqlValue;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

        }

    }
}
