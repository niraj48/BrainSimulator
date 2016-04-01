﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using GoodAI.BrainSimulator.Forms;
using GoodAI.Platform.Core.Configuration;
using GoodAI.TypeMapping;

namespace GoodAI.BrainSimulator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // See: http://stackoverflow.com/questions/5762526/how-can-i-make-something-that-catches-all-unhandled-exceptions-in-a-winforms-a
            if (!Debugger.IsAttached)
            {
                Application.ThreadException += new ThreadExceptionEventHandler(ProcessThreadException);

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ProcessUnhandledException);
            }

            ConfigureTypeMap();

            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }

        private static void ConfigureTypeMap()
        {
            TypeMap.InitializeConfiguration<CoreContainerConfiguration>();
            TypeMap.Verify();
        }

        static void ProcessThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowExceptionAndExit("Unhandled Thread Exception", e.Exception);
        }

        static void ProcessUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowExceptionAndExit("Unhandled UI Exception", e.ExceptionObject as Exception);
        }

        static void ShowExceptionAndExit(string title, Exception ex)
        {
            if (ex != null)
            {
                MessageBox.Show("Unhandled exception encountered, sorry:-("
                    + "\n\nMessage:\n" + ex.Message
                    + "\n\nStack trace:\n" + ex.StackTrace, title,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Unhandled exception encountered (but it's null!).",
                    title, MessageBoxButtons.OK);
            }

            Environment.Exit(1);
        }
    }
}
    