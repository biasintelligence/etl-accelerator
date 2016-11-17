using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Xml;

namespace BIAS.Framework.DeltaExtractor
{
    public class PrintOutput
    {
        
        /// <summary>
        /// Prints text to console with timestamp
        /// </summary>
        /// <param name="in_strOutputMessage"> Text to output </param>
        /// <param name="loggingEnabled"> determines whether what is passed in is written to a log file</param>
        /// <param name="verbose">whether to print the message to Console</param>
        /// 
        //private static readonly string LOG_MESSAGE_SOURCE = "DeltaExtractor";

        //private static Dictionary<string, EventLogger> s_loggers = new Dictionary<string, EventLogger>() ;

        //public static void AddEventLogger(string loggerName, EventLogger logger)
        //{ 
        //    if (!(s_loggers.ContainsKey(loggerName)))
        //    {
        //        s_loggers.Add(loggerName, logger) ;
        //    }
        //}

        public static void PrintToOutput(string in_strOutputMessage,int in_err, bool loggingEnabled)
        {
            if (!loggingEnabled)
                return;
            
            //if (s_loggers.Count > 0)
            //{
            //    foreach (EventLogger l in s_loggers.Values)
            //    {
            //        l.LogEvent(LOG_MESSAGE_SOURCE, in_strOutputMessage, in_err, null);
            //    }
            //}

            if (ETLController.Connected)
            {
                ETLController.LogMessage(in_strOutputMessage, in_err);
            }
            else
            {
                Console.WriteLine(String.Format("Err={0}:{1}", in_err, in_strOutputMessage));
            }
            
        }
        public static void PrintToOutput(string in_strOutputMessage, bool loggingEnabled)
        {
            PrintToOutput(in_strOutputMessage, 0, loggingEnabled);
        }

        //default of this method is to print to console and not log
        public static void PrintToOutput(string in_strOutputMessage)
        {
            PrintToOutput(in_strOutputMessage, true);
        }
        /// <summary>
        /// Prints text to console error
        /// </summary>
        /// <param name="in_strErrorMessage"> Text to output </param>
        public static void PrintToError(string in_strErrorMessage, bool loggingEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            //PrintToOutput(in_strErrorMessage, EventLogger.LOG_MESSAGE_LEVEL_ERROR_MIN, true, loggingEnabled);
            PrintToOutput(in_strErrorMessage, 1, loggingEnabled);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        public static void PrintToError(string in_strErrorMessage)
        {
            PrintToError(in_strErrorMessage, true);
        }

        /// <summary>
        /// appends to a file specified by in_filePath
        /// </summary>
        /// <param name="in_strMessage"></param>
        /// <param name="in_filePath"></param>
        public static void PrintToFile(string in_strMessage, string in_filePath)
        {
            System.IO.File.AppendAllText(in_filePath,in_strMessage);
            System.IO.File.AppendAllText(in_filePath, "\n\r");
     
       }
        
    }
}
