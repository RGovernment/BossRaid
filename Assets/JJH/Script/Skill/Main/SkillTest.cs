using UnityEngine;
using UnityEngine.InputSystem;
using SF = UnityEngine.SerializeField;
public class SkillTest : MonoBehaviour
{
    private static readonly int TestHash = Animator.StringToHash("Test");
    private static readonly int Test2Hash = Animator.StringToHash("Test2");
    [SF] private SkillSpawner skillSpawner;
    [SF] private Animator animator;

    private float skill1Cooldown = 3;
    private float skill2Cooldown = 10;
    private float skill3Cooldown = 20;

    private float nowCoolSk1 = 0;
    private float nowCoolSk2 = 0;
    private float nowCoolSk3 = 0;
    void Update()
    {
        if (Keyboard.current == null) return;
        if (animator.IsInTransition(0)) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("SlashEffect") ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("BladeDanceEffect2")) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame && nowCoolSk1 < Time.time)
        {
            bool val = TriggerTestSkill(1000);
            if (!val) return;

            nowCoolSk1 = Time.time + skill1Cooldown;
        }

        if (Keyboard.current.leftAltKey.wasPressedThisFrame && nowCoolSk2 < Time.time)
        {
            bool val = TriggerTestSkill(1001);
            if (!val) return;

            nowCoolSk2 = Time.time + skill2Cooldown;
            
        }

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame && nowCoolSk3 < Time.time)
        {
            bool val = TriggerTestSkill(1002);
            if (!val) return;

            nowCoolSk3 = Time.time + skill3Cooldown;

        }
    }

    private bool TriggerTestSkill(int id)
    {
        bool success = skillSpawner.UseSkill(id);

        if (!success) return false;

        if(id != 1002)
            animator.SetTrigger(TestHash);
        else 
            animator.SetTrigger(Test2Hash);

        Debug.Log($"[테스트] {id}번 스킬 시전!");

        return true;
    }
}
