using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSceneManager : MonoBehaviourPunCallbacks
{
    public static GameSceneManager m_Instance { get; private set; }
    [SerializeField] private List<SOSceneData> m_listSceneData = new();
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
        int iInsideCount = 0;
        foreach (Player player in _playersInside)
            if (player != null && player.IsAlive)
                iInsideCount++;

        if (iInsideCount == 0 || iInsideCount != GameManager.m_Instance.GetAlivePlayerCount())
            return false;

        return MoveScene(_refSceneData);
    }

    public bool MoveScene(SOSceneData _refSceneData)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || m_bLoading)
            return false;

        if (_refSceneData == null)
            return false;

        m_bLoading = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LoadLevel(_refSceneData.m_strNextSceneName);
        return true;
    }

    public bool TryGetSpawnOrigin(out Vector3 _vSpawnPos)
    {
        _vSpawnPos = default;
        if (!PhotonNetwork.InRoom)
            return false;

        string sceneName = SceneManager.GetActiveScene().name;
        foreach (SOSceneData sceneData in m_listSceneData)
        {
            if (sceneData != null && sceneData.m_strNextSceneName == sceneName)
            {
                _vSpawnPos = sceneData.m_vSpawnPos;
                return true;
            }
        }
        return false;
    }

    private void OnSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        m_bLoading = false;
        if (TryGetSpawnOrigin(out Vector3 spawn))
            GameManager.m_Instance.RefreshPlayer(spawn);
    }

    public override void OnLeftRoom()
    {
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
