﻿// -----------------------------------------------------------------------
// <copyright file="GameNetworkManager.cs" company="Lethal Company Modding Community">
// Copyright (c) Lethal Company Modding Community. All rights reserved.
// Licensed under the GPL-3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace LCAPI.Core.Patches;

using System.Linq;

using HarmonyLib;
using Steamworks;
using Steamworks.Data;

[HarmonyPatch(typeof(GameNetworkManager))]
internal static class GameNetworkManagerPatches
{

    [HarmonyPatch("SteamMatchmaking_OnLobbyCreated")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyWrapSafe]
    private static void SteamMatchmaking_OnLobbyCreated_Postfix(Result result, ref Lobby lobby)
    {
        // lobby has not yet created or something went wrong
        if (result != Result.OK)
        {
            return;
        }

        lobby.SetData("__modded_lobby", "true");
        lobby.SetData("__joinable", lobby.GetData("joinable")); // the actual joinable

        // if the user is forced to only allow modded user to join, joinable flag is set to prevent vanilla user to join
        if (Core._moddedOnly)
        {
            lobby.SetData("joinable", "false");
        }
    }

    [HarmonyPatch(nameof(GameNetworkManager.LobbyDataIsJoinable))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyWrapSafe]
    private static bool LobbyDataIsJoinable_Prefix(GameNetworkManager __instance, ref Lobby lobby, ref bool __result)
    {
        Plugin.Log.LogDebug(string.Format("Attempting to join lobby id: {0}", lobby.Id));
        var data = lobby.GetData("__modded_lobby"); // is modded lobby?
        if (Core._moddedOnly && data != "true")
        {
            Plugin.Log.LogDebug("Lobby join denied! Attempted to join non-modded lobby");
            UObject.FindObjectOfType<MenuManager>().SetLoadingScreen(false, RoomEnter.DoesntExist, "The server host is not a modded user");
            __result = false;
            return false;
        }
        data = lobby.GetData("vers"); // game version
        if (data != __instance.gameVersionNum.ToString())
        {
            Plugin.Log.LogDebug(string.Format("Lobby join denied! Attempted to join vers {0}", data, lobby.Id));
            UObject.FindObjectOfType<MenuManager>().SetLoadingScreen(false, RoomEnter.DoesntExist, string.Format("The server host is playing on version {0} while you are on version {1}.", data, GameNetworkManager.Instance.gameVersionNum));
            __result = false;
            return false;
        }

        var friendArr = SteamFriends.GetBlocked().ToArray<Friend>();
        if (friendArr != null && friendArr.Length > 0)
        {
            foreach (var friend in friendArr)
            {
                Plugin.Log.LogDebug(string.Format("Lobby join denied! Attempted to join a lobby owned by a user which you has blocked: name: {0} | id: {1}", friend.Name, friend.Id));
                if (lobby.IsOwnedBy(friend.Id))
                {
                    UObject.FindObjectOfType<MenuManager>().SetLoadingScreen(false, RoomEnter.DoesntExist, "You attempted to join a lobby owned by a user you blocked.");
                    __result = false;
                    return false;
                }
            }
        }

        data = lobby.GetData("__joinable"); // is lobby joinable?
        if (data == "false")
        {
            Plugin.Log.LogDebug("Lobby join denied! Host lobby is not joinable");
            UObject.FindObjectOfType<MenuManager>().SetLoadingScreen(false, RoomEnter.DoesntExist, "The server host has already landed their ship, or they are still loading in.");
            return false;
        }

        if (lobby.MemberCount >= 4 || lobby.MemberCount < 1)
        {
            Plugin.Log.LogDebug(string.Format("Lobby join denied! Too many members in lobby! {0}", lobby.Id));
            UObject.FindObjectOfType<MenuManager>().SetLoadingScreen(false, RoomEnter.Full, "The server is full!");
            __result = false;
            return false;
        }

        Plugin.Log.LogDebug(string.Format("Lobby join accepted! Lobby ID: {0}", lobby.Id));
        __result = true;
        return false;
    }
}