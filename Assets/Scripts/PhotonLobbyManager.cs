using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;
using HashTable = ExitGames.Client.Photon.Hashtable;
using PhotonPlayer = Photon.Realtime.Player;

public class PhotonLobbyManager : MonoBehaviourPunCallbacks
{
    private const string VillageScenePath = "Assets/Scenes/Mainvillage.unity";

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text roomInfoText;
    [SerializeField] private TMP_Text lobbyStatsText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private GameObject roomListContent;
    [SerializeField] private GameObject roomButtonPrefab;

    private Dictionary<string, RoomInfo> roomDic = new Dictionary<string, RoomInfo>();

    private string lastStatus;
    private bool startingGame;

    // 1. 만약 연결된 상황이면 아예 연결 버튼 못누르게(연결끊기도 마찬가지)
    // 2. 접속전 닉네임과 게임 버전 설정.
    // 3. 로비 통계( 로비에 몇명, 방이 몇개 있는지.
    // 4. 로비 나가기
    // 5. 끊겼을때 재접속

    // 6. 플레이어 목록 + 방장 표시 
    // 7. 방장 교체 - 3일차
    // 8. 방 닫기 - 3일차
    // 9. (로비에서) 방목록 확인 - 3일차

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        RefreshButtons();
        SetStatus("Disconnect");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RefreshButtons();
    }

    public void OnClickConnect()
    {
        if (PhotonNetwork.IsConnected)
            return;
        PhotonNetwork.NickName = "Player";
        PhotonNetwork.GameVersion = "1.1";
        PhotonNetwork.ConnectUsingSettings();
        SetStatus("Connecting");
        RefreshButtons();
    }

    public void OnClickDisconnect()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.Disconnect();
    }

    public void OnClickJoinLobby()
    {
        if (PhotonNetwork.InLobby)
            return;

        PhotonNetwork.JoinLobby();
    }


    public void OnClickLeaveLobby()
    {
        if (!PhotonNetwork.InLobby)
            return;

        PhotonNetwork.LeaveLobby();
    }
    public void OnClickCreateRoom()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        PhotonNetwork.CreateRoom("ComeOn_", options);
    }
    public void OnClickJoinRoom()
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.InLobby)
            return;
        PhotonNetwork.JoinRoom("ComeOn_");
    }

    public void OnClickReady()
    {
        if (!PhotonNetwork.InRoom)
            return;

        bool ready = IsReady(PhotonNetwork.LocalPlayer);

        HashTable props = new HashTable();
        props["ready"] = !ready;

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void OnClickStart()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || startingGame)
            return;

        foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
        {
            if (!IsReady(player))
            {
                SetStatus("All players must be ready.");
                return;
            }
        }

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(VillageScenePath);
        if (sceneIndex < 0)
        {
            SetStatus("Add Mainvillage to Build Profiles.");
            return;
        }

        startingGame = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(sceneIndex);
    }

    public void OnClickTransferMaster()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        foreach(PhotonPlayer p in PhotonNetwork.PlayerList)
        {
            if (p.IsLocal)
                continue;

            PhotonNetwork.SetMasterClient(p);
            return;
        }
    }

    private void JoinRoomByName(string roomName)
    {
        if (!PhotonNetwork.InLobby)
            return;

        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"JoinRandomRoomFailed ({returnCode} {message})");

        
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"JoinRoomFailed ({returnCode} {message})");

    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"CreateRoomFailed ({returnCode} {message})");

    }

    public void OnClickLeaveRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;

        PhotonNetwork.LeaveRoom();
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("ConnectedToMaster");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SetStatus("InLobby");
    }

    public override void OnLeftLobby()
    {
        SetStatus("LeftLobby");
        ClearRoomList();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
                roomDic.Remove(info.Name);
            else
                roomDic[info.Name] = info;
        }
        RefreshRoomButtons();
    }



    public override void OnJoinedRoom()
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new HashTable { { "ready", false } });
        SetStatus("InRoom");
        RefreshPlayerList();
        ClearRoomList();
        RefreshRoomInfo();
    }

    public override void OnLeftRoom()
    {
        RefreshRoomInfo();
        RefreshPlayerList();
    }

    public override void OnPlayerEnteredRoom(PhotonPlayer newPlayer)
    {
        RefreshRoomInfo();
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(PhotonPlayer otherPlayer)
    {
        RefreshRoomInfo();
        RefreshPlayerList();
    }

    public override void OnPlayerPropertiesUpdate(PhotonPlayer targetPlayer, HashTable changedProps)
    {
        RefreshPlayerList();
    }
    public override void OnRoomPropertiesUpdate(HashTable propertiesThatChanged)
    {

    }

    public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        Debug.Log($"새로운 방장 : {newMasterClient.ActorNumber}");
        RefreshPlayerList();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SetStatus($"Disconnected - {cause}");

        if (cause == DisconnectCause.ServerTimeout || cause == DisconnectCause.ClientTimeout)
        {
            if(PhotonNetwork.Reconnect())
            {
                SetStatus($"Reconnecting ({cause})");
            }
        }
        ClearRoomList();
        RefreshButtons();
    }
    private void SetStatus(string status)
    {
        if (status == lastStatus)
            return;

        lastStatus = status;

        if(statusText != null)
            statusText.text = status;
    }

    private void RefreshButtons()
    {
        ClientState state = PhotonNetwork.NetworkClientState;

        bool offline = (state == ClientState.PeerCreated) || (state == ClientState.Disconnected);

        connectButton.interactable = offline;
        disconnectButton.interactable = !offline;
    }

    public void RefreshLobbyStats()
    {
        if(PhotonNetwork.InLobby)
        {
            lobbyStatsText.text = $"Current Player {PhotonNetwork.CountOfPlayersOnMaster} / Count Room {PhotonNetwork.CountOfRooms}";
        }
        else
        {
            lobbyStatsText.text = "Not Lobby.";
        }
    }

    private void RefreshRoomInfo()
    {
        Room room = PhotonNetwork.CurrentRoom;
        string text = "No Room";

        if(PhotonNetwork.InRoom && room != null)
        {
            text = $"Current Room Name : {room.Name} ({room.PlayerCount} / {room.MaxPlayers})";
        }

        roomInfoText.text = text;
    }
    private void RefreshPlayerList()
    {
        string text = "No Room";

        if(PhotonNetwork.InRoom)
        {
            StringBuilder sb = new StringBuilder();
            
            foreach(PhotonPlayer p in PhotonNetwork.PlayerList)
            {
                sb.AppendLine($"{p.ActorNumber}. {p.NickName}{(p.IsMasterClient ? " *" : "")}");

                if (IsReady(p))
                    sb.Append(" [Ready]");

                sb.AppendLine();
            }

            text = sb.ToString();
        }

        playerListText.text = text;
    }

    private bool IsReady(PhotonPlayer player)
    {
        return player.CustomProperties.TryGetValue("ready", out object value) && value is bool ready && ready;
    }

    private void ClearRoomList()
    {
        roomDic.Clear();
        RefreshRoomButtons();
    }

    private void RefreshRoomButtons()
    {
        foreach(Transform child in roomListContent.transform)
        {
            Destroy(child.gameObject);
        }

        if (!PhotonNetwork.InLobby)
            return;

        foreach(RoomInfo info in roomDic.Values)
        {
            RoomButton item = Instantiate(roomButtonPrefab, roomListContent.transform).GetComponent<RoomButton>();

            // RoomButton에서 버튼 정보 초기화
            item.Setup(info, JoinRoomByName);
        }
    }
    public void LeaveCurrentRoom()
    {
        if(PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("LobyScene");
    }

}
