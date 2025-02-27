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
    using UnityEngine;
    using System.Threading.Tasks;

    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string modGUID = "zelofi.MorePlayers";
        public const string modName = "MorePlayers";
        public const string modVersion = "1.0.1";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ConfigEntry<int> configMaxPlayers;

        public static ManualLogSource mls;

        void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo($"{modGUID} is now awake!");

            configMaxPlayers = Config.Bind
            (
                "General", 
                "MaxPlayers", 
                10, 
                "The max amount of players allowed in a server"
            );

            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(NetworkConnect), "TryJoiningRoom")]
        public class TryJoiningRoomPatch
        {
            static bool Prefix(ref string ___RoomName)
            {
                if(string.IsNullOrEmpty(___RoomName))
                {
                    mls.LogError("RoomName is null or empty, using previous method!");
                    return true;
                }
                
                if(configMaxPlayers.Value == 0)
                {
                    mls.LogError("The MaxPlayers config is null or empty, using previous method!");
                    return true;
                }

                if(NetworkConnect.instance != null)
                {
                    PhotonNetwork.JoinOrCreateRoom(___RoomName, new RoomOptions
                    {
                        MaxPlayers = configMaxPlayers.Value
                    }, TypedLobby.Default, null);

                    return false;
                }
                else
                {
                    mls.LogError("NetworkConnect instance is null, using previous method!");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SteamManager), "HostLobby")]
        public class HostLobbyPatch
        {
            static async Task<bool> PrefixAsync()
            {
                Debug.Log("Steam: Hosting lobby...");
                Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync(configMaxPlayers.Value);

                if (!lobby.HasValue)
                {
                    Debug.LogError("Lobby created but not correctly instantiated.");
                    return false;
                }

                lobby.Value.SetPublic();
                lobby.Value.SetJoinable(b: false);

                return false;
            }
        }
    }
}
