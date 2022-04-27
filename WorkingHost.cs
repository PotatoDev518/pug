using System;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
// TODO: implement finished and starting matches

public class Lobby : NetworkManager
{

    public static Lobby lobby { get; private set; }
    private string Kintamo;

    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    private string LobbyDataString;

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
        string lobbyMetadata = SteamUser.GetSteamID().ToString();
        SteamMatchmaking.SetLobbyData(
            steamID,
            LobbyDataString,
            lobbyMetadata
        );
        
    }
    protected Callback<LobbyEnter_t> lobbyEntered;

    private void OnLobbyEntered(LobbyEnter_t callback)
    {

        if (NetworkServer.active) return; // don't do anything if we host

        
        var idk = new CSteamID(callback.m_ulSteamIDLobby);

        LobbyDataString = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            LobbyDataString
        );

        var lobbyMetadata = LobbyDataString.Split(",");

        string HostAddress = lobbyMetadata[0];
        Debug.Log(lobbyMetadata[1]);

        
        lobby.networkAddress = HostAddress;
        lobby.StartClient();
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
    }
    private void InitCallbacks()
    {
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public static void Host()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, lobby.maxConnections);
        Debug.Log("hosting starts");
    }
}
