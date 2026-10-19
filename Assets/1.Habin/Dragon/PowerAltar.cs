using System;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PowerAltar : MonoBehaviourPun
{
    public static event Action OnAltarDestroyed;

    [Header("상호작용 범위")]
    [Min(0f)]
    [SerializeField] private float interactionDistance = 3f;

    private bool destructionHandled;

    public bool IsDestroyed => destructionHandled;
    public bool IsRequestPending { get; private set; }

    public bool IsInRange(Player player)
    {
        if (player == null || !player.IsAlive)
            return false;

        Vector3 offset = player.transform.position - transform.position;

        return offset.sqrMagnitude <= interactionDistance * interactionDistance;
    }

    public void RequestDestroy(Player player)
    {
        if (!PhotonNetwork.InRoom || destructionHandled || IsRequestPending)
            return;

        if (!IsInRange(player))
            return;

        IsRequestPending = true;

        photonView.RPC(nameof(ReceiveAltarInteraction), RpcTarget.MasterClient, player.photonView.ViewID);
    }

    [PunRPC]
    private void ReceiveAltarInteraction(int playerViewId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || destructionHandled)
            return;

        PhotonView playerView = PhotonView.Find(playerViewId);
        Player player = playerView != null ? playerView.GetComponent<Player>() : null;

        // 요청한 사람이 해당 캐릭터의 소유자인지와 거리를 확인합니다.
        if (playerView == null || playerView.Owner != info.Sender || !IsInRange(player))
        {
            photonView.RPC(nameof(RejectInteraction), info.Sender);
            return;
        }

        destructionHandled = true;

        // 카운트를 먼저 올리고 제단 전체를 제거합니다.
        OnAltarDestroyed?.Invoke();

        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void RejectInteraction(PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        IsRequestPending = false;
    }
}