using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HkvsRecordDemo
{
    public class LogHandler
    {
        public static string logFileName = "";
        public static string logFilePath = "";

        public static void CreateLogFile(string fileName)
        {
            logFileName = fileName;
            try
            {
                //如不存在log文件夹，先创建log文件夹
                if (!Directory.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "Log\\"))
                {
                    Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + "Log\\");
                }
                logFilePath = System.AppDomain.CurrentDomain.BaseDirectory + "Log\\" + logFileName;
                if (!File.Exists(logFilePath))
                {
                    FileStream fs = File.Create(logFilePath);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void WriteLog(string log)
        {
            try
            {
                System.IO.File.AppendAllText(logFilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + log + "\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
