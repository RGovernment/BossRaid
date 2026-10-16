using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TestPortal : MonoBehaviour
{
    private const float Half = 0.5f;

    [Header("이동할 씬")]
    [SerializeField] private string targetScene;

    [Header("플레이어 감지")]
    [SerializeField] private LayerMask playerLayers;

    [Min(0.01f)]
    [SerializeField] private float checkInterval = 0.5f;

    private readonly HashSet<int> insidePlayers = new HashSet<int>();

    private BoxCollider portalArea;
    private float checkTimer;
    private bool isLoading;

    private void Awake()
    {
        portalArea = GetComponent<BoxCollider>();
        portalArea.isTrigger = true;
    }

    private void Update()
    {
        // 인원 확인과 씬 이동은 방장만 처리합니다.
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || isLoading)
            return;

        checkTimer -= Time.deltaTime;

        if (checkTimer > 0f)
            return;

        checkTimer = checkInterval;
        CheckPlayers();
    }

    private void CheckPlayers()
    {
        insidePlayers.Clear();

        Vector3 center = transform.TransformPoint(portalArea.center);
        Vector3 scale = transform.lossyScale;
        Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 halfExtents = Vector3.Scale(portalArea.size, absoluteScale) * Half;

        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation, playerLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider playerCollider in colliders)
        {
            PhotonView playerView = playerCollider.GetComponentInParent<PhotonView>();

            if (playerView == null || !playerView.CompareTag("Player"))
                continue;

            if (playerView.OwnerActorNr <= 0)
                continue;

            // 플레이어의 콜라이더가 여러 개여도 한 명으로 셉니다.
            insidePlayers.Add(playerView.OwnerActorNr);
        }

        // 현재 방의 모든 플레이어가 들어왔는지 확인합니다.
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!insidePlayers.Contains(player.ActorNumber))
                return;
        }

        MoveScene();
    }

    private void MoveScene()
    {
        if (string.IsNullOrWhiteSpace(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError("이동할 씬 이름과 Build Profiles의 Scene List를 확인하세요.", this);
            enabled = false;
            return;
        }

        isLoading = true;

        Debug.Log($"전원 포탈 입장 완료 → {targetScene} 이동", this);

        PhotonNetwork.LoadLevel(targetScene);
    }
}