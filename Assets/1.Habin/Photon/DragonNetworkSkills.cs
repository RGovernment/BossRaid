using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonNetworkSkills : MonoBehaviourPun
{
    [Header("스킬 참조")]
    [SerializeField] private DragonSlam slam;
    [SerializeField] private DragonTailWhip tailWhip;
    [SerializeField] private DragonShockwave shockwave;


    private void Awake()
    {
        if (slam == null)
            slam = GetComponent<DragonSlam>();
        if(tailWhip == null)
            tailWhip = GetComponent<DragonTailWhip>();
        if (shockwave == null)
            shockwave = GetComponent<DragonShockwave>();
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
    public bool PlayShockwave()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return false;

        if (shockwave == null || !shockwave.isActiveAndEnabled || shockwave.IsAttacking)
            return false;

        shockwave.Shockwave();

        if (!shockwave.IsAttacking)
            return false;

        photonView.RPC(nameof(ReceiveShockwave), RpcTarget.Others, transform.position, transform.rotation, shockwave.AttackCenter);

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
    [PunRPC]
    private void ReceiveShockwave(Vector3 bossPosition, Quaternion bossRotation, Vector3 attackCenter, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        if (shockwave == null || !shockwave.isActiveAndEnabled)
            return;

        transform.SetPositionAndRotation(bossPosition, bossRotation);

        shockwave.Shockwave(attackCenter);
    }

}