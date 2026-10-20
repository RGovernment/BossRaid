using Photon.Pun;
using System.Xml.Serialization;
using Unity.Behavior;
using UnityEngine;

public class DragonNetworkAuthority : MonoBehaviourPunCallbacks
{
    [Header("참조")]
    [SerializeField] private BehaviorGraphAgent behaviorAgent;

    private void Awake()
    {
        if (behaviorAgent == null)
        {
            behaviorAgent = GetComponent<BehaviorGraphAgent>();
        }
    }
    private void Start()
    {
        UpdateAuthority();
    }
    public override void OnJoinedRoom()
    {
        UpdateAuthority();
    }
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        UpdateAuthority();
    }
    public override void OnLeftRoom()
    {
        UpdateAuthority();
    }
    private void UpdateAuthority()
    {
        if (behaviorAgent == null)
            return;

        DragonBoss boss = GetComponent<DragonBoss>();
        DragonWipeMechanic mechanic = GetComponent<DragonWipeMechanic>();

        bool canRunAI = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;

        if (boss != null && boss.IsDead)
            canRunAI = false;

        if (mechanic != null && mechanic.IsActive)
            canRunAI = false;

        behaviorAgent.enabled = canRunAI;
    }
}