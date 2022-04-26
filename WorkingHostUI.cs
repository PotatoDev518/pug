using System;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
// TODO: implement finished and starting matches
public class Lobby : NetworkManager{  
  
  public static Lobby lobby{ get; private set; }

    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback){
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    private const string HostAddressName = "HostName";
    private string LobbyCode, Access;
    private static string AccessModifier;
    public UIManager uIManager;
    private static string lobbyCode = "lmao";
    protected Callback<LobbyCreated_t> lobbyCreated;
    private void OnLobbyCreated(LobbyCreated_t callback){
        if (callback.m_eResult != EResult.k_EResultOK){
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
            HelperFuncs.GenLobbyCode()
        );

        SteamMatchmaking.SetLobbyData(
            steamID,
            Access,
            AccessModifier
        );
    }
    protected Callback<LobbyEnter_t> lobbyEntered;

    public enum Received{
        ReceivedFromuIManager = 0
    }
    public static Received ReceivedFrom;

    private void OnLobbyEntered(LobbyEnter_t callback){

        string _lobbyCode = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            LobbyCode);
        LobbyCodeReceiver(_lobbyCode);

        if(NetworkServer.active) return; // dont do anything if we host

        string HostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressName
        );

        lobby.networkAddress = HostAddress;
        lobby.StartClient();
    }
    private void LobbyCodeReceiver(string _lobbyCode){
        
        if(ReceivedFrom == Received.ReceivedFromuIManager){
            uIManager.SetText(_lobbyCode);
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
        }

#region API
    public static void Host(int Sender, string _access="0")
    {
        switch (Sender)
        {
            case 1:
                ReceivedFrom = Received.ReceivedFromuIManager;
                break;
        }
        AccessModifier = _access;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, lobby.maxConnections);
        Debug.Log("hosting starts");
    }
    public static void GetLobbyList(){

    }
#endregion
}
