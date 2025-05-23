﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.Versioning;

namespace Microsoft.DotNet.Cli.Installer.Windows;

[SupportedOSPlatform("windows")]
internal class NativeMethods
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern uint FormatMessage(uint dwFlags, nint lpSource, uint dwMessageId, uint dwLanguageId, StringBuilder lpBuffer, uint nSize, nint Arguments);
}
