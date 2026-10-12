using UnityEngine;
using UnityEngine.InputSystem;
using SF = UnityEngine.SerializeField;
public class SkillTest : MonoBehaviour
{
    private static readonly int TestHash = Animator.StringToHash("Test");
    [SF] private SkillSpawner skillSpawner;
    [SF] private Animator animator;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TriggerTestSkill(1000);
        }

        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            TriggerTestSkill(1001);
        }
    }

    private void TriggerTestSkill(int id)
    {
        bool success = skillSpawner.UseSkill(id);

        if (!success) return;

        animator.SetTrigger(TestHash);

        Debug.Log($"[테스트] {id}번 스킬 시전!");
    }
}
