using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class AltarDestroyCounter : MonoBehaviourPunCallbacks
{
    private const string CountKey = "AltarDestroyedCount";

    [SerializeField] private int destroyedCount;

    public int DestroyedCount => destroyedCount;

    public override void OnEnable()
    {
        base.OnEnable();
        PowerAltar.OnAltarDestroyed += HandleAltarDestroyed;
    }

    public override void OnDisable()
    {
        PowerAltar.OnAltarDestroyed -= HandleAltarDestroyed;
        base.OnDisable();
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (PhotonNetwork.IsMasterClient)
            ResetCount();
        else
            ReadCount();
    }

    public void ResetCount()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        destroyedCount = 0;
        PublishCount();
    }

    private void HandleAltarDestroyed()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        destroyedCount++;
        PublishCount();

        Debug.Log($"제단 파괴 수: {destroyedCount}", this);
    }

    private void PublishCount()
    {
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { CountKey, destroyedCount }
        });
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(CountKey))
            ReadCount();
    }

    private void ReadCount()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CountKey, out object value))
            destroyedCount = (int)value;
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        ReadCount();
    }
}