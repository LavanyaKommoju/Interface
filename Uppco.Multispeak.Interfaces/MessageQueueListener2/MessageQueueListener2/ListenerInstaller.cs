using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace MessageQueueListener
{
    [RunInstaller(true)]
    public partial class ListenerInstaller : System.Configuration.Install.Installer
    {
        public ListenerInstaller()
        {
            //InitializeComponent();
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            SetServicePropertiesFromCommandLine(serviceInstaller);

            //# Service Information
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }

        private void SetServicePropertiesFromCommandLine(ServiceInstaller serviceInstaller)
        {
            string[] commandlineArgs = Environment.GetCommandLineArgs();

            string servicename;
            string servicedisplayname;
            ParseServiceNameSwitches(commandlineArgs, out servicename, out servicedisplayname);

            serviceInstaller.ServiceName = servicename;
            serviceInstaller.DisplayName = servicedisplayname;
        }

        private void ParseServiceNameSwitches(string[] commandlineArgs, out string serviceName, out string serviceDisplayName)
        {
            string servicenameswitch = string.Empty;
            string servicedisplaynameswitch = string.Empty;
            foreach (string s in commandlineArgs)
            {
                if (s.StartsWith("/servicename=", StringComparison.CurrentCultureIgnoreCase))
                    servicenameswitch = s.Substring(13);
                else if (s.StartsWith("/servicedisplayname=", StringComparison.CurrentCultureIgnoreCase))
                    servicedisplaynameswitch = s.Substring(20);
            }

            if (servicedisplaynameswitch.Length > 0)
                serviceDisplayName = servicedisplaynameswitch.Trim('"');
            else
                serviceDisplayName = "MQ Listener";

            if (servicenameswitch.Length > 0)
                serviceName = servicenameswitch.Trim('"');
            else
                serviceName = "MQ Listener";
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
