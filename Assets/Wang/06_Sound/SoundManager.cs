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
       풀이 가득 차면 우선순위가 같거나 낮은 기존 소리를 정지하고 교체한다.
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

    [Header("Mixer")]
    [SerializeField] private AudioMixer m_refMixer;

    [Header("BGM")]
    [SerializeField] private AudioSource m_refBgmSource;

    [Header("SFX Pool")]
    [SerializeField] private int m_iSfxPoolCount = 10;                 // 풀링할 개수
    [SerializeField] private AudioSource m_refSfxPrefab;              // 3D 감쇠 설정(rolloff 등)을 프리팹에서 조정. 없으면 기본 AudioSource

    private AudioSource[] m_arrSfxSource;
    private int[] m_arrPriority;                                       // 소스별 현재 재생 중인 소리의 우선순위 (뺏기 판단)

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

        int iIdx = FindSourceIdx(_refAudio.Priority);
        if (iIdx < 0)
            return default;   // 전부 나보다 중요한 소리가 재생 중 - 이번 소리는 버림

        AudioSource refSrc = m_arrSfxSource[iIdx];
        refSrc.Stop();
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

    // 1) 쉬는 소스 2) 없으면 나와 같거나 덜 중요한 소스 중 가장 덜 중요한 것 3) 그것도 없으면 -1
    private int FindSourceIdx(int _iPriority)
    {
        int iStealIdx = -1;
        int iWorstPriority = _iPriority;

        for (int i = 0; i < m_arrSfxSource.Length; ++i)
        {
            AudioSource refSrc = m_arrSfxSource[i];
            if (refSrc.isPlaying == false)
                return i;

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
