using UnityEngine;
using UnityEngine.InputSystem;
using SF = UnityEngine.SerializeField;
public class SkillTest : MonoBehaviour
{
    private static readonly int TestHash = Animator.StringToHash("Test");
    [SF] private SkillSpawner skillSpawner;
    [SF] private Animator animator;
    [SF] private int testSkillId = 1000; // 테스트할 스킬 ID

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TriggerTestSkill();
        }
    }

    private void TriggerTestSkill()
    {
        Debug.Log($"[테스트] {testSkillId}번 스킬 시전!");

        // 앞서 설계한 프로세스 작동
        // 1. 스패너에게 지금 쓸 스킬 ID 예약
        skillSpawner.UseSkill(testSkillId);

        animator.SetTrigger(TestHash);
    }
}
