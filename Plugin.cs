namespace MorePlayers
{
    using BepInEx;
    using BepInEx.Configuration;
    using BepInEx.Logging;
    using HarmonyLib;
    using Photon.Pun;
    using Photon.Realtime;
    using Steamworks.Data;
    using Steamworks;

    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "zelofi.MorePlayers";
        private const string modName = "MorePlayers";
        private const string modVersion = "1.0.1";
        
        private static readonly Harmony harmony = new Harmony(modGUID);
        
        internal static ConfigEntry<int> configMaxPlayers;

        internal static ManualLogSource mls;
        
        private void Awake()
        {
            mls = Logger;
            
            configMaxPlayers = Config.Bind("General", "MaxPlayers", 10, new ConfigDescription("The max amount of players allowed in a server", new AcceptableValueRange<int>(1, 100)));

            harmony.PatchAll(typeof(Patches_Photon));
            harmony.PatchAll(typeof(Patches_Steam));

            mls.LogInfo("Patches Loaded");
        }
    }

    [HarmonyPatch]
    internal static class Patches_Photon
    {
        [HarmonyPatch(typeof(LoadBalancingClient), "OpCreateRoom")] // Host a public lobby (button in the server list)
        [HarmonyPrefix]
        private static void CreateRoom_Prefix(ref EnterRoomParams enterRoomParams)
        {
            if (enterRoomParams.RoomOptions?.MaxPlayers == 6)
            {
                enterRoomParams.RoomOptions.MaxPlayers = Plugin.configMaxPlayers.Value;
                Plugin.mls.LogInfo("Changed MaxPlayers for PhotonNetwork.CreateRoom");
            }
        }
        
        [HarmonyPatch(typeof(LoadBalancingClient), "OpJoinRandomOrCreateRoom")] // Host a public lobby (random matchmaking)
        [HarmonyPrefix]
        private static void JoinRandomOrCreateRoom_Prefix(ref OpJoinRandomRoomParams opJoinRandomRoomParams, ref EnterRoomParams createRoomParams)
        {
            // if (opJoinRandomRoomParams.ExpectedMaxPlayers == 6)
            // {
            //     opJoinRandomRoomParams.ExpectedMaxPlayers = (byte)Plugin.configMaxPlayers.Value;
            // }
            
            if (createRoomParams.RoomOptions?.MaxPlayers == 6)
            {
                createRoomParams.RoomOptions.MaxPlayers = Plugin.configMaxPlayers.Value;
                Plugin.mls.LogInfo("Changed MaxPlayers for PhotonNetwork.JoinRandomOrCreateRoom");
            }
        }
        
        [HarmonyPatch(typeof(LoadBalancingClient), "OpJoinOrCreateRoom")] // Host a private lobby
        [HarmonyPrefix]
        private static void JoinOrCreateRoom_Prefix(ref EnterRoomParams enterRoomParams)
        {
            if (enterRoomParams.RoomOptions?.MaxPlayers == 6)
            {
                enterRoomParams.RoomOptions.MaxPlayers = Plugin.configMaxPlayers.Value;
                Plugin.mls.LogInfo("Changed MaxPlayers for PhotonNetwork.JoinOrCreateRoom");
            }
        }
    }

    [HarmonyPatch]
    internal static class Patches_Steam
    {
        [HarmonyPatch(typeof(SteamManager), "OnLobbyCreated")] // Created a steam lobby
        [HarmonyPrefix]
        private static void OnLobbyCreated_Prefix(Result _result, ref Lobby _lobby)
        {
            if (_result == Result.OK && _lobby.MaxMembers == 6)
            {
                _lobby.MaxMembers = Plugin.configMaxPlayers.Value;
                Plugin.mls.LogInfo("Changed MaxPlayers for SteamManager.OnLobbyCreated");
            }
        }
    }
}
