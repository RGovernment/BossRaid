using UnityEngine;

public class DragonBoss : MonoBehaviour
{
    [SerializeField] private MonsterData monsterData;

    [SerializeField] private float currentHp;
    [SerializeField] private float attack;


    public float Attack => attack;
    private void Awake()
    {
        currentHp = monsterData.MaxHp;
        attack = monsterData.Attack;

    }
}