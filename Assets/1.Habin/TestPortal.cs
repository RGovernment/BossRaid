using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TestPortal : MonoBehaviour
{
    [SerializeField] private SOSceneData m_refSceneData;

    private readonly HashSet<Player> m_playersInside = new();
    private bool isLoading;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || isLoading)
            return;

        Player player = GetPlayer(other);
        if (player == null || !player.IsAlive)
            return;

        m_playersInside.Add(player);
        isLoading = GameSceneManager.m_Instance.MoveScene(m_refSceneData, m_playersInside);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || isLoading)
            return;

        m_playersInside.Remove(GetPlayer(other));
    }

    private Player GetPlayer(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
            return other.GetComponent<Player>();

        return null;
    }

}
