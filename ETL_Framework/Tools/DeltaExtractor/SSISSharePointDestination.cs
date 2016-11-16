using System;
using System.Globalization;
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

using mwrt = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.InteropServices;

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISSharePointDestination : SSISModule
    {

        public SSISSharePointDestination(SharePointDestination spdst, MainPipe pipe, IDTSComponentMetaData100 src, int outputID)
            : base(pipe, "SharePoint List Destination", outputID, "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListDestination, SharePointListAdapters, Version=1.2012.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47")
        {
            //Create a new SharePointDestination component

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            foreach (KeyValuePair<string, object> prop in spdst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            this.Reinitialize(dcomp);

            //Create datatype converter if needed
            Dictionary<string, int> converted = new Dictionary<string, int>();
            IDTSVirtualInput100 vInput = src.InputCollection[0].GetVirtualInput();
            if (this.needDataTypeChange(vInput, comp.InputCollection[0]))
            {
                //create the destination column collection
                Dictionary<string, MyColumn> exColumns = new Dictionary<string, MyColumn>();
                foreach (IDTSExternalMetadataColumn100 exColumn in comp.InputCollection[0].ExternalMetadataColumnCollection)
                {
                    MyColumn col = new MyColumn();
                    col.Name = exColumn.Name;
                    col.DataType = exColumn.DataType;
                    col.Length = exColumn.Length;
                    col.Precision = exColumn.Precision;
                    col.Scale = exColumn.Scale;
                    col.CodePage = exColumn.CodePage;
                    exColumns.Add(exColumn.Name, col);
                }
                SSISDataConverter ssisdc = new SSISDataConverter(pipe, src, outputID, exColumns);
                src = ssisdc.MetadataCollection;
                converted = ssisdc.ConvertedColumns;
                outputID = 0;
            }

            this.ConnectComponents(src, outputID);
            this.MatchInputColumns(converted, false);
        }
    }
}
