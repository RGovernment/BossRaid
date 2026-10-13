using UnityEngine;

public class DragonBoss : MonoBehaviour
{
    [SerializeField] private MonsterData monsterData;

    [SerializeField] private float currentHp;

    private void Awake()
    {
        currentHp = monsterData.MaxHp;
    }
}