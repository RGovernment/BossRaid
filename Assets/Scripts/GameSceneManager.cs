using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public sealed class GameSceneManager : MonoBehaviourPunCallbacks
{
    private const string SceneKey = "gameScene";
    private const string SpawnKey = "gameSceneSpawn";
    private const string RequestKey = "gameSceneRequest";

    public static GameSceneManager m_Instance { get; private set; }
    private SOSceneData m_refPendingScene;
    private int m_iRequest;
    private bool m_bLoading;

    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        m_Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public bool MoveScene(SOSceneData _refSceneData, IEnumerable<Player> _playersInside)
    {
        int insideCount = 0;
        foreach (Player player in _playersInside)
            if (player != null && player.IsAlive)
                insideCount++;

        if (insideCount == 0 || insideCount != GameManager.m_Instance.GetAlivePlayerCount())
            return false;

        return MoveScene(_refSceneData);
    }

    public bool MoveScene(SOSceneData _refSceneData)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || m_bLoading || m_refPendingScene != null)
            return false;

        if (_refSceneData == null)
            return false;

        m_refPendingScene = _refSceneData;
        m_iRequest = PhotonNetwork.CurrentRoom.CustomProperties[RequestKey] is int previous ? previous + 1 : 1;
        if (PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [SceneKey] = _refSceneData.m_strNextSceneName,
            [SpawnKey] = _refSceneData.m_vSpawnPos,
            [RequestKey] = m_iRequest
        }))
            return true;

        m_refPendingScene = null;
        return false;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (m_refPendingScene == null || !PhotonNetwork.IsMasterClient ||
            !(propertiesThatChanged[RequestKey] is int request) || request != m_iRequest)
            return;

        // LoadLevel pauses message dispatch, so wait until the server has shared the spawn position.
        string sceneName = m_refPendingScene.m_strNextSceneName;
        m_refPendingScene = null;
        m_bLoading = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LoadLevel(sceneName);
    }

    public bool TryGetSpawnOrigin(out Vector3 _vSpawnPos)
    {
        _vSpawnPos = default;
        if (!PhotonNetwork.InRoom)
            return false;

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        if (!(properties[SceneKey] is string sceneName) || sceneName != SceneManager.GetActiveScene().name ||
            !(properties[SpawnKey] is Vector3 spawn))
            return false;

        _vSpawnPos = spawn;
        return true;
    }

    public static Vector3 GetPlayerSpawnPosition(Vector3 _vOrigin, int _iActorNumber)
    {
        int slot = Mathf.Max(0, _iActorNumber - 1) % 4;
        return _vOrigin + new Vector3((slot % 2) * 3f - 1.5f, 0f, (slot / 2) * 3f);
    }

    private void OnSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        m_bLoading = false;
        if (TryGetSpawnOrigin(out Vector3 spawn))
            GameManager.m_Instance.RefreshPlayer(spawn);
    }

    public override void OnLeftRoom()
    {
        m_refPendingScene = null;
        m_bLoading = false;
    }

    private void OnDestroy()
    {
        if (m_Instance != this)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        m_Instance = null;
    }
}
