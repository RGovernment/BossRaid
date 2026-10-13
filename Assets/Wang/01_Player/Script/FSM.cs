using System.Collections.Generic;
using UnityEngine;
using static InputManager;

public sealed class FSM : MonoBehaviour
{
    public enum eStateType
    {
        Idle,
        Walk,
        Run,
        Jump,
        Roll,
        Hit,
        Dead,
        FallDown,
    }

    [SerializeField] private List<SOState> m_listState = new List<SOState>();
    [SerializeField] private eStateType m_eInitialState = eStateType.Idle;
    [SerializeField] private LayerMask m_iGroundLayers = ~0;

    private readonly Dictionary<eStateType, AnimatorEvent> m_hashAnimEvent = new Dictionary<eStateType, AnimatorEvent>();
    private readonly Dictionary<eStateType, PlayerState> m_hashState = new Dictionary<eStateType, PlayerState>();
    private PlayerState m_refCurState;
    private Player m_refPlayer;
    private Collider m_refGroundCollider;

    public PlayerState CurrentState => m_refCurState;
    public PlayerState PreviousState { get; private set; }
    public Animator Animator { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public Quaternion ViewRotation => m_refPlayer.ViewRotation;
    public bool IsGrounded => m_refGroundCollider != null && Rigidbody.linearVelocity.y <= 0.1f;

    public bool Init(Player _refController)
    {
        m_refPlayer = _refController;
        Animator = GetComponent<Animator>();
        Rigidbody = GetComponent<Rigidbody>();

        foreach (SOState refData in m_listState)
        {
            if (m_hashState.ContainsKey(refData.State))
            {
                Debug.LogError("FSM에 같은 상태가 중복 등록되었습니다.", this);
                return false;
            }

            PlayerState refState = refData.CreateState(this, refData.Clip);
            m_hashState.Add(refState.Type, refState);
            m_hashAnimEvent.Add(refState.Type, new AnimatorEvent());
        }

        if (!m_hashState.ContainsKey(m_eInitialState))
        {
            Debug.LogError("FSM 초기 상태의 SOState를 등록해야 합니다.", this);
            return false;
        }

        foreach (var tKeyValue in m_hashState)
            tKeyValue.Value.Init();

        m_Instance.OnInputCanceled += CancelInput;
        return true;
    }

    public void Dispose()
    {
        Stop();
        if (m_Instance != null)
            m_Instance.OnInputCanceled -= CancelInput;
        foreach (var tKeyValue in m_hashState)
            tKeyValue.Value.Dispose();

        m_hashState.Clear();
        m_hashAnimEvent.Clear();
        PreviousState = null;
    }

    private void OnDisable()
    {
        Stop();
        ClearGroundContact();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void CancelInput()
    {
        if (m_refCurState?.Type != eStateType.Hit && m_refCurState?.Type != eStateType.FallDown)
            Stop();
    }

    public bool StartState()
    {
        return ChangeState(m_eInitialState);
    }

    public void TakeDamage(tAttackInfo _tInfo)
    {
        eStateType _eCurState = m_refCurState.Type;

        if (_eCurState == eStateType.Jump || !IsGrounded)
        {
            PlayerHitState refDownState = FindState<PlayerHitState>(eStateType.FallDown);
            if (refDownState == null)
                return;
            refDownState.SetHitInfo(_tInfo);
        }

        if (_eCurState != eStateType.Jump)
        {
            PlayerHitState refHitState = FindState<PlayerHitState>(eStateType.Hit);
            if (refHitState == null)
                return;
            refHitState.SetHitInfo(_tInfo);
        }

        ChangeState(_eCurState);
    }

    public void UpdateState()
    {
        if (m_Instance.HasGameplayInput == false &&
            m_refCurState?.Type != eStateType.Hit &&
            m_refCurState?.Type != eStateType.FallDown)
        {
            Stop();
            return;
        }

        m_refCurState?.UpdateState();
    }

    public void FixedUpdateState()
    {
        m_refCurState?.FixedUpdateState();
    }

    // Animator와 같은 오브젝트에서 모든 클립 이벤트를 받습니다.
    public void ExecuteEvent(AnimationEvent _refEvent)
    {
        if (m_refCurState == null)
            return;

        m_hashAnimEvent[m_refCurState.Type].ExecuteFrame(_refEvent);
    }

    public bool ChangeState(eStateType _eState)
    {
        PlayerState refFindState = FindState<PlayerState>(_eState);
        if (refFindState == null || m_refCurState == refFindState)
            return false;

        if (!refFindState.CanEnter())
            return false;

        PreviousState = m_refCurState;
        PreviousState?.Exit();
        m_refCurState = refFindState;
        m_refCurState.Enter();
        return true;
    }

    public void Stop()
    {
        if (m_refCurState == null)
            return;

        PreviousState = m_refCurState;
        m_refCurState.Exit();
        m_refCurState = null;

        Vector3 vVelocity = Rigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        Rigidbody.linearVelocity = vVelocity;
    }

    public T FindState<T>(eStateType _eType) where T : PlayerState
    {
        if (m_hashState.TryGetValue(_eType, out PlayerState refState) == false)
            return null;

        return refState as T;
    }

    public AnimatorEvent FindAnimEvent(eStateType _eType)
    {
        return m_hashAnimEvent[_eType];
    }

    public void ClearGroundContact()
    {
        m_refGroundCollider = null;
    }

    private void OnCollisionEnter(Collision _refCollision)
    {
        UpdateGroundContact(_refCollision);
    }
    private void OnCollisionStay(Collision _refCollision)
    {
        UpdateGroundContact(_refCollision);
    }

    private void OnCollisionExit(Collision _refCollision)
    {
        if (_refCollision.collider == m_refGroundCollider)
            ClearGroundContact();
    }

    private void UpdateGroundContact(Collision _refCollision)
    {
        bool bGroundContact = false;
        if ((m_iGroundLayers.value & (1 << _refCollision.gameObject.layer)) != 0
            && Rigidbody.linearVelocity.y <= 0.1f)
        {
            for (int i = 0; i < _refCollision.contactCount; i++)
            {
                if (_refCollision.GetContact(i).normal.y >= 0.5f)
                {
                    bGroundContact = true;
                    break;
                }
            }
        }

        if (bGroundContact)
            m_refGroundCollider = _refCollision.collider;
        else if (_refCollision.collider == m_refGroundCollider)
            ClearGroundContact();
    }
}
