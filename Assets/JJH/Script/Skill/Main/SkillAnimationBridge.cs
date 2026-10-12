using ExitGames.Client.Photon;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using SF = UnityEngine.SerializeField;

public class SkillAnimationBridge : MonoBehaviour
{
    [SF] private SkillSpawner spawner;

    // 유니티 애니메이션 이벤트가 때릴 수 있도록 인자를 딱 1개(string)만 둡니다.
    public void ReceiveAnimEvent(string eventName)
    {
        if (spawner != null)
        {
            spawner.ReceiveAnimEvent(eventName);
        }
    }
}
