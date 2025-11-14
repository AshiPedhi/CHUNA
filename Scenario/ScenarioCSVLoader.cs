using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CSV 파일에서 시나리오 데이터를 로드하는 클래스
/// </summary>
public class ScenarioCSVLoader : MonoBehaviour
{
    /// <summary>
    /// Resources 폴더에서 CSV 파일 로드
    /// </summary>
    public ScenarioCollection LoadScenarios(string csvFileName = "ScenarioData")
    {
        TextAsset csvFile = Resources.Load<TextAsset>($"Scenarios/{csvFileName}");

        if (csvFile == null)
        {
            Debug.LogError($"[ScenarioLoader] CSV 파일을 찾을 수 없습니다: Resources/Scenarios/{csvFileName}.csv");
            Debug.LogError($"[ScenarioLoader] CSV 파일을 Assets/Resources/Scenarios/ 폴더에 넣어주세요!");
            return null;
        }

        return ParseCSV(csvFile.text);
    }

    /// <summary>
    /// CSV 텍스트를 파싱하여 ScenarioCollection 생성
    /// </summary>
    private ScenarioCollection ParseCSV(string csvText)
    {
        ScenarioCollection collection = new ScenarioCollection
        {
            scenarios = new List<ScenarioData>()
        };

        string[] lines = csvText.Split('\n');

        // 이전 값들을 기억 (빈칸 채우기용)
        int lastScenarioNo = 0;
        string lastScenarioName = "";
        string lastPhase = "";
        string lastStepName = "";

        ScenarioData currentScenario = null;
        PhaseData currentPhase = null;
        StepData currentStep = null;

        // 첫 줄(헤더) 건너뛰기
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);

            // 최소 8개 (기본) 또는 19개 (환자 움직임 포함) 컬럼 필요
            if (values.Length < 8) continue;

            // 빈칸이면 이전 값 사용
            int scenarioNo = string.IsNullOrEmpty(values[0]) ? lastScenarioNo : int.Parse(values[0].Trim());
            string scenarioName = string.IsNullOrEmpty(values[1]) ? lastScenarioName : values[1].Trim();
            string phase = string.IsNullOrEmpty(values[2]) ? lastPhase : values[2].Trim();
            string stepName = string.IsNullOrEmpty(values[3]) ? lastStepName : values[3].Trim();
            int subStepNo = int.Parse(values[4].Trim());
            int duration = string.IsNullOrEmpty(values[5]) ? 0 : int.Parse(values[5].Trim());
            string textInstruction = values[6].Trim();
            string voiceInstruction = values[7].Trim();

            // Step 번호와 이름 분리 (예: "1.평가" -> stepNo=1, stepName="평가")
            int stepNo;
            string stepDisplayName;
            ParseStepName(stepName, out stepNo, out stepDisplayName);

            // 현재 값 기억
            lastScenarioNo = scenarioNo;
            lastScenarioName = scenarioName;
            lastPhase = phase;
            lastStepName = stepName;

            // === 시나리오 생성/찾기 ===
            if (currentScenario == null || currentScenario.scenarioNo != scenarioNo)
            {
                currentScenario = new ScenarioData
                {
                    scenarioNo = scenarioNo,
                    scenarioName = scenarioName
                };
                collection.scenarios.Add(currentScenario);
                currentPhase = null;
                currentStep = null;
            }

            // === Phase 생성/찾기 ===
            if (currentPhase == null || currentPhase.phaseName != phase)
            {
                currentPhase = currentScenario.phases.FirstOrDefault(p => p.phaseName == phase);

                if (currentPhase == null)
                {
                    currentPhase = new PhaseData { phaseName = phase };
                    currentScenario.phases.Add(currentPhase);
                }

                currentStep = null;
            }

            // === Step 생성/찾기 ===
            if (currentStep == null || currentStep.stepNo != stepNo)
            {
                currentStep = currentPhase.steps.FirstOrDefault(s => s.stepNo == stepNo);

                if (currentStep == null)
                {
                    currentStep = new StepData
                    {
                        stepNo = stepNo,
                        stepName = stepDisplayName
                    };
                    currentPhase.steps.Add(currentStep);
                }
            }

            // === SubStep 추가 ===
            SubStepData subStep = new SubStepData
            {
                subStepNo = subStepNo,
                duration = duration,
                textInstruction = textInstruction,
                voiceInstruction = voiceInstruction,
                isSameToPrevious = voiceInstruction == "상동"
            };

            // 환자 움직임 파일명 파싱 (컬럼이 9개 이상인 경우)
            if (values.Length >= 9)
            {
                // 8: PatientMotionFileName
                subStep.patientMotionFileName = values[8].Trim();
            }

            // 핸드 트래킹 파일명 파싱 (컬럼이 10개 이상인 경우)
            if (values.Length >= 10)
            {
                // 9: HandTrackingFileName
                subStep.handTrackingFileName = values[9].Trim();
            }

            currentStep.subSteps.Add(subStep);
        }

        // Phase와 Step 정렬
        foreach (var scenario in collection.scenarios)
        {
            foreach (var phase in scenario.phases)
            {
                phase.steps = phase.steps.OrderBy(s => s.stepNo).ToList();
            }
        }

        Debug.Log($"[ScenarioLoader] 총 {collection.scenarios.Count}개 시나리오 로드 완료");

        return collection;
    }

    /// <summary>
    /// Step 이름을 파싱 (예: "1.평가" -> stepNo=1, stepName="평가")
    /// </summary>
    private void ParseStepName(string stepName, out int stepNo, out string stepDisplayName)
    {
        if (stepName == "가이드")
        {
            stepNo = 0;
            stepDisplayName = "가이드";
            return;
        }

        string[] parts = stepName.Split('.');

        if (parts.Length == 2)
        {
            stepNo = int.Parse(parts[0]);
            stepDisplayName = parts[1];
        }
        else
        {
            stepNo = 0;
            stepDisplayName = stepName;
        }
    }

    /// <summary>
    /// CSV 라인 파싱 (따옴표 안의 쉼표 처리)
    /// </summary>
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentValue = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue);
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }

        result.Add(currentValue);

        return result.ToArray();
    }

    /// <summary>
    /// 문자열을 float로 파싱 (실패 시 기본값 반환)
    /// </summary>
    private float ParseFloat(string value, float defaultValue)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
            return defaultValue;

        float result;
        if (float.TryParse(value.Trim(), out result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// 문자열을 bool로 파싱 (TRUE/FALSE, true/false, 1/0)
    /// </summary>
    private bool ParseBool(string value, bool defaultValue)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
            return defaultValue;

        string trimmed = value.Trim().ToUpper();

        if (trimmed == "TRUE" || trimmed == "1" || trimmed == "YES")
            return true;

        if (trimmed == "FALSE" || trimmed == "0" || trimmed == "NO")
            return false;

        return defaultValue;
    }
}