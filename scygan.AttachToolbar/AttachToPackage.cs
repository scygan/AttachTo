using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;


namespace scygan.AttachToolbar {
    //// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    //// This attribute is used to register the informations needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    //// This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidAttachToPkgString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    public sealed class AttachToPackage : Package {
        protected override void Initialize() {
            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;


            //Attach button
            {
                OleMenuCommand attachCommand = new OleMenuCommand(new EventHandler(OnAttach), new CommandID(GuidList.guidAttachToCmdSet, (int)PkgCmdIDList.cmdidAttach));
                mcs.AddCommand(attachCommand);
            }

            //"Transport" combobox
            {
                OleMenuCommand comboCommand =
                    new OleMenuCommand(new EventHandler(OnTransportCombo), new CommandID(GuidList.guidAttachToCmdSet,
                    (int)PkgCmdIDList.cmdidTransportCombo));
                comboCommand.ParametersDescription = "$";
                mcs.AddCommand(comboCommand);

                MenuCommand comboGetListCommand =
                    new OleMenuCommand(new EventHandler(OnTransportComboGetList),
                    new CommandID(GuidList.guidAttachToCmdSet, (int)PkgCmdIDList.cmdidTransportComboGetList));
                mcs.AddCommand(comboGetListCommand);
            }

            //"Qualified combobox"
            {
                OleMenuCommand comboCommand =
                    new OleMenuCommand(new EventHandler(OnQualifierCombo), new CommandID(GuidList.guidAttachToCmdSet,
                    (int)PkgCmdIDList.cmdidQualifierCombo));
                comboCommand.ParametersDescription = "$";
                mcs.AddCommand(comboCommand);
            }


            //initialize some locals
            {
                DTE dte = (DTE)this.GetService(typeof(DTE));
                Debugger2 debugger = dte.Debugger as Debugger2;

                foreach (Transport t in debugger.Transports) {
                    m_Transports.Add(t);
                    if (m_Transport == null || t.Name.StartsWith("Default")) {
                        m_Transport = t;
                    }
                }
            }
        }

        private void OnAttach(object sender, EventArgs e) {
            try {
                DTE dte = (DTE)this.GetService(typeof(DTE));
                Debugger2 debugger = dte.Debugger as Debugger2;
                Processes processes = debugger.GetProcesses(m_Transport, m_Qualifier);

                Process process = null;

                try {
                    process = processes.Item("notepad.exe");
                } catch (System.ArgumentException) {
                    throw (new System.Exception(Resources.ProcessNameNotFound));
                }

                process.Attach();

            } catch (System.Exception ex) {
                CallMessageBox(ex.Message);
            }
        }


        private List<Transport> m_Transports = new List<Transport>();
        Transport m_Transport = null;
        string m_Qualifier = "";

        private void OnTransportCombo(object sender, EventArgs e) {
            if (e == EventArgs.Empty) {
                throw (new ArgumentException(Resources.EventArgsRequired));
            }
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            
            if (eventArgs == null) {
                throw (new ArgumentException(Resources.EventArgsRequired));
            }

            if (eventArgs.InValue == null && eventArgs.OutValue != IntPtr.Zero) {
                throw (new ArgumentException(Resources.ParamNull));
            }

            if (eventArgs.InValue != null) {

                string newChoice = eventArgs.InValue as string;
                if (newChoice == null) {
                    throw (new ArgumentException(Resources.ParamIllegal));
                }

                Transport newTransport = null;
                foreach (Transport t in m_Transports) {
                    if (t.Name.Equals(newChoice)) {
                        newTransport = t;
                        break;
                    }
                }

                if (newTransport == null) {
                    throw (new ArgumentNullException(Resources.ParamNotValidStringInList));
                }

                if (m_Transport != newTransport) {
                    //TODO
                    m_Transport = newTransport;
                }
            }

            if (eventArgs.OutValue != IntPtr.Zero) {
                Marshal.GetNativeVariantForObject(m_Transport.Name, eventArgs.OutValue);
            }
        }

        private void OnQualifierCombo(object sender, EventArgs e) {
            if (e == EventArgs.Empty) {
                throw (new ArgumentException(Resources.EventArgsRequired));
            }
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs == null) {
                throw (new ArgumentException(Resources.EventArgsRequired));
            }

            if (eventArgs.OutValue == null && eventArgs.OutValue == null) {
                throw (new ArgumentException(Resources.ParamNull));
            }

            if (eventArgs.InValue != null) {

                string newChoice = eventArgs.InValue as string;
                if (newChoice == null) {
                    throw (new ArgumentException(Resources.ParamIllegal));
                }

                m_Qualifier = newChoice;
            }

            if (eventArgs.OutValue != IntPtr.Zero) {
                Marshal.GetNativeVariantForObject(m_Transport.Name, eventArgs.OutValue);
            }
        }

        private void OnTransportComboGetList(object sender, EventArgs e) {
            if ((null == e) || (e == EventArgs.Empty)) {
                // --- We should never get here; EventArgs are required.
                throw (new ArgumentNullException(Resources.EventArgsRequired));
            }
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            
            if (eventArgs == null) {
                throw (new ArgumentException(Resources.EventArgsRequired));
            }

            if (eventArgs.InValue != null) {
                throw (new ArgumentException(Resources.ParamNull));
            } 
            
            if (eventArgs.OutValue != IntPtr.Zero) {

                List<string> transportNames = new List<String>();
                foreach (Transport t in m_Transports) 
                {
                    transportNames.Add(t.Name);
                }
                Marshal.GetNativeVariantForObject(transportNames.ToArray(), eventArgs.OutValue);
            }
        }

        private void CallMessageBox(string message) {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            uiShell.ShowMessageBox(0, ref clsid, "SimpleCommand", message, string.Empty, 0, 
                OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO,
                0, out result);
        }
    }
}
