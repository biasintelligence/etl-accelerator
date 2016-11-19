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

using ControllerRuntime;


namespace DefaultActivities
{
    /// <summary>
    /// Gzip compression
    /// Example: InputFile = c:\test.txt.gz, OutputFolder = c:\output Command = C(compress)/D(decompress)
    /// </summary>
    public class GzipActivity : IWorkflowActivity
    {
        private const string INPUT_FILE = "InputFile";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string COMMAND = "Command";
        private const string TIMEOUT = "Timeout";


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>() { INPUT_FILE, OUTPUT_FOLDER, COMMAND, TIMEOUT };

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



            _logger.Write(String.Format("Gzip ({2}): {0} => {1}", _attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER],_attributes[COMMAND]));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            if(_attributes[COMMAND].Equals("C"))
            {
                Compress(_attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER], token);
                result = WfResult.Succeeded;
            }
            else if (_attributes[COMMAND].Equals("D"))
            {
                Decompress(_attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER], token);
                result = WfResult.Succeeded;
            }
            else
            {
                throw new ArgumentException(String.Format("Invalid Command (expected: C - compress, D - decompress): {0}", _attributes[COMMAND]));
            }

            return result;
        }
        #endregion

        private void Compress(string input, string output,CancellationToken token)
        {
        
            string[] files = Directory.GetFiles(Path.GetDirectoryName(input),Path.GetFileName(input),SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (token.IsCancellationRequested)
                    break;

                FileInfo fileToCompress = new FileInfo(file);
                string outputFile = Path.Combine(output, fileToCompress.Name + ".gz");
                using (FileStream originalFileStream = fileToCompress.OpenRead())
                {
                    if ((File.GetAttributes(fileToCompress.FullName) &
                       FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                    {
                        using (FileStream compressedFileStream = File.Create(outputFile))
                        {
                            using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                               CompressionMode.Compress))
                            {
                                originalFileStream.CopyTo(compressionStream);

                            }
                        }

                        FileInfo info = new FileInfo(outputFile);
                        _logger.Write(String.Format("Compressed {0} from {1} to {2} bytes.",
                        fileToCompress.Name, fileToCompress.Length.ToString(), info.Length.ToString()));
                    }

                }
            }
        }

        private void Decompress(string input, string output,CancellationToken token)
        {

            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (token.IsCancellationRequested)
                    break;
                //fileToDecompress.Name.Remove(fileToDecompress.FullName.Length - fileToDecompress.Extension.Length
                FileInfo fileToDecompress = new FileInfo(file);
                string outputFile = Path.Combine(output, Path.GetFileNameWithoutExtension(file));
                using (FileStream originalFileStream = fileToDecompress.OpenRead())
                {
                    using (FileStream decompressedFileStream = File.Create(outputFile))
                    {
                        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(decompressedFileStream);
                            _logger.Write(String.Format("Decompressed: {0}", fileToDecompress.Name));
                        }
                    }
                }
            }


        }


    }

}
