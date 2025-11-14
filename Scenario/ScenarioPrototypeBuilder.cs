using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inspector에서 프로토타입 시나리오를 쉽게 생성하는 헬퍼
/// </summary>
public class ScenarioPrototypeBuilder : MonoBehaviour
{
    [Header("=== 빠른 프로토타입 생성 ===")]
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private IntegratedChunaTrainingSystem integratedSystem;
    
    [Header("=== 템플릿 선택 ===")]
    [SerializeField] private TemplateType selectedTemplate = TemplateType.UpperTrapezius;
    
    public enum TemplateType
    {
        UpperTrapezius,    // 상부승모근
        Custom,            // 사용자 정의
        Simple             // 단순 테스트용
    }
    
    [ContextMenu("🔨 템플릿으로 프로토타입 생성")]
    public void BuildPrototypeFromTemplate()
    {
        if (scenarioManager == null)
        {
            Debug.LogError("ScenarioManager가 연결되지 않았습니다!");
            return;
        }
        
        ScenarioData prototype = null;
        
        switch (selectedTemplate)
        {
            case TemplateType.UpperTrapezius:
                prototype = CreateUpperTrapeziusTemplate();
                break;
            case TemplateType.Simple:
                prototype = CreateSimpleTemplate();
                break;
            case TemplateType.Custom:
                prototype = CreateCustomTemplate();
                break;
        }
        
        if (prototype != null)
        {
            // ScenarioManager에 설정 (리플렉션 사용)
            SetPrototypeData(prototype);
            Debug.Log($"✅ '{prototype.scenarioName}' 프로토타입 생성 완료!");
        }
    }
    
    /// <summary>
    /// 상부승모근 템플릿
    /// </summary>
    private ScenarioData CreateUpperTrapeziusTemplate()
    {
        var scenario = new ScenarioData
        {
            scenarioNo = 1,
            scenarioName = "상부승모근",
            phases = new List<PhaseData>()
        };
        
        // Phase 1: 평가
        var evaluationPhase = new PhaseData
        {
            phaseName = "평가",
            steps = new List<StepData>()
        };
        
        // 가이드 스텝
        evaluationPhase.steps.Add(new StepData
        {
            stepNo = 0,
            stepName = "가이드",
            subSteps = new List<SubStepData>
            {
                new SubStepData
                {
                    subStepNo = 1,
                    duration = 0,
                    voiceInstruction = "평가를 시작합니다. 환자의 어깨를 확인해주세요.",
                    textInstruction = "평가 준비"
                }
            }
        });
        
        // 평가 스텝
        evaluationPhase.steps.Add(new StepData
        {
            stepNo = 1,
            stepName = "평가",
            subSteps = new List<SubStepData>
            {
                new SubStepData
                {
                    subStepNo = 1,
                    duration = 5,
                    voiceInstruction = "환자의 상부승모근을 촉진합니다.",
                    textInstruction = "촉진 위치 확인",
                    customAction = "CheckPosition"
                },
                new SubStepData
                {
                    subStepNo = 2,
                    duration = 3,
                    voiceInstruction = "근육의 긴장도를 평가합니다.",
                    textInstruction = "긴장도 평가"
                }
            }
        });
        
        scenario.phases.Add(evaluationPhase);
        
        // Phase 2: 중부 치료
        var treatmentPhase = new PhaseData
        {
            phaseName = "중부",
            steps = new List<StepData>()
        };
        
        // 세판상박회인 스텝
        treatmentPhase.steps.Add(new StepData
        {
            stepNo = 1,
            stepName = "세판상박회인",
            subSteps = new List<SubStepData>
            {
                new SubStepData
                {
                    subStepNo = 1,
                    duration = 10,
                    voiceInstruction = "관절 가동 범위를 확인합니다.",
                    textInstruction = "ROM 테스트"
                }
            }
        });
        
        // 등척성운동 스텝
        treatmentPhase.steps.Add(new StepData
        {
            stepNo = 2,
            stepName = "등척성운동",
            subSteps = new List<SubStepData>
            {
                new SubStepData
                {
                    subStepNo = 1,
                    duration = 5,
                    voiceInstruction = "환자가 저항을 유지하도록 지시합니다.",
                    textInstruction = "등척성 수축",
                    customAction = "StartHaptic"
                },
                new SubStepData
                {
                    subStepNo = 2,
                    duration = 5,
                    voiceInstruction = "5초간 유지합니다.",
                    textInstruction = "유지"
                }
            }
        });
        
        scenario.phases.Add(treatmentPhase);
        
        return scenario;
    }
    
    /// <summary>
    /// 단순 테스트 템플릿
    /// </summary>
    private ScenarioData CreateSimpleTemplate()
    {
        return new ScenarioData
        {
            scenarioNo = 99,
            scenarioName = "테스트 시나리오",
            phases = new List<PhaseData>
            {
                new PhaseData
                {
                    phaseName = "테스트",
                    steps = new List<StepData>
                    {
                        new StepData
                        {
                            stepNo = 1,
                            stepName = "테스트동작",
                            subSteps = new List<SubStepData>
                            {
                                new SubStepData
                                {
                                    subStepNo = 1,
                                    duration = 3,
                                    voiceInstruction = "테스트 동작입니다.",
                                    textInstruction = "테스트"
                                }
                            }
                        }
                    }
                }
            }
        };
    }
    
    /// <summary>
    /// 커스텀 템플릿 (비어있는 구조)
    /// </summary>
    private ScenarioData CreateCustomTemplate()
    {
        return new ScenarioData
        {
            scenarioNo = 1,
            scenarioName = "커스텀 시나리오",
            phases = new List<PhaseData>
            {
                new PhaseData
                {
                    phaseName = "Phase 1",
                    steps = new List<StepData>
                    {
                        new StepData
                        {
                            stepNo = 1,
                            stepName = "Step 1",
                            subSteps = new List<SubStepData>
                            {
                                new SubStepData
                                {
                                    subStepNo = 1,
                                    duration = 0,
                                    voiceInstruction = "내용을 입력하세요",
                                    textInstruction = ""
                                }
                            }
                        }
                    }
                }
            }
        };
    }
    
    /// <summary>
    /// ScenarioManager에 프로토타입 데이터 설정
    /// </summary>
    private void SetPrototypeData(ScenarioData data)
    {
        // 리플렉션으로 private 필드 접근
        var prototypeField = scenarioManager.GetType().GetField("prototypeScenario",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (prototypeField != null)
        {
            prototypeField.SetValue(scenarioManager, data);
        }
        
        // CSV 사용 비활성화
        var useCSVField = scenarioManager.GetType().GetField("useCSVData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (useCSVField != null)
        {
            useCSVField.SetValue(scenarioManager, false);
        }
        
#if UNITY_EDITOR
        // Inspector 업데이트
        UnityEditor.EditorUtility.SetDirty(scenarioManager);
#endif
    }
    
    [ContextMenu("📊 현재 프로토타입 정보 출력")]
    public void PrintCurrentPrototype()
    {
        var prototypeField = scenarioManager.GetType().GetField("prototypeScenario",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (prototypeField != null)
        {
            var data = prototypeField.GetValue(scenarioManager) as ScenarioData;
            
            if (data != null)
            {
                Debug.Log($"=== 프로토타입: {data.scenarioName} ===");
                Debug.Log($"Phase 수: {data.phases.Count}");
                
                foreach (var phase in data.phases)
                {
                    Debug.Log($"  📁 {phase.phaseName}");
                    
                    foreach (var step in phase.steps)
                    {
                        Debug.Log($"    📄 Step {step.stepNo}: {step.stepName} ({step.subSteps.Count} SubSteps)");
                        
                        foreach (var subStep in step.subSteps)
                        {
                            Debug.Log($"      ▶ SubStep {subStep.subStepNo}: {subStep.voiceInstruction}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("프로토타입 데이터가 없습니다!");
            }
        }
    }
    
    [ContextMenu("🚀 프로토타입으로 바로 시작")]
    public void StartWithPrototype()
    {
        if (integratedSystem != null)
        {
            integratedSystem.StartScenario();
        }
        else if (scenarioManager != null)
        {
            scenarioManager.StartScenario();
        }
    }
}
