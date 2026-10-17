using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    public static GameManager m_Instance { get; private set; }
    public IReadOnlyList<Player> Players => m_listPlayer;

    private readonly List<Player> m_listPlayer = new();

 
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
    }

    public void OnEnterPlayer(Player _refPlayer)
    {
        if (_refPlayer == null || m_listPlayer.Contains(_refPlayer))
            return;

        m_listPlayer.Add(_refPlayer);
    }

    public void OnExitPlayer(Player _refPlayer)
    {
        m_listPlayer.Remove(_refPlayer);
    }

    public void RefreshPlayer(Vector3 _vSpawnPos)
    {
        foreach (Player player in m_listPlayer)
        {
            if (player != null)
                player.RefreshPlayer(_vSpawnPos);
        }
    }
    
    public int GetAlivePlayerCount()
    {
        int iCount = 0;

        for(int i = 0; i<m_listPlayer.Count; ++i)
        {
            Player refPlayer = m_listPlayer[i];
            if (refPlayer != null && refPlayer.IsAlive)
                ++iCount;
        }


        return iCount;
    }

    private void OnDestroy()
    {
        if (m_Instance != this)
            return;

        m_listPlayer.Clear();
        m_Instance = null;
    }

}
