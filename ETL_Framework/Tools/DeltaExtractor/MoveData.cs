using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace BIAS.Framework.DeltaExtractor
{

    [XmlRoot(Namespace = "DeltaExtractor.XSD", ElementName = "SavePackage")]
    public class SavePackageBlock
    {
        [XmlTextAttribute()]
        public string File { get; set; }

        [XmlAttribute("Save")]
        public bool Save { get; set; }
        [XmlAttribute("Load")]
        public bool Load { get; set; }
    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD", ElementName = "Partition")]
    public class PartitionBlock
    {
        [XmlAttribute("Function")]
        public string Function { get; set; }

        //current implementation of PartitionData custom component only can use Partition_Key as output
        private string partitionfunctionoutput = @"Partition_Key";
        public string Output
        {
            get { return this.partitionfunctionoutput; }
            set { this.partitionfunctionoutput = value; }
        }

        public string Input { get; set; }

    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class ETLHeader
    {

        public int BatchID { get; set; }
        public int StepID { get; set; }
        public int RunID { get; set; }
        public DBConnection Controller { get; set; }
        public DBConnection Node { get; set; }
        public Guid Conversation { get; set; }
        public Guid ConversationGrp { get; set; }
        public int Options { get; set; }

    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class BIASHeader
    {

        [XmlAttribute("RunID")]
        public int RunID { get; set; }
        [XmlAttribute("UserOptions")]
        public string UserOptions { get; set; }

    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class MoveData
    {       
        public DataSource DataSource {get; set;}
        public DataDestination DataDestination {get; set;}
        //dsv location
        public string StagingAreaRoot { get; set; }
        //partition functions UPS or SRC are supported using PartitionData custom component NONE if not partitioned
        public PartitionBlock Partition { get; set; }
        public SavePackageBlock SavePackage { get; set; }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class RunPackage
    {
        public string File { get; set; }
    }

}
