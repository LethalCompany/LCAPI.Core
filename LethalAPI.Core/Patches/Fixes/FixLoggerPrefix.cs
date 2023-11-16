﻿// -----------------------------------------------------------------------
// <copyright file="FixLoggerPrefix.cs" company="LethalAPI Modding Community">
// Copyright (c) LethalAPI Modding Community. All rights reserved.
// Licensed under the GPL-3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace LethalAPI.Core.Patches.Fixes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using BepInEx.Logging;

#pragma warning disable SA1402

/// <summary>
/// Patches the <see cref="BepInEx.Logging.ConsoleLogListener.LogEvent">BepInEx.Logging.ConsoleLogListener.LogEvent</see> method to utilize a custom logger.
/// </summary>
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
#pragma warning disable SA1313
[HarmonyPatch(typeof(ConsoleLogListener), nameof(ConsoleLogListener.LogEvent))]
internal static class FixLoggerPrefix
{
    private static readonly Dictionary<char, ConsoleColor> ConsoleText = new ()
    {
        { '0', ConsoleColor.Black }, // black
        { '1', ConsoleColor.Red }, // red
        { '2', ConsoleColor.Green }, // green
        { '3', ConsoleColor.Yellow }, // yellow
        { '4', ConsoleColor.Blue }, // blue
        { '5', ConsoleColor.Magenta }, // purple
        { '6', ConsoleColor.Cyan }, // cyan
        { '7', ConsoleColor.White }, // white
        { 'a', ConsoleColor.DarkGray }, // dark gray
        { 'b', ConsoleColor.DarkRed }, // dark red
        { 'c', ConsoleColor.DarkGreen }, // dark green
        { 'd', ConsoleColor.DarkYellow }, // dark yellow
        { 'e', ConsoleColor.DarkBlue }, // dark blue
        { 'f', ConsoleColor.DarkMagenta }, // dark magenta
        { 'g', ConsoleColor.DarkCyan }, // dark cyan
        { 'h', ConsoleColor.Gray }, // gray
    };

    [HarmonyPrefix]
    private static bool Prefix(ConsoleLogListener __instance, object sender, LogEventArgs eventArgs)
    {
        // Redirect BepInEx logs
        if ((int)eventArgs.Level != 62)
        {
            Log.Skip(eventArgs.Data.ToString(), eventArgs.Level);
            return false;
        }

        // Get variables and default output.
        Type consoleManager = AccessTools.TypeByName("BepInEx.ConsoleManager");
        MethodInfo setConsoleColor = AccessTools.Method(consoleManager, "SetConsoleColor", new[] { typeof(ConsoleColor) });
        TextWriter instance = (TextWriter)AccessTools.Property(consoleManager, "ConsoleStream")?.GetMethod.Invoke(null, null)!;

        /* BepInEx.ConsoleManager.SetConsoleColor(eventArgs.Level.GetConsoleColor());
           BepInEx.ConsoleStream?.Write((string)eventArgs.Data);
           BepInEx.SetConsoleColor(ConsoleColor.Gray);*/

        // Set default color
        setConsoleColor.Invoke(null, new object[] { ConsoleColor.Gray });
        bool flag = false;
        string text = (string)eventArgs.Data;

        // Process the characters
        // ReSharper disable once ForCanBeConvertedToForeach
        // In case we want to ensure it isn't \& in the future, we will need a for loop.
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '&' && !flag)
            {
                flag = true;
            }
            else if (flag)
            {
                if (!ConsoleText.ContainsKey(text[i]))
                {
                    flag = false;
                    continue;
                }

                setConsoleColor.Invoke(null, new object[] { ConsoleText[text[i]] });
                flag = false;
            }
            else
            {
                instance.Write(text[i]);
            }
        }

        instance.Write('\n');

        // revert default color.
        // instance!.Write((string)eventArgs.Data);
        setConsoleColor.Invoke(null, new object[] { ConsoleColor.Gray });
        return false;
    }
}

/// <summary>
/// Patches the <see cref="BepInEx.Logging.DiskLogListener.LogEvent">BepInEx.Logging.DiskLogListener.LogEvent</see> with a custom implementation.
/// </summary>
[HarmonyPatch(typeof(DiskLogListener), nameof(DiskLogListener.LogEvent))]
internal static class PatchLogFilePrefix
{
    [HarmonyPrefix]
    private static bool Prefix(DiskLogListener __instance, object sender, LogEventArgs eventArgs)
    {
        // Skip default implementation. The other prefix will re-route it through the custom implementation anyways.
        if ((int)eventArgs.Level != 62)
        {
            return false;
        }

        string text = (string)eventArgs.Data;
        bool flag = false;

        // ReSharper disable once ForCanBeConvertedToForeach
        // In case we want to check for a \& in the future.
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '&')
            {
                flag = true;
                continue;
            }

            if (flag)
            {
                flag = false;
                continue;
            }

            __instance.LogWriter.Write(text[i]);
        }

        __instance.LogWriter.Write('\n');
        return false;
    }
}