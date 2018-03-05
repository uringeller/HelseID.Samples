﻿using System;
using System.Reflection;
using Microsoft.Win32;

namespace HelseID.Clients.HIDEnabler.Utils
{

    /// <summary>
    /// Registers the custom URI-scheme used by HIDEnabler in the registry
    /// </summary>
    internal class RegistryConfig
    {
        public RegistryConfig(string uriScheme)
        {
            CustomUriScheme = uriScheme;
        }

        public void Configure()
        {
            if (NeedToAddKeys()) AddRegKeys();
        }

        private string CustomUriScheme { get; }

        private string CustomUriSchemeKeyPath => RootKeyPath + @"\" + CustomUriScheme;
        private string CustomUriSchemeKeyValueValue => "URL:" + CustomUriScheme;
        private string CommandKeyPath => CustomUriSchemeKeyPath + @"\shell\open\command";

        private const string RootKeyPath = @"Software\Classes";

        private const string CustomUriSchemeKeyValueName = "";

        private const string ShellKeyName = "shell";
        private const string OpenKeyName = "open";
        private const string CommandKeyName = "command";

        private const string CommandKeyValueName = "";
        private const string CommandKeyValueFormat = "\"{0}\" \"%1\"";
        private static string CommandKeyValueValue { get; } = String.Format(CommandKeyValueFormat, Assembly.GetExecutingAssembly().Location);

        private const string UrlProtocolValueName = "URL Protocol";
        private const string UrlProtocolValueValue = "";

        public bool NeedToAddKeys()
        {
            var addKeys = false;

            using (var commandKey = Registry.CurrentUser.OpenSubKey(CommandKeyPath))
            {
                var commandValue = commandKey?.GetValue(CommandKeyValueName);
                addKeys |= !CommandKeyValueValue.Equals(commandValue);
            }

            using (var customUriSchemeKey = Registry.CurrentUser.OpenSubKey(CustomUriSchemeKeyPath))
            {
                var uriValue = customUriSchemeKey?.GetValue(CustomUriSchemeKeyValueName);
                var protocolValue = customUriSchemeKey?.GetValue(UrlProtocolValueName);

                addKeys |= !CustomUriSchemeKeyValueValue.Equals(uriValue);
                addKeys |= !UrlProtocolValueValue.Equals(protocolValue);
            }

            return addKeys;
        }

        private void AddRegKeys()
        {
            using (var classesKey = Registry.CurrentUser.OpenSubKey(RootKeyPath, true))
            {
                if (classesKey == null)
                {
                    return;
                }

                using (var root = classesKey.OpenSubKey(CustomUriScheme, true) ??
                                  classesKey.CreateSubKey(CustomUriScheme, true))
                {
                    root.SetValue(CustomUriSchemeKeyValueName, CustomUriSchemeKeyValueValue);
                    root.SetValue(UrlProtocolValueName, UrlProtocolValueValue);

                    using (var shell = root.OpenSubKey(ShellKeyName, true) ??
                                       root.CreateSubKey(ShellKeyName, true))
                    {
                        using (var open = shell.OpenSubKey(OpenKeyName, true) ??
                                          shell.CreateSubKey(OpenKeyName, true))
                        {
                            using (var command = open.OpenSubKey(CommandKeyName, true) ??
                                                 open.CreateSubKey(CommandKeyName, true))
                            {
                                command.SetValue(CommandKeyValueName, CommandKeyValueValue);
                            }
                        }
                    }
                }
            }
        }
    }
}