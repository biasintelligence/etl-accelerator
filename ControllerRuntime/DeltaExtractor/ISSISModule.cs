using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace BIAS.Framework.DeltaExtractor
{
    public interface ISSISModule
    {
        IDTSComponentMetaData100 Initialize();
        IDTSComponentMetaData100 MetadataCollection { get; }
        IDTSComponentMetaData100 Connect(IDTSComponentMetaData100 src, int outputID = 0);
        IDTSComponentMetaData100 ConnectDestination(IDTSComponentMetaData100 src, int outputID = 0);
    }
}
