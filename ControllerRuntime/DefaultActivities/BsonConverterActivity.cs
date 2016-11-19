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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Bson;

using ControllerRuntime;


namespace DefaultActivities
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


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>() { INPUT_FILE, OUTPUT_FOLDER, TIMEOUT };

        #region IWorkflowActivity
        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Name, attribute.Value);
            }



            _logger.Write(String.Format("Bson : {0} => {1}", _attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER]));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            BsonToJson(_attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER], token);
            result = WfResult.Succeeded;

            return result;
        }
        #endregion

        private void BsonToJson(string input, string output, CancellationToken token)
        {

            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (token.IsCancellationRequested)
                    break;

                FileInfo fileToConvert = new FileInfo(file);
                string outputFile = Path.Combine(output, fileToConvert.Name + ".json");
                using (FileStream originalFileStream = fileToConvert.OpenRead())
                {
                    if ((File.GetAttributes(fileToConvert.FullName) &
                       FileAttributes.Hidden) != FileAttributes.Hidden & fileToConvert.Extension != ".json")
                    {

                        StreamReader input_reader = new StreamReader(originalFileStream, Encoding.UTF8);
                        byte[] data = Convert.FromBase64String(input_reader.ReadToEnd());
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            using (BsonReader reader = new BsonReader(ms))
                            {
                                using (FileStream jsonFileStream = File.Create(outputFile))
                                {
                                    JsonSerializer jser = new JsonSerializer();
                                    //JObject json =  jser.Deserialize(reader) as JObject;
                                    var json = jser.Deserialize(reader);

                                    StreamWriter writer = new StreamWriter(jsonFileStream,Encoding.UTF8);
                                    writer.Write(json);
                                    writer.Flush();
 
                                }
                            }
                        }


                        FileInfo info = new FileInfo(outputFile);
                        _logger.Write(String.Format("Converted {0} => {1}", fileToConvert.Name,info.Name));
                    }

                }
            }
        }

    }

}
