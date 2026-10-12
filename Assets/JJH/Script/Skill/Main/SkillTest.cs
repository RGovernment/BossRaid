using UnityEngine;
using UnityEngine.InputSystem;
using SF = UnityEngine.SerializeField;
public class SkillTest : MonoBehaviour
{
    private static readonly int TestHash = Animator.StringToHash("Test");
    [SF] private SkillSpawner skillSpawner;
    [SF] private Animator animator;

    private float skill1Cooldown = 3;
    private float skill2Cooldown = 10;

    private float nowCoolSk1 = 0;
    private float nowCoolSk2 = 0;

    void Update()
    {
        if (Keyboard.current == null) return;
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame && nowCoolSk1 < Time.time)
        {
            bool val = TriggerTestSkill(1000);
            if (!val) return;
            Debug.Log("----");
            Debug.Log(nowCoolSk1);
            Debug.Log(nowCoolSk2);
            nowCoolSk1 = Time.time + skill1Cooldown;
            Debug.Log(nowCoolSk1);
            Debug.Log(nowCoolSk2);
        }

        if (Keyboard.current.leftAltKey.wasPressedThisFrame && nowCoolSk2 < Time.time)
        {
            bool val = TriggerTestSkill(1001);
            if (!val) return;
            Debug.Log("====");
            Debug.Log(nowCoolSk1);
            Debug.Log(nowCoolSk2);
            nowCoolSk2 = Time.time + skill2Cooldown;
            Debug.Log(nowCoolSk1);
            Debug.Log(nowCoolSk2);
            
        }
    }

    private bool TriggerTestSkill(int id)
    {
        bool success = skillSpawner.UseSkill(id);

        if (!success) return false;

        animator.SetTrigger(TestHash);

        Debug.Log($"[테스트] {id}번 스킬 시전!");

        return true;
    }
}
