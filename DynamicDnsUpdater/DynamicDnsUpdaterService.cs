using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            if (!EventLog.SourceExists(this.eventLog.Source))
            {
                EventLog.CreateEventSource(this.eventLog.Source, this.eventLog.Log);
            }
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();
        }

        public List<DynDnsHost> Hosts = new List<DynDnsHost>();
        public int UpdateIntervalMins = 1440; //Defaulting to update every 24h
        public string LogFile = "DynDNSlog.txt";

        //This method is used to raise event during start of service
        protected override void OnStart(string[] args)
        {
            //add this line to text file during start of service
            WriteLog("Starting service");

            string lsbkey = @"Software\Digiex\DynDNSUpdater";
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(lsbkey, false);

            try
            {
                var hostskey = regkey.OpenSubKey("hosts");
                foreach (var host in hostskey.GetSubKeyNames())
                {
                    var hostkey = hostskey.OpenSubKey(host);
                    Hosts.Add(new DynDnsHost()
                    {
                        Hostname = hostkey.GetValue("Hostname", host).ToString(),
                        UpdateUrl = hostkey.GetValue("UpdateUrl").ToString(),
                        Username = hostkey.GetValue("Username").ToString(),
                        Password = hostkey.GetValue("Password").ToString(),
                    });
                }
                UpdateIntervalMins = int.Parse(regkey.GetValue("UpdateIntervalMins", UpdateIntervalMins).ToString());
                LogFile = regkey.GetValue("LogFile", LogFile).ToString();
            }
            catch (NullReferenceException ne)
            {
                WriteLog(@"Aborting Service, Configuration not stored properly in registry : " + ne.Message);
                this.Stop();
                return;
            }

            //handle Elapsed event
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            //This statement is used to set interval to 1 minute (= 60,000 milliseconds)
            timer.Interval = UpdateIntervalMins;
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
            foreach (var host in Hosts)
            {
                WriteLog("Updating DynDNS host " + host.Hostname);
                WebClient wc = new WebClient();
                wc.Credentials = new NetworkCredential(host.Username, host.Password);
                string result = wc.DownloadString(string.Format(host.UpdateUrl, host.Hostname, host.Username)).Trim().ToLower();
                if (result.StartsWith("badauth"))
                {
                    WriteLog("Wrong username and/or password!", EventLogEntryType.Error);
                }
                else if (result.StartsWith("nohost"))
                {
                    WriteLog("Requested hostname does not belong to the user.", EventLogEntryType.Error);
                }
                else if (result.StartsWith("nochg"))
                {
                    WriteLog("IP already up-to-date");
                }
                else if (result.StartsWith("good"))
                {
                    WriteLog("IP updated. Response: " + result);
                }
                else if (result.StartsWith("dnserr"))
                {
                    WriteLog("DNS Error at server!", EventLogEntryType.Error);
                }
                else if (result.StartsWith("abuse"))
                {
                    WriteLog("Account has been marked as abuse!", EventLogEntryType.Error);
                }
                else
                {
                    WriteLog("Got unknown response from DynDNS provider: " + result, EventLogEntryType.Warning);
                }
            }
        }

        private void WriteLog(string content, EventLogEntryType level = EventLogEntryType.Information)
        {
            eventLog.WriteEntry(content, level);
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(level.ToString().ToUpper());
            sb.Append("]");
            sb.Append("[");
            sb.Append(DateTime.Now);
            sb.Append("] ");
            sb.Append(content);
            content = sb.ToString();
            Console.WriteLine(content);
#if DEBUG
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(content);
            }
#endif
            if (LogFile == "")
            {
                return;
            }
            try
            {
                //set up a filestream
                using (FileStream fs = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    //set up a streamwriter for adding text
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
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
            catch { }
        }
    }
}
