using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace lwshostsvc
{
    [RunInstaller(true)]
    public partial class HostServiceInstaller : System.Configuration.Install.Installer
    {
        public HostServiceInstaller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Installs or uninstalls the lwshost service
        /// 
        /// Source: https://groups.google.com/forum/#!topic/microsoft.public.dotnet.languages.csharp/TUXp6lRxy6Q
        /// </summary>
        /// <param name="undo">revert installation</param>
        public static EHostServiceInstallState Install(bool undo = false)
        {
            using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, new string[0]))
            {
                IDictionary state = new Hashtable();
                inst.UseNewContext = true;

                try
                {
                    if (undo)
                    {
                        inst.Uninstall(state);
                        return EHostServiceInstallState.UninstalCompleted;
                    }
                    else
                    {
                        inst.Install(state);
                        inst.Commit(state);
                        return EHostServiceInstallState.InstallAndCommitCompleted;
                    }
                }
                catch
                {
                    try
                    {
                        inst.Rollback(state);
                        return EHostServiceInstallState.RollbackCompleted;
                    }
                    catch
                    {
                        return EHostServiceInstallState.RollbackFailed;
                    }
                }
            }
        }

        public enum EHostServiceInstallState : byte
        {
            InstallationFailed,
            UninstalCompleted,
            InstallAndCommitCompleted,
            RollbackCompleted,
            RollbackFailed
        }
    }
}
