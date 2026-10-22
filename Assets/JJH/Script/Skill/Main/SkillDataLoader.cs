using Newtonsoft.Json;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Constant_JJH;

public class SkillDataLoader : MonoBehaviour
{
    public static SkillDataLoader Instance;

    public Dictionary<int, SkillData> SkillData;
    public List<SkillData> SkillDataList;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        SkillDataList = new();
        SkillData = new();

        ResourceLoad();
    }

    public void ResourceLoad()
    {
        string skillJson = "";

        try
        {
            skillJson = Resources.Load<TextAsset>(ASSET_DATA_PATH + SKILL_DATA).text;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataLoad] 스킬 데이터 로드 실패 : {e.Message}");
        }

        try
        {
            // 리스트 변환
            SkillDataList = JsonConvert.DeserializeObject<List<SkillData>>(skillJson);

            // 딕셔너리 가공
            SkillData = SkillDataList?.ToDictionary(x => x.Id) ?? new Dictionary<int, SkillData>();
        }
        catch (JsonException jsonEx)
        {
            // JSON 문법(쉼표 누락, 중괄호 미닫힘 등) 오류 발생 시
            Debug.LogError($"[JSON Parsing] JSON 데이터 파싱 중 문법 오류 발생: {jsonEx.Message}");
        }
        catch (Exception e)
        {
            // 이외 런타임 에러
            Debug.LogError($"[DataProcessing] 데이터 변환 및 딕셔너리 구성 중 오류 발생: {e.Message}");
        }

    }
}
