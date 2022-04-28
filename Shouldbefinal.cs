using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;
// TODO: implement finished and starting matches

public class Lobby : NetworkManager
{

    public static Lobby lobby { get; private set; }

    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    private static string LobbyMetadata, AccessMod, searchString;
    public static List<LobbyRenderer> lobbyRenderers;
    private static List<CSteamID> _steamIDs;
    private static string seperator = ",";
    private static string pub = "0";

    protected Callback<LobbyCreated_t> lobbyCreated;
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.Log("not okay");
            return;
        }

        lobby.StartHost();

        var steamID = new CSteamID(callback.m_ulSteamIDLobby);
        /// we keep getting an error if we setup more than one lobbydata
        /// so we have to concat everything we do into this one string
        /// 
        /// index 0 is steam, index 1 is lobbycode, index 2 is accessmodifier
        string HostAddress = SteamUser.GetSteamID().ToString() + seperator;
        string _LobbyCode = HelperFuncs.GenLobbyCode() + seperator;
        string accessmodifier = AccessMod/*dont need a + seperator unless there is more to string*/;
        string _lobbyMetadata = HostAddress + _LobbyCode + accessmodifier;

        SteamMatchmaking.SetLobbyData(
            steamID,
            LobbyMetadata,
            _lobbyMetadata
        );
        
    }
    protected Callback<LobbyEnter_t> lobbyEntered;

    private void OnLobbyEntered(LobbyEnter_t callback)
    {

        if (NetworkServer.active) return; // don't do anything if we host

        
        var idk = new CSteamID(callback.m_ulSteamIDLobby);

        LobbyMetadata = SteamMatchmaking.GetLobbyData(
            idk,
            LobbyMetadata
        );

        var lobbyMetadata = LobbyMetadata.Split(seperator);

        string HostAddress = lobbyMetadata[0];
        Debug.Log(lobbyMetadata[1]);

        
        lobby.networkAddress = HostAddress;
        lobby.StartClient();
    }

    protected Callback<LobbyMatchList_t> c_lobbyList;

    private void OnGetLobbies(LobbyMatchList_t callback)
    {
        Debug.Log("Found" + callback.m_nLobbiesMatching + " lobbies");
        // for some reason unity doesnt like lists???
        if(_steamIDs.Count > 0)_steamIDs.Clear();
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            // since SteamMatchmaking doesnt actually return a list of _steamIDs
            // we need to get by index for each one that exists
            CSteamID steamID = SteamMatchmaking.GetLobbyByIndex(i);
            _steamIDs.Add(steamID);
            SteamMatchmaking.RequestLobbyData(steamID);
        }
    }


    protected Callback<LobbyDataUpdate_t> c_lobbyData;
    private void OnGetLobbyInfo(LobbyDataUpdate_t callback)
    {
        for (int i = 0; i < _steamIDs.Count; i++)
        {
            if(_steamIDs[i].m_SteamID == callback.m_ulSteamIDLobby)
            {
                LobbyRenderer lobbyRenderer;
                var lobbyID = new CSteamID(_steamIDs[i].m_SteamID);
                string _lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, LobbyMetadata);
                var _lobbyDataList = _lobbyData.Split(seperator);
                

                if(string.IsNullOrEmpty(searchString))
                {
                    if(_lobbyDataList[0] == searchString)
                    {
                        lobbyRenderer.steamID = _lobbyDataList[0];
                        lobbyRenderer._CSteamID = lobbyID;
                        lobbyRenderer.ActivePlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                        lobbyRenderer.MaxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
                        lobbyRenderer.lobbyCode = _lobbyDataList[1];
                        lobbyRenderers.Add(lobbyRenderer);
                    }
                }

                else
                {
                    if(_lobbyDataList[2] == pub)
                    {
                        lobbyRenderer.steamID = _lobbyDataList[0];
                        lobbyRenderer._CSteamID = lobbyID;
                        lobbyRenderer.ActivePlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                        lobbyRenderer.MaxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
                        lobbyRenderer.lobbyCode = _lobbyDataList[1];
                        lobbyRenderers.Add(lobbyRenderer);
                    }
                }
                
            }
        }
    }

    private void MakeInstance()
    {
        if (lobby == null)
        {
            lobby = this;
            Debug.Log("inited singleton");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("destroyed singleton");
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        if (!SteamManager.Initialized) { return; }
        MakeInstance();
        InitCallbacks();
        lobbyRenderers = new List<LobbyRenderer>();
        _steamIDs = new List<CSteamID>();
    }
    private void InitCallbacks()
    {
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        c_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbies);
        c_lobbyData = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyInfo);
    }
#region API

    public static void Host(int _maxConnections=7, string access="0")
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _maxConnections);
        Debug.Log("hosting starts");
        AccessMod = access;
    }
    public static void Join(CSteamID lobbyID){
        SteamMatchmaking.JoinLobby(lobbyID);
    }
    public static int GetMaxConn(){
        return lobby.maxConnections;
    }
    public static string GetLobbyCode(){
        return LobbyMetadata.Split(seperator)[0];
    }
    /// <summary>
    /// because this doesnt return a value u need to get the value of lobby.lobbyRenderers
    /// its public so dw
    /// </summary>
    /// <param name="search"></param>
    public static void SetTheLobbyList(string search=""){
        if(string.IsNullOrEmpty(search)) searchString = null;
        else searchString = search;
        SteamMatchmaking.RequestLobbyList();
    }
#endregion
}
