using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class TestPlayerOwnership : MonoBehaviourPun
{
    [Header("본인만 실행할 스크립트")]
    [SerializeField] private MonoBehaviour[] localOnlyScripts;

    [Header("본인만 사용할 카메라 등의 오브젝트")]
    [SerializeField] private GameObject[] localOnlyObjects;

    private void Start()
    {
        bool isMine = photonView.IsMine;

        foreach (MonoBehaviour script in localOnlyScripts)
        {
            if (script != null && script != this)
                script.enabled = isMine;
        }

        foreach (GameObject localObject in localOnlyObjects)
        {
            if (localObject != null)
                localObject.SetActive(isMine);
        }

        // 다른 사람의 캐릭터는 받은 위치를 따라갑니다.
        if (!isMine && TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
}