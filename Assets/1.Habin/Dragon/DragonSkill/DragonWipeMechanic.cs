using Photon.Pun;
using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonWipeMechanic : MonoBehaviourPun
{
    private enum MechanicPhase
    {
        None,
        Flying,
        Altars,
        Landing,
        Groggy,
        FailureDamage,
        Finished
    }

    [Header("참조")]
    [SerializeField] private DragonBoss boss;
    [SerializeField] private AltarDestroyCounter altarCounter;
    [SerializeField] private BehaviorGraphAgent behaviorAgent;
    [SerializeField] private Animator animator;

    [Header("중단할 일반 스킬")]
    [SerializeField] private MonoBehaviour[] skillScripts;

    [Header("시작 체력 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float triggerHpRatio = 0.5f;

    [Header("중앙 상공 이동")]
    [SerializeField] private Transform arenaCenter;

    [Min(0f)]
    [SerializeField] private float flightHeight = 20f;

    [Min(0.01f)]
    [SerializeField] private float flightSpeed = 8f;

    [SerializeField] private string flightState = "Base Layer.Fly Idle";

    [Header("미리 배치한 제단 4개")]
    [SerializeField] private PowerAltar[] altars;

    [Header("빔 도착점")]
    [SerializeField] private Transform energyTarget;

    [Header("제한 시간")]
    [Min(0.01f)]
    [SerializeField] private float timeLimit = 60f;

    [Header("성공 후 착지")]
    [Min(0.01f)]
    [SerializeField] private float landingSpeed = 12f;

    [Header("그로기")]
    [Min(0.01f)]
    [SerializeField] private float groggyDuration = 5f;

    [SerializeField] private string groggyState = "Base Layer.Rise";
    [SerializeField] private string idleState = "Base Layer.Idle";

    [Header("기믹 성공 후 공격력")]
    [Min(1f)]
    [SerializeField] private float successAttackMultiplier = 1.5f;

    [Header("기믹 실패 시 피해")]
    [Min(1f)]
    [SerializeField] private float failureDamage = 100000f;

    [Min(0.01f)]
    [SerializeField] private float failureDamageInterval = 0.5f;

    private MechanicPhase phase;
    private Vector3 flightDestination;
    private Vector3 landingDestination;

    private bool hasStarted;
    private bool resultHandled;
    private bool attackBoostApplied;
    private bool[] previousSkillEnabled;

    private int requiredCount;
    private double deadline;
    private double groggyDeadline;
    private double nextFailureDamageTime;

    public bool IsActive { get; private set; }

    public float RemainingTime => phase == MechanicPhase.Altars ? Mathf.Max(0f, (float)(deadline - PhotonNetwork.Time)) : 0f;

    private void Awake()
    {
        if (boss == null)
            boss = GetComponent<DragonBoss>();

        if (behaviorAgent == null)
            behaviorAgent = GetComponent<BehaviorGraphAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (energyTarget == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name == "EnergyTarget")
                {
                    energyTarget = child;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        ConnectBeams();
        SetAltarsVisible(false);
    }

    private void OnEnable()
    {
        if (boss != null)
            boss.OnHpChanged += CheckStartCondition;
    }

    private void OnDisable()
    {
        if (boss != null)
            boss.OnHpChanged -= CheckStartCondition;
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (!IsActive || boss == null || boss.IsDead)
            return;

        switch (phase)
        {
            case MechanicPhase.Flying:
                MoveToCenter();
                break;

            case MechanicPhase.Altars:
                CheckAltarResult();
                break;

            case MechanicPhase.Landing:
                MoveToGround();
                break;

            case MechanicPhase.Groggy:
                if (PhotonNetwork.Time >= groggyDeadline)
                {
                    EndGroggy();
                    photonView.RPC(nameof(ReceiveGroggyEnd), RpcTarget.Others);
                }
                break;

            case MechanicPhase.FailureDamage:
                if (PhotonNetwork.Time >= nextFailureDamageTime)
                {
                    nextFailureDamageTime = PhotonNetwork.Time + failureDamageInterval;
                    DamageAllPlayers();
                }
                break;
        }
    }

    private void CheckStartCondition()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (hasStarted || boss.IsDead || boss.MaxHp <= 0f)
            return;

        if (boss.CurrentHp > boss.MaxHp * triggerHpRatio)
            return;

        if (!ValidateReferences())
            return;

        int playerCount = Mathf.Clamp(PhotonNetwork.CurrentRoom.PlayerCount, 1, 4);

        BeginMechanic(playerCount);
        photonView.RPC(nameof(ReceiveMechanicStart), RpcTarget.Others, playerCount);
    }

    private bool ValidateReferences()
    {
        if (altarCounter == null || arenaCenter == null || energyTarget == null)
        {
            Debug.LogError("Altar Counter, Arena Center, Energy Target 연결을 확인하세요.", this);
            return false;
        }

        if (altars == null || altars.Length != 4)
        {
            Debug.LogError("Altars에 제단 4개를 연결하세요.", this);
            return false;
        }

        for (int i = 0; i < altars.Length; i++)
        {
            if (altars[i] == null)
            {
                Debug.LogError($"Altars의 Element {i}가 비어 있습니다.", this);
                return false;
            }

            for (int j = 0; j < i; j++)
            {
                if (altars[i] == altars[j])
                {
                    Debug.LogError("같은 제단이 중복 연결되어 있습니다.", this);
                    return false;
                }
            }
        }

        return true;
    }

    [PunRPC]
    private void ReceiveMechanicStart(int playerCount, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        BeginMechanic(playerCount);
    }

    private void BeginMechanic(int playerCount)
    {
        if (hasStarted)
            return;

        hasStarted = true;
        IsActive = true;
        requiredCount = playerCount;
        phase = MechanicPhase.Flying;

        landingDestination = arenaCenter.position;
        flightDestination = landingDestination + Vector3.up * flightHeight;

        boss.SetInvincible(true);

        if (behaviorAgent != null)
            behaviorAgent.enabled = false;

        DisableNormalSkills();

        if (animator != null)
        {
            animator.speed = 1f;
            animator.applyRootMotion = false;
        }

        PlayAnimation(flightState);

        Debug.Log($"전멸 기믹 시작! 필요한 제단 수: {requiredCount}", this);
    }

    private void MoveToCenter()
    {
        transform.position = Vector3.MoveTowards(transform.position, flightDestination, flightSpeed * Time.deltaTime);

        if ((transform.position - flightDestination).sqrMagnitude > 0.0001f)
            return;

        transform.position = flightDestination;

        altarCounter.ResetCount();

        double endTime = PhotonNetwork.Time + timeLimit;

        BeginAltarPhase(endTime);
        photonView.RPC(nameof(ReceiveAltarPhaseStart), RpcTarget.Others, endTime);
    }

    [PunRPC]
    private void ReceiveAltarPhaseStart(double endTime, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        BeginAltarPhase(endTime);
    }

    private void BeginAltarPhase(double endTime)
    {
        phase = MechanicPhase.Altars;
        deadline = endTime;

        ConnectBeams();
        SetAltarsVisible(true);

        Debug.Log($"제단 활성화! 제한 시간: {RemainingTime:F1}초", this);
    }

    private void CheckAltarResult()
    {
        if (PhotonNetwork.Time >= deadline)
        {
            FinishAltarPhase(false);
            return;
        }

        if (altarCounter.DestroyedCount >= requiredCount)
            FinishAltarPhase(true);
    }

    private void FinishAltarPhase(bool success)
    {
        if (phase != MechanicPhase.Altars || resultHandled)
            return;

        float multiplier = successAttackMultiplier;

        ApplyAltarResult(success, multiplier);
        photonView.RPC(nameof(ReceiveAltarResult), RpcTarget.Others, success, multiplier);
    }

    [PunRPC]
    private void ReceiveAltarResult(bool success, float multiplier, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        ApplyAltarResult(success, multiplier);
    }

    private void ApplyAltarResult(bool success, float multiplier)
    {
        if (resultHandled)
            return;

        resultHandled = true;
        SetAltarsVisible(false);

        if (!success)
        {
            phase = MechanicPhase.FailureDamage;
            nextFailureDamageTime = PhotonNetwork.Time;

            Debug.Log("기믹 실패! 전원 사망할 때까지 반복 피해 시작.", this);
            return;
        }

        if (!attackBoostApplied)
        {
            attackBoostApplied = true;
            boss.MultiplyAttack(multiplier);
        }

        phase = MechanicPhase.Landing;

        Debug.Log("기믹 성공! 공격력 증가 및 착지 시작.", this);
    }

    private void MoveToGround()
    {
        transform.position = Vector3.MoveTowards(transform.position, landingDestination, landingSpeed * Time.deltaTime);

        if ((transform.position - landingDestination).sqrMagnitude > 0.0001f)
            return;

        transform.position = landingDestination;

        double endTime = PhotonNetwork.Time + groggyDuration;

        BeginGroggy(endTime);
        photonView.RPC(nameof(ReceiveGroggyStart), RpcTarget.Others, endTime);
    }

    [PunRPC]
    private void ReceiveGroggyStart(double endTime, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        BeginGroggy(endTime);
    }

    private void BeginGroggy(double endTime)
    {
        phase = MechanicPhase.Groggy;
        groggyDeadline = endTime;

        boss.SetInvincible(false);
        PlayAnimation(groggyState);

        Debug.Log("착지 완료! Rise 애니메이션 및 그로기 시작.", this);
    }

    [PunRPC]
    private void ReceiveGroggyEnd(PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        EndGroggy();
    }

    private void EndGroggy()
    {
        phase = MechanicPhase.Finished;
        IsActive = false;

        if (boss == null || boss.IsDead)
            return;

        RestoreNormalSkills();
        PlayAnimation(idleState);

        if (behaviorAgent != null)
        {
            behaviorAgent.enabled = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;

            if (behaviorAgent.enabled)
                behaviorAgent.Restart();
        }

        Debug.Log("그로기 종료! 증가한 공격력으로 일반 공격 재개.", this);
    }

    private void DamageAllPlayers()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        bool hasAlivePlayer = false;

        foreach (Player player in players)
        {
            if (player == null || !player.IsAlive)
                continue;

            player.TakeDamage(new tAttackInfo(transform.position, failureDamage));

            // 피해를 받고도 살아 있으면 다음 간격에 다시 피해를 줍니다.
            if (player.IsAlive)
                hasAlivePlayer = true;
        }

        if (hasAlivePlayer)
            return;

        EndFailureDamage();
        photonView.RPC(nameof(ReceiveWipeEnd), RpcTarget.Others);
    }

    [PunRPC]
    private void ReceiveWipeEnd(PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        EndFailureDamage();
    }

    private void EndFailureDamage()
    {
        phase = MechanicPhase.Finished;

        // 전멸 후 보스는 상공에서 무적·AI 중단 상태를 유지합니다.
        Debug.Log("생존 플레이어 없음. 반복 피해 종료.", this);
    }

    private void DisableNormalSkills()
    {
        if (skillScripts == null)
            return;

        previousSkillEnabled = new bool[skillScripts.Length];

        for (int i = 0; i < skillScripts.Length; i++)
        {
            MonoBehaviour skill = skillScripts[i];

            if (skill == null)
                continue;

            previousSkillEnabled[i] = skill.enabled;
            skill.enabled = false;
        }
    }

    private void RestoreNormalSkills()
    {
        if (skillScripts == null || previousSkillEnabled == null)
            return;

        for (int i = 0; i < skillScripts.Length && i < previousSkillEnabled.Length; i++)
        {
            if (skillScripts[i] != null)
                skillScripts[i].enabled = previousSkillEnabled[i];
        }
    }

    private void PlayAnimation(string stateName)
    {
        if (animator == null)
            return;

        int stateHash = Animator.StringToHash(stateName);

        if (animator.HasState(0, stateHash))
            animator.CrossFadeInFixedTime(stateHash, 0.2f);
        else
            Debug.LogWarning($"애니메이션 상태가 없습니다: {stateName}", this);
    }

    private void ConnectBeams()
    {
        if (altars == null)
            return;

        foreach (PowerAltar altar in altars)
        {
            if (altar == null)
                continue;

            foreach (EnergyBeam beam in altar.GetComponentsInChildren<EnergyBeam>(true))
                beam.SetTarget(energyTarget);
        }
    }

    private void SetAltarsVisible(bool visible)
    {
        if (altars == null)
            return;

        foreach (PowerAltar altar in altars)
        {
            if (altar != null)
                altar.gameObject.SetActive(visible);
        }
    }
}