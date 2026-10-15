using Cysharp.Threading.Tasks;
using Photon.Pun;
using System;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using SF = UnityEngine.SerializeField;
public class SkillPointer : MonoBehaviourPun
{
    [Header("대상 및 범위 설정")]
    [SF] private Transform playerTransform;
    // 최대 사거리
    [SF] private float maxRange = 10f;
    [SF] private LayerMask groundLayer;
    [SF] private float yOffset = 0.01f; 

    [Header("파티클 시스템")]
    [SF] private ParticleSystem blueParticle; // 사거리 내
    [SF] private ParticleSystem redParticle;  // 사거리 외

    private Camera mainCamera;
    private bool isOutOfRange = false;
    public bool CanUseSkill => !isOutOfRange;
    private CancellationTokenSource pointerToken;
    void Start()
    {
        mainCamera = Camera.main;
    }

    public async UniTask<Vector3?> ActiveSkillPointer()
    {
        pointerToken = new CancellationTokenSource();
        CancellationToken token = pointerToken.Token;
        return await TrackIndicatorPosition(token);
    }

    public void TrackCancel()
    {
        if (pointerToken == null) return;

        pointerToken.Cancel();
        pointerToken.Dispose();
        pointerToken = null;
        if (redParticle.gameObject.activeSelf)
            redParticle.gameObject.SetActive(false);
        if (blueParticle.gameObject.activeSelf)
            blueParticle.gameObject.SetActive(false);

    }

    private async UniTask<Vector3?> TrackIndicatorPosition(CancellationToken cancellationToken)
    {
        try
        {
            isOutOfRange = false;
            SwitchParticle(false);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            while (true)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {

                    redParticle.gameObject.SetActive(false);
                    blueParticle.gameObject.SetActive(false);

                    if (CanUseSkill)
                        return transform.position;
                    else
                        return null;
                }

                if (playerTransform == null)
                    throw new SkillTargetingException("조준할 플레이어가 존재하지 않습니다.");
            
                if(mainCamera == null)
                    throw new SkillTargetingException("카메라가 존재하지 않습니다.");


                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                Vector3 screenCenter = new (Screen.width / 2f, Screen.height / 2f, 0);
                Ray ray = mainCamera.ScreenPointToRay(screenCenter);

                Vector3 targetPosition;

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
                    targetPosition = hit.point;
                else
                    targetPosition = ray.origin + (ray.direction * 100f);
        

                Vector3 playerFloorPos = new (playerTransform.position.x, 0, playerTransform.position.z);
                Vector3 targetFloorPos = new (targetPosition.x, 0, targetPosition.z);

                Vector3 direction = (targetFloorPos - playerFloorPos).normalized;
                float currentDistance = Vector3.Distance(playerFloorPos, targetFloorPos);

                Vector3 finalPosition;

                if (currentDistance > maxRange)
                {
                    finalPosition = playerFloorPos + (direction * maxRange);
                    SwitchParticle(false);
                    if (!isOutOfRange)
                        isOutOfRange = true;
                        
                    
                }
                else
                {
                    finalPosition = targetFloorPos;
                    SwitchParticle(true);
                    if (isOutOfRange)
                        isOutOfRange = false;
                }

                finalPosition.y = playerTransform.position.y + yOffset;

                transform.SetPositionAndRotation(finalPosition, 
                    Quaternion.Euler(0, playerTransform.eulerAngles.y, 0));
            }
        }    
        // UniTask가 중간에 취소(오브젝트 파괴 등)되었을 때
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
    }



    private void SwitchParticle(bool type)
    {
        if (type)
        {
            if(redParticle.gameObject.activeSelf)
                redParticle.gameObject.SetActive(false);
            if(!blueParticle.gameObject.activeSelf)
                blueParticle.gameObject.SetActive(true);
        }
        else
        {
            if (!redParticle.gameObject.activeSelf)
                redParticle.gameObject.SetActive(true);
            if (blueParticle.gameObject.activeSelf)
                blueParticle.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        TrackCancel();
    }

}
