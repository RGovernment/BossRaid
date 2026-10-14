using UnityEngine;
using static FSM;
using static InputManager;
using static UnityEngine.InputSystem.InputAction;

[RequireComponent(typeof(FSM), typeof(Animator), typeof(Rigidbody))]
public sealed class Player : MonoBehaviour, ITakeDamgeable
{
    //스킬같은 우선권이 밀릴 때도 마우스 회전을 받기 위해서 Player쪽에서 관리
    [SerializeField, Min(0f)] private float m_fRotateSensitivity = 3f;
    private float m_fRotateY;
    public Quaternion ViewRotation => Quaternion.Euler(0f, m_fRotateY, 0f);

    private FSM m_refFSM;

    private ObjectInfo m_refObjectInfo = new();
    public ObjectInfo ObjectInfo => m_refObjectInfo;

    [SerializeField] private SkillSpawner m_refTest;

    private void Awake()
    {
        //임시 코드 -> SO로 수정
        m_refObjectInfo.CurrentHP = 100.0f;
        m_refObjectInfo.CurrentSpeed = 2.0f;

        m_refTest = GetComponentInChildren<SkillSpawner>();
        Init();
    }

    private void Init()
    {
        m_refFSM = GetComponent<FSM>();
        m_fRotateY = GetComponent<Rigidbody>().rotation.eulerAngles.y;
        if (!m_refFSM.Init(this))
            enabled = false;
    }

    private void Start()
    {
        InputManager.m_Instance.Skill0Action.started += Skill0;
    }

    private void OnEnable()
    {
        m_refFSM.StartState();
    }

    private void Update()
    {
        if(m_refTest)
        if (m_Instance.HasGameplayInput)
        {
            float fRotate = m_Instance.InputInfo.Delta.x * m_fRotateSensitivity;
            m_fRotateY = Mathf.Repeat(m_fRotateY + fRotate, 360f);
        }

        m_refFSM.UpdateState();
    }

    private void FixedUpdate()
    {
        m_refFSM.FixedUpdateState();
    }

    private void OnDisable()
    {
        m_refFSM.Stop();
    }

    private void OnDestroy()
    {
        m_refFSM.Dispose();
    }

    public void TakeDamage(tAttackInfo _tInfo)
    {
        if (m_refFSM.CurrentState?.Type == eStateType.Dead)
            return;

        m_refObjectInfo.CurrentHP -= _tInfo.Damage;
        //if(m_refObjectInfo.CurrentHP >)

        m_refFSM.TakeDamage(_tInfo);
    }

    private void Skill0(CallbackContext _tCallback)
    {
        m_refFSM.Animator.SetTrigger("Test");
        m_refTest.UseSkill(1001);
    }
}
