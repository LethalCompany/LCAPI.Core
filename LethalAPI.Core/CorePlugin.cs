﻿// -----------------------------------------------------------------------
// <copyright file="CorePlugin.cs" company="LethalAPI Modding Community">
// Copyright (c) LethalAPI Modding Community. All rights reserved.
// Licensed under the GPL-3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace LethalAPI.Core;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable SA1401 // field should be made private
#pragma warning disable SA1309 // Names should not start with an underscore. ie: _Logger.
using System;
using System.IO;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MEC;

/// <inheritdoc />
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CorePlugin : BaseUnityPlugin
{
    /// <summary>
    /// Gets the instance for the main api.
    /// </summary>
    public static CorePlugin Instance = null!;

    /// <summary>
    /// Gets the <see cref="BepInEx.Logging.Logger"/>.
    /// </summary>
    /// <summary>
    /// The base logger.
    /// </summary>
    internal static ManualLogSource _Logger = null!;

    /// <summary>
    /// The harmony instance.
    /// </summary>
    internal static Harmony Harmony = null!;

    private void Awake()
    {
        _Logger = this.Logger;
        Harmony = new(PluginInfo.PLUGIN_GUID);

        // Events.Events contains the instance. This should become a plugin for loading and config purposes, in the future.
        // Events..cctor -> Patcher.PatchAll will do the patching. This is necessary for dynamic patching.
        _ = new Events.Events();
        Instance = this;

        Loader.PluginDirectory = Paths.PluginPath;
        Loader.DependencyDirectory = Path.GetFullPath(Path.Combine(Paths.PluginPath, "../", "Dependencies"));
        Loader.ConfigDirectory = Paths.ConfigPath;
        _ = new Loader();

        Events.Handlers.Server.GameOpened += InitTimings;
        Log.Info($"{PluginInfo.PLUGIN_GUID} is being loaded...");
    }

    private void InitTimings()
    {
        Timing.Instance.name = "Timing Controller";
        Timing.Instance.OnException += OnError;
    }

    // ReSharper disable once ParameterHidesMember
    private void OnError(Exception exception, string tag)
    {
        Log.Error($"Timings has caught an error during the execution of a coroutine{(tag == "Unknown" ? string.Empty : $" [{tag}]")}. Exception: \n" + exception.Message, "MEC Timings");
    }
}