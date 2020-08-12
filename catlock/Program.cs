using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace catlock {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            bool main;

            using (var appMutex = new Mutex(true, "catlock", out main)) {
                if (main) {
                    var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                        .IsInRole(WindowsBuiltInRole.Administrator);

                    if (!isAdmin) {
                        try {
                            appMutex.Dispose();

                            Process.Start(new ProcessStartInfo {
                                FileName = Assembly.GetExecutingAssembly().CodeBase,
                                Verb = "runas"
                            });
                        }
                        catch (Win32Exception) { }

                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    var form = new MainForm();
                    var cts = new CancellationTokenSource();
                    var task = Task.Factory.StartNew(() => {
                        while (!cts.Token.IsCancellationRequested) {
                            var ps = new PipeSecurity();
                            ps.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
                            var server = new NamedPipeServerStream("catlock", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 512, 512, ps);

                            using (var commandProcessed = new ManualResetEvent(false)) {
                                server.BeginWaitForConnection(ar => {
                                    if (!server.CanRead) return;
                                    server.EndWaitForConnection(ar);

                                    using (var sr = new StreamReader(server)) {
                                        var cmd = sr.ReadLine();

                                        if (cmd == "block") {
                                            form.Invoke((Action)(() => form.Block()));
                                        }
                                    }

                                    commandProcessed.Set();
                                }, null);

                                WaitHandle.WaitAny(new WaitHandle[] { commandProcessed, cts.Token.WaitHandle });
                            }
                        }
                            
                    });

                    Application.Run(form);

                    cts.Cancel();
                    task.Wait();
                }
                else {
                    using (var client = new NamedPipeClientStream(".", "catlock", PipeDirection.Out, PipeOptions.Asynchronous)) { 
                        client.Connect();

                        using (var sw = new StreamWriter(client)) {
                            sw.WriteLine("block");
                            sw.Flush();
                            client.WaitForPipeDrain();
                        }
                    }
                }
            }
        }
    }
}
