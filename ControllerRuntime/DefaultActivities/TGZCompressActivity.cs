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
    public class TGZCompressActivity : IWorkflowActivity
    {
        private const string INPUT_FILE = "InputFile";
        private const string ARCHIVE_NAME = "ArchiveName";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string TIMEOUT = "Timeout";


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>() { INPUT_FILE, ARCHIVE_NAME, OUTPUT_FOLDER, TIMEOUT };

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



            _logger.Write(String.Format("TGZ ({2}): {0} => {1}; archive: {2}", _attributes[INPUT_FILE], _attributes[OUTPUT_FOLDER], _attributes[ARCHIVE_NAME]));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            CompressTGZ(_attributes[INPUT_FILE], _attributes[ARCHIVE_NAME], _attributes[OUTPUT_FOLDER], token);
            result = WfResult.Succeeded;

            return result;
        }
        #endregion

        private void Compress(string input, string output, CancellationToken token)
        {

            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
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


        private void CompressTGZ(string input, string archive, string output, CancellationToken token)
        {

            string sourceDirectory = Path.GetDirectoryName(input);



            string archive_name = archive.Replace(".tar.gz", "") + ".tar.gz";
            string outputFile = Path.Combine(output, archive_name);

                using (Stream outStream = File.Create(outputFile))
                {
                    using (Stream gzoStream = new GZipOutputStream(outStream))
                    {
                        using (TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
                        {
                        try
                        {
                            // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
                            // and must not end with a slash, otherwise cuts off first char of filename
                            // This is scheduled for fix in next release
                            tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
                            if (tarArchive.RootPath.EndsWith("/"))
                                tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

                            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
                            tarArchive.WriteEntry(tarEntry, false);

                            // Write each file to the tar.
                            string[] files = Directory.GetFiles(sourceDirectory, Path.GetFileName(input), SearchOption.TopDirectoryOnly);
                            foreach (string file in files)
                            {
                                if (token.IsCancellationRequested)
                                    break;

                                tarEntry = TarEntry.CreateEntryFromFile(file);
                                tarArchive.WriteEntry(tarEntry, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        //finally
                        //{
                        //    tarArchive.Close();
                        //    gzoStream.Close();
                        //    outStream.Close();
                        //}

                    }
                }

                FileInfo info = new FileInfo(outputFile);
                _logger.Write(String.Format("Compressed to {0} ({1} bytes)", info, info.Length.ToString()));

            }
        }

    }

}
