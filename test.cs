using System;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
// TODO: implement finished and starting matches
//TODO: use multiple calls when accessing api not a million ugly functions in this script 
public class Lobby : NetworkManager
{
    public static Lobby lobby { get; private set; }

    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    private const string HostAddressName = "HostName";
    private string LobbyCode, Access;
    private static string AccessModifier;
    private static bool didSearch;
    private string publ;
    private static string lobbyCode;
    private List<CSteamID> steamIDs;
    private static string SearchField;
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

        SteamMatchmaking.SetLobbyData(
            steamID,
            HostAddressName,
            SteamUser.GetSteamID().ToString()
        );

        SteamMatchmaking.SetLobbyData(
            steamID,
            LobbyCode,
            "HelperFuncs.GenLobbyCode()"
        );

        SteamMatchmaking.SetLobbyData(
            steamID,
            Access,
            AccessModifier
        );
    }
    protected Callback<LobbyEnter_t> lobbyEntered;

    private void OnLobbyEntered(LobbyEnter_t callback)
    {

        lobbyCode = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            LobbyCode);

        Debug.Log("Lobbycode: " + lobbyCode);

        if (NetworkServer.active) return; // dont do anything if we host

        string HostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressName
        );

        lobby.networkAddress = HostAddress;
        lobby.StartClient();
    }

    protected Callback<LobbyMatchList_t> c_lobbyList;

    private void OnGetLobbies(LobbyMatchList_t callback)
    {
        Debug.Log("Found" + callback.m_nLobbiesMatching + " lobbies");
        steamIDs.Clear(); // no roll-over
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            // since SteamMatchmaking doesnt actually return a list of steamIDs
            // we need to get by index for each one that exists
            CSteamID steamID = SteamMatchmaking.GetLobbyByIndex(i);
            steamIDs.Add(steamID);
            SteamMatchmaking.RequestLobbyData(steamID);
        }

        for (int i = 0; i < steamIDs.Count; i++)
        {
            if(didSearch)
            {
                /// <summary>
                /// only return searchvalue matching ones
                /// </summary>
                /// <value></value>
            }
            else
            {
                /// <summary>
                /// only return public
                /// </summary>
                /// <param name="_lobbyCode"></param>
            }
        }

    }


    private void MakeInstance(){
        if(lobby == null){
            lobby = this;
            Debug.Log("inited singleton");
            DontDestroyOnLoad(gameObject);
        }
        else{
            Debug.Log("destroyed singleton");
            Destroy(gameObject);
        }
    }
    private void Start(){
        if (!SteamManager.Initialized) { return; }
        MakeInstance();
        InitCallbacks();
    }
    private void InitCallbacks(){
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        c_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbies);
    }

#region API
    public static void Host(string _access="0")
    {
        AccessModifier = _access;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, lobby.maxConnections);
        Debug.Log("hosting starts");
    }
    public static void GetLobbyList(string search=""){
        didSearch = string.IsNullOrEmpty(search);
        SearchField = search;
        SteamMatchmaking.RequestLobbyList();
    }
    public static void Join(CSteamID lobbyId){
        SteamMatchmaking.JoinLobby(lobbyId);
    }
    public static string GetLobbyCode(){
        if(!string.IsNullOrEmpty(lobbyCode)) return lobbyCode;
        Debug.Log("empty string aya");
        return System.String.Empty;
    }
#endregion
}
