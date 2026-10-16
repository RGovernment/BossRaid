using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TestPortal : MonoBehaviour
{
    [SerializeField] private string targetScene;

    private int insideCount;
    private bool isLoading;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || isLoading)
            return;

        if (!IsPlayerCollider(other))
            return;

        insideCount++;

        Debug.Log($"포탈 인원: {insideCount}/{PhotonNetwork.CurrentRoom.PlayerCount}");

        if (insideCount == PhotonNetwork.CurrentRoom.PlayerCount)
            MoveScene();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || isLoading)
            return;

        if (!IsPlayerCollider(other))
            return;

        insideCount = Mathf.Max(0, insideCount - 1);

        Debug.Log($"포탈 인원: {insideCount}/{PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    private bool IsPlayerCollider(Collider other)
    {
        // 플레이어 최상위의 콜라이더만 감지합니다.
        PhotonView playerView = other.GetComponent<PhotonView>();


        return other.CompareTag("Player") && playerView != null && playerView.Owner != null;
    }

    private void MoveScene()
    {
        if (string.IsNullOrWhiteSpace(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError("이동할 씬 이름과 Scene List 등록을 확인하세요.", this);
            return;
        }

        isLoading = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(targetScene);
    }
}