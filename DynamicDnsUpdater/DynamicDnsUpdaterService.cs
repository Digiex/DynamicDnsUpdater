using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DynamicDnsUpdater
{
    public partial class DynamicDnsUpdaterService : ServiceBase
    {
        //Initialize the timer
        Timer timer = new Timer();
        public DynamicDnsUpdaterService()
        {
            InitializeComponent();
        }

        //This method is used to raise event during start of service
        protected override void OnStart(string[] args)
        {
            //add this line to text file during start of service
            WriteLog("Starting service");
            //handle Elapsed event
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            //This statement is used to set interval to 1 minute (= 60,000 milliseconds)
            timer.Interval = 10000;
            //enabling the timer
            timer.Enabled = true;
        }

        //This method is used to stop the service
        protected override void OnStop()
        {
            timer.Enabled = false;
            WriteLog("Stopping service");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteLog("OnElapsedTime");
        }

        private void WriteLog(string content)
        {
            content = "[" + DateTime.Now + "] " + content;
            Console.WriteLine(content);
#if DEBUG
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(content);
            }
#endif
            //set up a filestream
            FileStream fs = new FileStream(@"c:\DynDNSlog.txt", FileMode.OpenOrCreate, FileAccess.Write);
            //set up a streamwriter for adding text
            StreamWriter sw = new StreamWriter(fs);
            //find the end of the underlying filestream
            sw.BaseStream.Seek(0, SeekOrigin.End);
            //add the text
            sw.WriteLine(content);
            //add the text to the underlying filestream
            sw.Flush();
            //close the writer
            sw.Close();
        }
    }
}
