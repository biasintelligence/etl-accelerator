using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using System.IO;

using mwrt = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.InteropServices;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
        public class SSISFlatFileConnectionProperties
    {
        private int m_CodePage = 1252;
        private bool m_ColumnNamesInFirstDataRow = false;
        private string m_ConnectionString;
        private int m_DataRowsToSkip = 0;
        private string m_Format = "Delimited";
        private string m_HeaderRowDelimiter = "\r\n";
        private int m_HeaderRowsToSkip = 0;
        private string m_TextQualifier;
        private bool m_Unicode = false;
        private string m_ColumnDelimiter = "\t";
        private string m_RecordDelimiter = "\r\n";

        public int CodePage
        {
            get { return m_CodePage; }
            set { m_CodePage = value; }
        }
        public bool ColumnNamesInFirstDataRow
        {
            get { return m_ColumnNamesInFirstDataRow; }
            set { m_ColumnNamesInFirstDataRow = value; }
        }
        public string ConnectionString
        {
            get { return m_ConnectionString; }
            set { m_ConnectionString = value; }
        }
        public int DataRowsToSkip
        {
            get { return m_DataRowsToSkip; }
            set { m_DataRowsToSkip = value; }
        }
        public string Format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }
        public string HeaderRowDelimiter
        {
            get { return m_HeaderRowDelimiter; }
            set { m_HeaderRowDelimiter = FixEscapeChar(value); }
        }
        public int HeaderRowsToSkip
        {
            get { return m_HeaderRowsToSkip; }
            set { m_HeaderRowsToSkip = value; }
        }
        public string TextQualifier
        {
            get { return m_TextQualifier; }
            set { m_TextQualifier = FixEscapeChar(value); }
        }
        public bool Unicode
        {
            get { return m_Unicode; }
            set { m_Unicode = value; }
        }
        public string FileName
        {
            get { return m_ConnectionString; }
        }
        public string ColumnDelimiter
        {
            get { return m_ColumnDelimiter; }
            set { m_ColumnDelimiter = FixEscapeChar(value); }
        }
        public string RecordDelimiter
        {
            get { return m_RecordDelimiter; }
            set { m_RecordDelimiter = FixEscapeChar(value); }
        }

        private string FixEscapeChar(string val)
        {
            val = val.Replace("\\t", "\t");
            val = val.Replace("\\n", "\n");
            val = val.Replace("\\r", "\r");
            return val;
        }
    }    
    
    
    public static class SSISFlatFileConnection
    {

        public static void ConfigureConnectionManager(ConnectionManager cm, SSISFlatFileConnectionProperties prop, Dictionary<string,MyColumn> columnCollection, ILogger logger)
        {
            cm.Description = "connect to a flat file";
            logger.Debug("DE added FlatFile connection to {File}",prop.ConnectionString);

            //set connection properties
            mwrt.IDTSConnectionManagerFlatFile100 fcm = cm.InnerObject as mwrt.IDTSConnectionManagerFlatFile100;

            cm.Properties["ConnectionString"].SetValue(cm, prop.ConnectionString);

            fcm.CodePage = prop.CodePage;
            fcm.Unicode = prop.Unicode;
            fcm.ColumnNamesInFirstDataRow = prop.ColumnNamesInFirstDataRow;
            fcm.DataRowsToSkip = prop.DataRowsToSkip;
            fcm.Format = prop.Format;
            fcm.HeaderRowDelimiter = prop.HeaderRowDelimiter;
            fcm.HeaderRowsToSkip = prop.HeaderRowsToSkip;
            fcm.RowDelimiter = prop.RecordDelimiter;
            if (prop.TextQualifier != null)
            {
                fcm.TextQualifier = prop.TextQualifier;
            }
            mwrt.IDTSConnectionManagerFlatFileColumn100 fColumn;
            mwrt.IDTSName100 fName;
            if (columnCollection != null && columnCollection.Count > 0)
            {
                //define input column
                int i = 1;
                foreach (MyColumn dsvCol in columnCollection.Values)
                {
                    fColumn = fcm.Columns.Add();
                    fColumn.ColumnType = fcm.Format;
                    fColumn.DataType = dsvCol.DataType;
                    fColumn.DataPrecision = dsvCol.Precision;

                    fColumn.TextQualified = (fcm.TextQualifier != null
                        && (dsvCol.DataType == mwrt.DataType.DT_STR
                        || dsvCol.DataType == mwrt.DataType.DT_WSTR));

                    fColumn.ColumnDelimiter = (columnCollection.Count == i) ? prop.RecordDelimiter : prop.ColumnDelimiter;
                    //fColumn.ColumnDelimiter = (dsv.ColumnCollection.Count == i) ? "\r\n" : "\t";
                    fColumn.DataScale = dsvCol.Scale;
                    fColumn.MaximumWidth = dsvCol.Length;
                    fName = (mwrt.IDTSName100)fColumn;
                    fName.Name = dsvCol.Name;
                    i++;
                }
            }
            else if (prop.ColumnNamesInFirstDataRow)
            {
                //use file header
                string header = string.Empty;
                using (StreamReader sr = File.OpenText(cm.ConnectionString))
                {
                    for (int l = 0; l <= fcm.HeaderRowsToSkip; l++)
                    {
                        header = sr.ReadLine();
                    }
                    sr.Close();
                }

                string[] del = new string[] { prop.ColumnDelimiter };
                string[] cols = header.Split(del, StringSplitOptions.None);
                int i = 1;
                foreach (string col in cols)
                {
                    fColumn = fcm.Columns.Add();
                    fColumn.ColumnType = fcm.Format;
                    fColumn.DataType = (fcm.Unicode) ? mwrt.DataType.DT_WSTR : mwrt.DataType.DT_STR;

                    fColumn.TextQualified = (fcm.TextQualifier != null);
                    fColumn.ColumnDelimiter = (cols.Length == i) ? prop.RecordDelimiter : prop.ColumnDelimiter;
                    //fColumn.ColumnDelimiter = (dsv.ColumnCollection.Count == i) ? "\r\n" : "\t";
                    fColumn.MaximumWidth = 255;
                    fName = (mwrt.IDTSName100)fColumn;
                    fName.Name = col;
                    i++;
                }
            }
        }

    }
}
