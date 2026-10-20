using Photon.Pun;
using TMPro;
using UnityEngine;

public class AltarInteractionUI : MonoBehaviour
{
    [Header("화면")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private TMP_Text interactionText;

    [Header("조준 검사")]
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Min(0.01f)]
    [SerializeField] private float rayDistance = 100f;

    [Header("상호작용")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Min(0.01f)]
    [SerializeField] private float interactionDuration = 3f;

    private Player localPlayer;
    private PowerAltar selectedAltar;
    private float interactionTimer;

    private void Start()
    {
        if (interactionText == null)
        {
            Debug.LogError("Interaction Text를 연결하세요.", this);
            enabled = false;
            return;
        }

        ResetSelection();
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom || !Application.isFocused || Time.timeScale <= 0f)
        {
            ResetSelection();
            return;
        }

        FindLocalPlayer();

        if (localPlayer == null || !localPlayer.IsAlive)
        {
            ResetSelection();
            return;
        }

        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
            viewCamera = Camera.main;

        if (viewCamera == null)
        {
            ResetSelection();
            return;
        }

        // 커서가 잠겨 있으면 화면 중앙으로 조준합니다.
        Vector3 screenPosition = Cursor.lockState == CursorLockMode.Locked
            ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
            : Input.mousePosition;

        Ray ray = viewCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, raycastLayers, QueryTriggerInteraction.Ignore))
        {
            ResetSelection();
            return;
        }

        PowerAltar altar = hit.collider.GetComponentInParent<PowerAltar>();

        if (altar == null || altar.IsDestroyed || !altar.IsInRange(localPlayer))
        {
            ResetSelection();
            return;
        }

        // 다른 제단을 가리키면 누르기 시간을 초기화합니다.
        if (selectedAltar != altar)
        {
            selectedAltar = altar;
            interactionTimer = 0f;
        }

        interactionText.gameObject.SetActive(true);

        if (altar.IsRequestPending)
        {
            interactionTimer = 0f;
            interactionText.text = "처리 중...";
            return;
        }

        if (!Input.GetKey(interactionKey))
        {
            interactionTimer = 0f;
            interactionText.text = $"{interactionKey}키를 눌러 제단 파괴";
            return;
        }

        interactionTimer += Time.deltaTime;
        interactionText.text = $"제단 파괴 중 {interactionTimer:F1} / {interactionDuration:F1}초";

        if (interactionTimer >= interactionDuration)
        {
            interactionTimer = 0f;
            altar.RequestDestroy(localPlayer);
        }
    }

    private void FindLocalPlayer()
    {
        if (localPlayer != null)
            return;

        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in players)
        {
            if (player.photonView.IsMine)
            {
                localPlayer = player;
                return;
            }
        }
    }

    private void ResetSelection()
    {
        selectedAltar = null;
        interactionTimer = 0f;

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ResetSelection();
    }
}