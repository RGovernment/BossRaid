using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TestPortal : MonoBehaviour
{
    [SerializeField] private SOSceneData m_refSceneData;

    private readonly HashSet<Player> m_hashPlayersInside = new();
    private bool m_bIsLoading;
    public event Action<Player> OnPlayerInPortal;

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || m_bIsLoading)
            return;

        Player player = GetPlayer(other);
        if (player == null || !player.IsAlive)
            return;

        m_hashPlayersInside.Add(player);
        OnPlayerInPortal?.Invoke(player);
        m_bIsLoading = GameSceneManager.m_Instance.MoveScene(m_refSceneData, m_hashPlayersInside);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || m_bIsLoading)
            return;

        Player player = GetPlayer(other);

        m_hashPlayersInside.Remove(player);
        OnPlayerInPortal?.Invoke(player);
    }

    private Player GetPlayer(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
            return other.GetComponent<Player>();

        return null;
    }

}
