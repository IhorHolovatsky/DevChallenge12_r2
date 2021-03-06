﻿using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CssOptimizer.Services.Utils
{
    internal static class ChromeUtils
    {
        /// <summary>
        /// Get path to chrome.exe file
        /// </summary>
        /// <returns></returns>
        public static string GetChromePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //take path to chrome via registry key
                return Microsoft.Win32.Registry
                    .LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe")
                    ?.GetValue("").ToString();

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "google-chrome";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            }
            else
            {
                throw new InvalidOperationException("Unknown or unsupported platform.");
            }
        }

        public static string CreateTempFolder()
        {
            string path = Path.GetRandomFileName();
            return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path)).FullName;
        }

    }
}
