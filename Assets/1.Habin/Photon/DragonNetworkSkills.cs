using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonNetworkSkills : MonoBehaviourPun
{
    [Header("스킬 참조")]
    [SerializeField] private DragonSlam slam;
    [SerializeField] private DragonTailWhip tailWhip;

    private void Awake()
    {
        if (slam == null)
            slam = GetComponent<DragonSlam>();
        if(tailWhip == null)
            tailWhip = GetComponent<DragonTailWhip>();
    }

    public bool PlaySlam()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return false;

        if (slam == null || !slam.isActiveAndEnabled || slam.IsAttacking)
            return false;

        slam.Slam();

        photonView.RPC(nameof(ReceiveSlam), RpcTarget.Others, transform.position, transform.rotation);

        return true;
    }
    public bool PlayTailWhip()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return false;

        if (tailWhip == null || !tailWhip.isActiveAndEnabled || tailWhip.IsAttacking)
            return false;

        tailWhip.TailWhip();

        photonView.RPC(nameof(ReceiveTailWhip), RpcTarget.Others, transform.position, transform.rotation);

        return true;

    }
    [PunRPC]
    private void ReceiveSlam(Vector3 bossPosition, Quaternion bossRotation, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        if (slam == null || !slam.isActiveAndEnabled)
            return;

        transform.SetPositionAndRotation(bossPosition, bossRotation);

        slam.Slam();
    }
    [PunRPC]
    private void ReceiveTailWhip(Vector3 bossPosition, Quaternion bossRotation, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        if (tailWhip == null || !tailWhip.isActiveAndEnabled)
            return;

        transform.SetPositionAndRotation(bossPosition, bossRotation);

        tailWhip.TailWhip();
    }
}