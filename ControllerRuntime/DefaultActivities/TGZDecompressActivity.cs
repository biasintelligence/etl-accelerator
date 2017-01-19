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
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;


namespace DefaultActivities
{
    /// <summary>
    /// Gzip compression
    /// Example: InputFile = c:\test.txt.gz, OutputFolder = c:\output Command = C(compress)/D(decompress)
    /// </summary>
    public class TGZDecompressActivity : IWorkflowActivity
    {
        private const string INPUT_FILE = "InputFile";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string OUTPUT_EXT = "OutputExt";
        private const string TIMEOUT = "Timeout";
        private const string DECOMPRESS_MODE = "Mode"; //supported gz = .gz,tgz = .tar.gz

        private List<string> supported_mode = new List<string>() { "gz", "tgz" };


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>() { INPUT_FILE, OUTPUT_FOLDER, TIMEOUT, DECOMPRESS_MODE, OUTPUT_EXT };

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

                if (attribute.Name.Equals(DECOMPRESS_MODE, StringComparison.InvariantCultureIgnoreCase)
                    && !supported_mode.Contains(attribute.Value.ToLower()))
                {
                    throw new ArgumentException(String.Format("Unsupported {0}: {1}", DECOMPRESS_MODE, attribute.Value));
                }

            }

            _logger.Write(String.Format("TGZ mode:{2}: {0} => {1}", _attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER], _attributes[DECOMPRESS_MODE]));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            switch (_attributes[DECOMPRESS_MODE])
            {
                case "gz":
                    Decompress(token);
                    break;
                case "tgz":
                    DecompressTGZ(token);
                    break;
                default:
                    throw new ArgumentException(String.Format("Unsupported {0}: {1}", DECOMPRESS_MODE, _attributes[DECOMPRESS_MODE]));
            }
            result = WfResult.Succeeded;

            return result;
        }
        #endregion


        private void Decompress(CancellationToken token)
        {
            string input = _attributes[INPUT_FILE];
            string output = _attributes[OUTPUT_FOLDER];
            string ext = _attributes[OUTPUT_EXT];
            Directory.CreateDirectory(output);

            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();
                //fileToDecompress.Name.Remove(fileToDecompress.FullName.Length - fileToDecompress.Extension.Length
                FileInfo fileToDecompress = new FileInfo(file);
                string outputFile = Path.Combine(output, Path.GetFileNameWithoutExtension(file));
                outputFile = String.Concat(outputFile, ext);
                using (FileStream originalFileStream = fileToDecompress.OpenRead())
                using (FileStream decompressedFileStream = File.Create(outputFile))
                using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(decompressedFileStream);
                    _logger.Write(String.Format("Decompressed: {0}", fileToDecompress.Name));
                }
            }
        }


        private void DecompressTGZ(CancellationToken token)
        {

            string input = _attributes[INPUT_FILE];
            string output = _attributes[OUTPUT_FOLDER];

            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();

                using (Stream inStream = File.OpenRead(file))
                using (Stream gzipStream = new GZipInputStream(inStream))
                using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
                {
                    try
                    {
                        tarArchive.ExtractContents(output);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                _logger.Write(String.Format("Decompressed to {0}", output));
            }
        }

    }

}
