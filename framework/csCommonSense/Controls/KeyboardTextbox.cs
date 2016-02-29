using System.Web.UI.WebControls;
using System.Windows.Documents;
using csShared.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace csShared.Controls
{
    public class KeyboardTextBox : WatermarkTextBox
    {
        protected override void OnPreviewTouchDown(TouchEventArgs e)
        {
            base.OnPreviewTouchDown(e);
            //if (OSInfo.MajorVersion != 6 || OSInfo.MinorVersion < 2) return;
            const string f = @"C:\Program Files\Common Files\Microsoft Shared\ink\TabTip.exe";
            if (File.Exists(f))
                Process.Start(f);
            //else
            //{
            //    //StartOsk();
            //    //Process.Start("OSK");
            //}
            this.Focus();
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        public void StartOsk()
        {
            var ptr = new IntPtr();
            var sucessfullyDisabledWow64Redirect = false;

            // Disable x64 directory virtualization if we're on x64,
            // otherwise keyboard launch will fail.
            if (Environment.Is64BitOperatingSystem)
            {
                sucessfullyDisabledWow64Redirect =
                    Wow64DisableWow64FsRedirection(ref ptr);
            }


            var psi = new ProcessStartInfo { FileName = "osk", UseShellExecute = true };
            // We must use ShellExecute to start osk from the current thread
            // with psi.UseShellExecute = false the CreateProcessWithLogon API 
            // would be used which handles process creation on a separate thread 
            // where the above call to Wow64DisableWow64FsRedirection would not 
            // have any effect.
            //
            Process.Start(psi);

            // Re-enable directory virtualisation if it was disabled.
            if (!Environment.Is64BitOperatingSystem) return;
            if (sucessfullyDisabledWow64Redirect)
                Wow64RevertWow64FsRedirection(ptr);
        }

        protected override void OnGotFocus(System.Windows.RoutedEventArgs e)
        {

            base.OnGotFocus(e);

            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Keyboard");
            if (searcher.Get().Count == 0)
            {



            }

        }
    }
}
