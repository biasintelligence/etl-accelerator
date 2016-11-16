using System;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;

namespace BIAS.Framework.DeltaExtractor
{
    public class MyColumn
    {
        public string Name {get; set;}
        public DataType DataType {get; set;}
        public int Length {get; set;}
        public int Precision {get; set;}
        public int Scale {get; set;}
        public int CodePage {get; set;}
    }
}
