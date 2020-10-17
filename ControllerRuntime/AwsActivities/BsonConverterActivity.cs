/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     11/18/2016
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Bson;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

using Serilog;
using ControllerRuntime;


namespace AwsActivities
{
    /// <summary>
    /// Gzip compression
    /// Example: InputFile = c:\test.txt.gz, OutputFolder = c:\output Command = C(compress)/D(decompress)
    /// </summary>
    public class BsonConverterActivity : IWorkflowActivity
    {
        private const string INPUT_FILE = "InputFile";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string TIMEOUT = "Timeout";


        private WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        private ILogger _logger;
        private List<string> _required_attributes = new List<string>() { INPUT_FILE, OUTPUT_FOLDER, TIMEOUT };

        #region IWorkflowActivity
        public IEnumerable<string> RequiredAttributes
        {
            get { return _required_attributes; }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Count)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (var attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Key, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Key, attribute.Value);
            }

            _logger.Information("Bson : {From} => {To}", _attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER]);

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Int32.Parse(_attributes[TIMEOUT]))))
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token))
                {

                    BsonToJson(_attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER], linkedCts.Token);
                    result = WfResult.Succeeded;
                }
            }

            return result;
        }
        #endregion

        private void BsonToJson(string input, string output, CancellationToken token)
        {

            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();

                FileInfo fileToConvert = new FileInfo(file);
                string outputFile = Path.Combine(output, Path.GetFileNameWithoutExtension(file) + ".json");
                string outputDir = Path.GetDirectoryName(outputFile);
                Directory.CreateDirectory(outputDir);
                using (FileStream originalFileStream = fileToConvert.OpenRead())
                {
                    if ((File.GetAttributes(fileToConvert.FullName) &
                       FileAttributes.Hidden) != FileAttributes.Hidden & fileToConvert.Extension != ".json")
                    {

                        using (FileStream jsonFileStream = File.Create(outputFile))
                        {
                            StreamWriter writer = new StreamWriter(jsonFileStream, Encoding.UTF8);
                            using (var reader = new BsonBinaryReader(originalFileStream))
                            {
                                while (!reader.IsAtEndOfFile())
                                {
                                    token.ThrowIfCancellationRequested();

                                    var bson = BsonSerializer.Deserialize<BsonDocument>(reader);
                                    string json = bson.ToJson(new JsonWriterSettings() { OutputMode = JsonOutputMode.CanonicalExtendedJson });
                                    writer.Write(json);
                                }
                                writer.Flush();
                            }

                        }

                    }

                    FileInfo info = new FileInfo(outputFile);
                    _logger.Information("Converted {From} => {To}", fileToConvert.Name,info.Name);
                }
            }
        }

    }

}
