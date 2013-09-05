using DynamicDnsUpdater;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicDnsUpdaterConfig
{
    public partial class MainForm : Form
    {
        public const string regpath = @"Software\Digiex\DynDNSUpdater";
        public MainForm()
        {
            InitializeComponent();
            using (RegistryKey regkey = Registry.LocalMachine.OpenSubKey(regpath, false))
            {
                try
                {
                    intervalBox.Text = regkey.GetValue("UpdateIntervalMins", intervalBox.Text).ToString();
                    logPathBox.Text = regkey.GetValue("LogFile", logPathBox.Text).ToString();
                    using (var hostskey = regkey.OpenSubKey("Hosts"))
                    {
                        foreach (var host in hostskey.GetSubKeyNames())
                        {
                            using (var hostkey = hostskey.OpenSubKey(host))
                            {
                                dynDnsListBox.Items.Add(new DynDnsHost()
                                {
                                    Hostname = hostkey.GetValue("Hostname", host).ToString(),
                                    UpdateUrl = hostkey.GetValue("UpdateUrl").ToString(),
                                    Username = hostkey.GetValue("Username").ToString(),
                                    Password = Encoding.UTF8.GetString((byte[])hostkey.GetValue("Password")),
                                });
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void intervalBox_TextChanged(object sender, EventArgs e)
        {
            intervalBox.Text = Regex.Replace(intervalBox.Text, @"[^\d]", "");
            try
            {
                TimeSpan t = TimeSpan.FromMinutes(Double.Parse(intervalBox.Text));

                intervalReadable.Text = string.Format("{0:D2}d {1:D2}h:{2:D2}min", t.Days,
                                t.Hours,
                                t.Minutes);
            }
            catch
            {
                intervalBox.Text = "";
                intervalReadable.Text = "00d 00h:00min";
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            if (hostnameBox.Text.Trim().Length == 0 || updateUrlBox.Text.Trim().Length == 0
                || usernameBox.Text.Trim().Length == 0 || passwordBox.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Please fill all fields", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DynDnsHost host = null;
            foreach (var h in dynDnsListBox.Items)
            {
                if (((DynDnsHost)h).Hostname == hostnameBox.Text)
                {
                    host = (DynDnsHost)h;
                }
            }
            if (host != null)
            {
                dynDnsListBox.Items.Remove(host);
            }
            host = new DynDnsHost()
            {
                Hostname = hostnameBox.Text,
                UpdateUrl = updateUrlBox.Text,
                Username = usernameBox.Text,
                Password = passwordBox.Text
            };
            dynDnsListBox.Items.Add(host);
        }

        private void dynDnsListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (dynDnsListBox.SelectedItem == null)
            {
                return;
            }
            DynDnsHost host = (DynDnsHost)dynDnsListBox.SelectedItem;
            hostnameBox.Text = host.Hostname;
            updateUrlBox.Text = host.UpdateUrl;
            usernameBox.Text = host.Username;
            passwordBox.Text = host.Password;
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            dynDnsListBox.Items.Remove(dynDnsListBox.SelectedItem);
            hostnameBox.Text = "";
            updateUrlBox.Text = "";
            usernameBox.Text = "";
            passwordBox.Text = "";
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = logPathBox.Text;
            saveFileDialog1.ShowDialog();
            logPathBox.Text = saveFileDialog1.FileName;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey regkey = Registry.LocalMachine.CreateSubKey(regpath))
                {
                    regkey.SetValue("UpdateIntervalMins", Double.Parse(intervalBox.Text));
                    regkey.SetValue("LogFile", logPathBox.Text);
                    using (var hostskey = regkey.CreateSubKey("Hosts"))
                    {
                        foreach (var h in dynDnsListBox.Items)
                        {
                            var host = (DynDnsHost)h;
                            using (var hostkey = hostskey.CreateSubKey(host.Hostname))
                            {
                                hostkey.SetValue("Hostname", host.Hostname);
                                hostkey.SetValue("UpdateUrl", host.UpdateUrl);
                                hostkey.SetValue("Username", host.Username);
                                hostkey.SetValue("Password", Encoding.UTF8.GetBytes(host.Password));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
