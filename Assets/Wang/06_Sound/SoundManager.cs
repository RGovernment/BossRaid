using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/*///////////////////////////////////////////
                eAudioChannelType
목적 : AudioMixer 볼륨 채널 구분 (옵션 슬라이더 → SoundManager.SetVolume)
 *///////////////////////////////////////////
public enum eAudioChannelType
{
    BGM,
    SFX,
    UI,
    Master
}


/*///////////////////////////////////////////
                  SoundManager
목적 : BGM 1채널 + SFX AudioSource 풀 재생.
       탄 4000발 상황 대비 - 같은 SO 최소 간격 / 저우선순위 프레임당 개수 제한 / 우선순위 기반 소스 뺏기로
       중요한 소리(폭발, 보스)가 연사음에 끊기지 않게 한다
 *///////////////////////////////////////////
[DisallowMultipleComponent]
public sealed class SoundManager : MonoBehaviour
{
    // 재생한 풀 소스를 나중에 멈출 때 쓰는 핸들.
    public readonly struct SoundHandle
    {
        public readonly int Index;
        public bool IsValid { get; }

        public SoundHandle(int _iIndex)
        {
            Index = _iIndex;
            IsValid = true;
        }
    }

    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string UI_VOLUME_PARAM = "UIVolume";
    private const float MIN_DB = -80.0f;
    private const int LOW_PRIORITY_THRESHOLD = 128;   // 이보다 덜 중요한(값이 큰) 소리만 프레임당 개수 제한

    [Header("Mixer")]
    [SerializeField] private AudioMixer m_refMixer;

    [Header("BGM")]
    [SerializeField] private AudioSource m_refBgmSource;

    [Header("SFX Pool")]
    [SerializeField] private int m_iSfxPoolCount = 10;                 // 풀링할 개수
    [SerializeField] private AudioSource m_refSfxPrefab;              // 3D 감쇠 설정(rolloff 등)을 프리팹에서 조정. 없으면 기본 AudioSource
    [SerializeField] private int m_iMaxLowPriorityPerFrame = 4;

    private AudioSource[] m_arrSfxSource;
    private int[] m_arrPriority;                                       // 소스별 현재 재생 중인 소리의 우선순위 (뺏기 판단)
    private readonly Dictionary<SOAudio, float> m_hashLastPlayTime = new();
    private int m_iLowPriorityFrame = -1;
    private int m_iLowPriorityCount;

    public static SoundManager m_Instance { get; private set; }

    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSfxPool();
    }

    private void OnDestroy()
    {
        if (m_Instance == this)
            m_Instance = null;
    }

    public void PlayBgm(SOAudio _refAudio)
    {
        if (_refAudio == null)
            return;

        AudioClip refClip = _refAudio.GetRandomClip();
        if (refClip == null)
            return;

        // 스테이지마다 같은 BGM을 요청해도 처음부터 다시 틀지 않음
        if (m_refBgmSource.isPlaying == true && m_refBgmSource.clip == refClip)
            return;

        m_refBgmSource.outputAudioMixerGroup = _refAudio.OutputGroup;
        m_refBgmSource.clip = refClip;
        m_refBgmSource.loop = true;
        m_refBgmSource.volume = _refAudio.Volume;
        m_refBgmSource.pitch = 1.0f;
        m_refBgmSource.spatialBlend = 0.0f;
        m_refBgmSource.Play();
    }

    public void StopBgm()
    {
        m_refBgmSource.Stop();
        m_refBgmSource.clip = null;
    }

    // 2D (UI 등 위치 무관)
    public SoundHandle PlaySfx(SOAudio _refAudio) => PlaySfx(_refAudio, Vector3.zero);

    // Transform이 아니라 위치값을 받는다 - 풀에 반납됐거나 파괴된 오브젝트의 Transform이 넘어오는 사고 방지.
    // SpatialBlend가 0인 SO면 위치는 무시됨
    public SoundHandle PlaySfx(SOAudio _refAudio, Vector3 _vPos)
    {
        if (_refAudio == null)
            return default;

        AudioClip refClip = _refAudio.GetRandomClip();
        if (refClip == null)
            return default;

        if (IsThrottled(_refAudio) == true)
            return default;

        int iIdx = FindSourceIdx(_refAudio.Priority);
        if (iIdx < 0)
            return default;   // 전부 나보다 중요한 소리나 루프가 재생 중 - 이번 소리는 버림

        // unscaledTime: 레벨업 카드 선택 중 timeScale 0이어도 UI 소리 간격이 정상 계산되도록
        m_hashLastPlayTime[_refAudio] = Time.unscaledTime;
        if (_refAudio.Priority > LOW_PRIORITY_THRESHOLD)
            ++m_iLowPriorityCount;

        AudioSource refSrc = m_arrSfxSource[iIdx];
        m_arrPriority[iIdx] = _refAudio.Priority;

        refSrc.clip = refClip;
        refSrc.outputAudioMixerGroup = _refAudio.OutputGroup;
        refSrc.volume = _refAudio.Volume;
        refSrc.pitch = _refAudio.GetRandomPitch();
        refSrc.loop = _refAudio.Loop;
        refSrc.priority = _refAudio.Priority;      // Unity 자체 보이스 컬링도 같은 기준으로
        refSrc.spatialBlend = _refAudio.SpatialBlend;
        if (_refAudio.SpatialBlend > 0.0f)
            refSrc.transform.position = _vPos;

        refSrc.Play();
        return new SoundHandle(iIdx);
    }

    public void StopSfx(SoundHandle _tHandle)
    {
        if (!_tHandle.IsValid || (uint)_tHandle.Index >= (uint)m_arrSfxSource.Length)
            return;

        // shortcut: 재사용 후에는 현재 소리를 정지하므로, 재생별 식별이 필요해지면 보완.
        m_arrSfxSource[_tHandle.Index].Stop();
    }

    // 옵션 UI 슬라이더(0~1)에서 호출. Slider 타입에 의존하지 않도록 값만 받음
    public void SetVolume(eAudioChannelType _eType, float _fLinear)
    {
        float fDB = _fLinear <= 0.0001f ? MIN_DB : Mathf.Log10(_fLinear) * 20.0f;
        m_refMixer.SetFloat(GetVolumeParam(_eType), fDB);
    }

    private void BuildSfxPool()
    {
        m_arrSfxSource = new AudioSource[m_iSfxPoolCount];
        m_arrPriority = new int[m_iSfxPoolCount];

        for (int i = 0; i < m_iSfxPoolCount; ++i)
        {
            AudioSource refSrc;
            if (m_refSfxPrefab != null)
                refSrc = Instantiate(m_refSfxPrefab, transform);
            else
            {
                GameObject refObj = new GameObject("SFX_" + i);   // 초기화 1회뿐이라 문자열 할당 무관
                refObj.transform.SetParent(transform, false);
                refSrc = refObj.AddComponent<AudioSource>();
            }

            refSrc.playOnAwake = false;
            refSrc.loop = false;
            m_arrSfxSource[i] = refSrc;
        }
    }

    private bool IsThrottled(SOAudio _refAudio)
    {
        if (_refAudio.MinInterval > 0.0f
            && m_hashLastPlayTime.TryGetValue(_refAudio, out float fLastTime) == true
            && Time.unscaledTime - fLastTime < _refAudio.MinInterval)
            return true;

        if (_refAudio.Priority <= LOW_PRIORITY_THRESHOLD)
            return false;   // 중요한 소리는 프레임 한도에 안 걸림 - 피격음 4개가 먼저 와도 폭발음은 재생

        if (m_iLowPriorityFrame != Time.frameCount)
        {
            m_iLowPriorityFrame = Time.frameCount;
            m_iLowPriorityCount = 0;
        }
        return m_iLowPriorityCount >= m_iMaxLowPriorityPerFrame;
    }

    // 1) 쉬는 소스 2) 없으면 나와 같거나 덜 중요한 비루프 소스 중 가장 덜 중요한 것 3) 그것도 없으면 -1
    private int FindSourceIdx(int _iPriority)
    {
        int iStealIdx = -1;
        int iWorstPriority = _iPriority;

        for (int i = 0; i < m_arrSfxSource.Length; ++i)
        {
            AudioSource refSrc = m_arrSfxSource[i];
            if (refSrc.isPlaying == false)
                return i;

            if (refSrc.loop == true)
                continue;   // 레이저 루프 등은 핸들로만 멈춘다

            if (m_arrPriority[i] >= iWorstPriority)
            {
                iWorstPriority = m_arrPriority[i];
                iStealIdx = i;
            }
        }
        return iStealIdx;
    }

    private static string GetVolumeParam(eAudioChannelType _eType) => _eType switch
    {
        eAudioChannelType.Master => MASTER_VOLUME_PARAM,
        eAudioChannelType.BGM => BGM_VOLUME_PARAM,
        eAudioChannelType.UI => UI_VOLUME_PARAM,
        _ => SFX_VOLUME_PARAM,
    };
}
