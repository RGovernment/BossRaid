using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RaidTestLauncher : MonoBehaviourPunCallbacks
{
    [Header("접속")]
    [SerializeField] private string roomName = "BossRaid-Test";
    [SerializeField] private string gameVersion = "BossRaid-Test-1";
    [SerializeField] private string region = "kr";

    [Header("기존 플레이어 프리팹")]
    [SerializeField] private GameObject playerPrefab;

    [Header("생성 위치 4개")]
    [SerializeField] private Transform[] spawnPoints;

    private const byte MaxPlayers = 4;
    private const string ReadyKey = "Ready";
    private const string StartedKey = "Started";

    private bool startRequested;
    private bool hasSpawned;

    private void Start()
    {
        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length < MaxPlayers)
        {
            Debug.LogError("플레이어 프리팹과 생성 위치 4개를 연결해주세요.");
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
            {
                Debug.LogError("비어 있는 생성 위치가 있습니다.");
                return;
            }
        }

        Application.runInBackground = true;

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = region;

        // 각 실행 창을 서로 다른 접속자로 구분합니다.
        PhotonNetwork.AuthValues = new AuthenticationValues(System.Guid.NewGuid().ToString());

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = MaxPlayers,
            CustomRoomProperties = new Hashtable
            {
                { StartedKey, false }
            }
        };

        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"방 입장: {PhotonNetwork.LocalPlayer.ActorNumber}, 방장: {PhotonNetwork.IsMasterClient}");

        // 입장하면 자동으로 Ready 상태가 됩니다.
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { ReadyKey, true }
        });

        TryStartGame();
        TrySpawnPlayer();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        TryStartGame();
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        TryStartGame();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        TrySpawnPlayer();
    }

    private void TryStartGame()
    {
        if (!PhotonNetwork.IsMasterClient || startRequested)
            return;

        if (PhotonNetwork.CurrentRoom.PlayerCount != MaxPlayers)
            return;

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(ReadyKey, out object ready) || !(ready is bool isReady) || !isReady)
                return;
        }

        startRequested = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { StartedKey, true }
        });

        Debug.Log("4명 Ready 완료. 테스트 시작!");
    }

    private void TrySpawnPlayer()
    {
        if (!PhotonNetwork.InRoom || hasSpawned)
            return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(StartedKey, out object started) || !(started is bool isStarted) || !isStarted)
            return;

        // 접속 순서에 따라 서로 다른 생성 위치를 사용합니다.
        int spawnIndex = 0;

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber < PhotonNetwork.LocalPlayer.ActorNumber)
                spawnIndex++;
        }

        Transform spawnPoint = spawnPoints[spawnIndex];

        hasSpawned = true;
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 입장 실패: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Photon 연결 종료: {cause}");
    }
}