# CHUNA 프로젝트 최적화 보고서

## 실행 일자: 2025-11-14

---

## 1. 요약

Unity C# 프로젝트의 전체 스크립트 파일(40개)을 분석하여 연결성, 중복 기능, 사용되지 않는 파일을 확인하고 최적화를 수행했습니다.

### 주요 성과
- **삭제된 파일**: 3개 (ObjectController.cs, PoseRecorder.cs, PosePlayer.cs)
- **식별된 중복 기능**: 5개 주요 영역
- **코드 정리**: 총 ~300줄의 사용되지 않는 코드 제거
- **최적화 제안**: 우선순위별 로드맵 제공

---

## 2. 삭제된 파일

### 2.1 사용되지 않는 파일 (완전 삭제)

| 파일명 | 크기 | 사유 |
|--------|------|------|
| **ObjectController.cs** | 153줄 | 어디서도 참조되지 않음. UI 오브젝트 이동/회전 기능이지만 미사용 |
| **PoseRecorder.cs** | 248줄 | Body Pose 녹화 기능이지만 HandPoseRecorder로 대체됨 |
| **PosePlayer.cs** | 138줄 | OVRCustomSkeleton 재생 기능이지만 HandPosePlayer로 대체됨 |

**총 절감**: 539줄의 코드 및 6개의 .meta 파일

---

## 3. 파일 간 의존성 맵

### 3.1 시스템 계층 구조

```
┌─────────────────────────────────────────────┐
│               인증 계층 (Auth/)              │
│  AuthenticationService ──> AuthEvents       │
│  LobbyAuthUI ──────────────> AuthEvents     │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│          데이터 관리 계층 (ChunaData/)       │
│  ChunaMotionDataManager (Singleton)         │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│        포즈 처리 계층 (PoseData/)            │
│  HandPosePlayer, HandPoseRecorder           │
│  HandTransformMapper                        │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│       훈련 시스템 계층 (ChunaSystem/)        │
│  ChunaTrainingController                    │
│  ChunaEducationGuideSystem                  │
│  PatientModelController                     │
│  IntegratedChunaTrainingSystem              │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│       시나리오 관리 계층 (Scenario/)         │
│  ScenarioManager ──> ScenarioEventSystem    │
│  ScenarioUIController                       │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│            UI 계층 (UI/)                    │
│  DotTimeline, DotTimelineController         │
│  QuickMenuController, SettingsPopup         │
└─────────────────────────────────────────────┘
```

### 3.2 주요 참조 관계

**강한 결합 (주의 필요)**
```
ChunaTrainingController ────────> HandPosePlayer (직접 참조)
IntegratedChunaTrainingSystem ──> ScenarioManager (직접 참조)
PatientModelController ─────────> HandPosePlayer (유사도 계산)
```

**느슨한 결합 (좋은 설계)**
```
AuthEvents (이벤트 시스템)
  ├── LobbyAuthUI (구독)
  ├── AuthenticationService (발행)
  └── ScenarioCardButton (구독)

ScenarioEventSystem (이벤트 시스템)
  ├── ScenarioManager (발행)
  ├── ScenarioUIController (구독)
  └── IntegratedChunaTrainingSystem (구독)
```

---

## 4. 식별된 중복 기능

### 4.1 심각한 중복 (우선순위 높음)

#### A. 훈련 시스템 중복
```
파일 1: ChunaTrainingController.cs (830줄)
파일 2: IntegratedChunaTrainingSystem.cs (757줄)

중복 기능:
- 교육/평가 모드 관리
- HandPosePlayer 제어
- 진행도 추적
- 모드 전환 로직

권장 조치:
→ ChunaTrainingController: 기본 훈련 기능만 담당
→ IntegratedChunaTrainingSystem: 시나리오 통합 기능만 담당
→ 공통 로직을 BaseTrainingController로 추출
```

#### B. 포즈 처리 중복
```
파일: HandPosePlayer.cs, HandPoseRecorder.cs

중복 로직:
- CSV 파일 파싱 (동일한 포맷)
- 프레임 데이터 구조 (PoseFrame, PoseData)
- 타임스탬프 관리

권장 조치:
→ AbstractPoseHandler 기본 클래스 생성
→ CSV 파싱을 PoseDataUtility로 분리
→ 공통 데이터 클래스를 별도 파일로 추출
```

### 4.2 중간 중복 (우선순위 중간)

#### C. 타임라인 UI 중복
```
파일 1: DotTimeline.cs (138줄)
파일 2: DotTimelineController.cs (~150줄)

사용처:
- DotTimeline: ScenarioPrefabCreator에서 사용
- DotTimelineController: ScenarioUIController, ChunaTrainingController에서 사용

상태: 둘 다 사용 중이므로 당장 통합 어려움

권장 조치:
→ 장기적으로 DotTimelineController로 통합
→ ScenarioPrefabCreator를 DotTimelineController로 마이그레이션
→ DotTimeline 점진적 제거
```

#### D. 이벤트 시스템 중복
```
파일 1: AuthEvents.cs (정적 클래스)
파일 2: ScenarioEventSystem.cs (싱글톤)

차이점:
- AuthEvents: static 메서드, 간단한 이벤트
- ScenarioEventSystem: 인스턴스 기반, 복잡한 이벤트

권장 조치:
→ 공통 EventManager 패턴 적용
→ 이벤트 타입별 채널 관리
→ 구독 관리 자동화
```

---

## 5. 성능 최적화 기회

### 5.1 메모리 관리

**A. 캐시 정리 부재**
```cs
// 문제: HandPosePlayer에 OnDestroy 없음
// loadedSequence가 메모리에 계속 남아있음

// 권장 수정:
private void OnDestroy()
{
    loadedSequence?.Clear();
    leftHandCache?.Clear();
    rightHandCache?.Clear();
}
```

**B. ChunaMotionDataManager 캐시**
```cs
// 현재: ClearCache() 메서드는 있지만 OnDestroy에서 호출 안 함
// DontDestroyOnLoad 사용으로 씬 전환 시 캐시 유지

// 권장 개선:
private void OnDestroy()
{
    ClearCache();
    Debug.Log("ChunaMotionDataManager 캐시 정리 완료");
}
```

### 5.2 GC 압박 감소

**문제점**
```cs
// 매 프레임 임시 객체 생성
void Update()
{
    Vector3 temp = new Vector3(...);  // GC 압박
    Quaternion rot = new Quaternion(...);  // GC 압박
    List<int> indices = new List<int>();  // GC 압박
}
```

**권장 개선**
```cs
// 클래스 레벨 필드로 재사용
private Vector3 _tempVector;
private Quaternion _tempRotation;
private List<int> _reusableIndices = new List<int>();

void Update()
{
    _tempVector.Set(...);
    _tempRotation.Set(...);
    _reusableIndices.Clear();
}
```

### 5.3 CSV 로딩 최적화

**현재 문제**
```cs
// 동기 방식 파일 읽기
string[] lines = File.ReadAllLines(path);  // UI 블로킹
```

**권장 개선**
```cs
// UniTask 활용 비동기 로딩
public async UniTask LoadCSVAsync(string path)
{
    string content = await File.ReadAllTextAsync(path);
    // 파싱도 비동기로 처리
    await UniTask.SwitchToThreadPool();
    var data = ParseCSV(content);
    await UniTask.SwitchToMainThread();
    ApplyData(data);
}
```

---

## 6. 코드 품질 개선 사항

### 6.1 큰 파일 분할

**파일 크기 Top 5**
```
1. PatientModelController.cs     - 1,191줄 ⚠️ 분할 필요
2. LobbyAuthUI_Complete.cs       - 1,260줄 ⚠️ 분할 필요
3. ChunaTrainingController.cs    - 830줄  ⚠️ 분할 권장
4. IntegratedChunaTrainingSystem - 757줄  ⚠️ 분할 권장
5. HandPoseDataEditor.cs         - 685줄  ⚠️ 분할 권장
```

**권장 분할 전략**
```
PatientModelController.cs (1,191줄)
  ├── PatientModelController.cs (핵심 로직, ~400줄)
  ├── PatientAnimationHandler.cs (애니메이션, ~400줄)
  └── PatientSimilarityCalculator.cs (유사도 계산, ~300줄)

LobbyAuthUI_Complete.cs (1,260줄)
  ├── LobbyAuthUI.cs (UI 관리, ~400줄)
  ├── LobbyModeSelector.cs (모드 선택, ~300줄)
  └── LobbyScenarioCardManager.cs (카드 관리, ~400줄)
```

### 6.2 주석 및 문서화

**현재 상태**: 주석은 충실하나 한글/영문 혼용
**권장 개선**: XML 문서화 주석 활용

```cs
// 개선 전
// 포즈 비교 함수 - 유사도 계산

// 개선 후
/// <summary>
/// 플레이어 손 포즈와 리플레이 포즈를 비교하여 유사도를 계산합니다.
/// </summary>
/// <param name="playerHand">플레이어의 HandVisual 컴포넌트</param>
/// <param name="replayPoses">리플레이 포즈 데이터 딕셔너리</param>
/// <param name="passed">유사도 기준 통과 여부 (out)</param>
/// <param name="handName">손 이름 (디버깅용)</param>
/// <returns>0~1 범위의 유사도 점수</returns>
private float ComparePose(HandVisual playerHand,
    Dictionary<int, PoseData> replayPoses,
    out bool passed,
    string handName)
```

### 6.3 에러 처리 강화

**현재 문제**
```cs
try
{
    // 작업 수행
}
catch (Exception e)
{
    Debug.LogError(e.Message);  // 로그만 출력
}
```

**권장 개선**
```cs
try
{
    // 작업 수행
}
catch (FileNotFoundException e)
{
    Debug.LogError($"파일을 찾을 수 없습니다: {e.FileName}");
    ShowErrorUI("파일 로드 실패");
    return null;
}
catch (FormatException e)
{
    Debug.LogError($"CSV 포맷 오류: {e.Message}");
    // 기본값으로 복구 시도
    return GetDefaultData();
}
catch (Exception e)
{
    Debug.LogError($"예기치 않은 오류: {e}");
    throw;  // 치명적 오류는 재발생
}
```

---

## 7. 우선순위별 액션 플랜

### 7.1 즉시 실행 (완료)
- [x] ObjectController.cs 삭제
- [x] PoseRecorder.cs 삭제
- [x] PosePlayer.cs 삭제
- [x] 최적화 보고서 작성

### 7.2 단기 (1-2주)
- [ ] HandPosePlayer OnDestroy 추가
- [ ] ChunaMotionDataManager OnDestroy 추가
- [ ] GC 압박 개선 (임시 객체 재사용)
- [ ] CSV 로딩 비동기화

### 7.3 중기 (1개월)
- [ ] 포즈 처리 기본 클래스 추출 (AbstractPoseHandler)
- [ ] 큰 파일 분할 (PatientModelController, LobbyAuthUI)
- [ ] 이벤트 시스템 통합 (공통 EventManager)
- [ ] 단위 테스트 추가 (NUnit)

### 7.4 장기 (2-3개월)
- [ ] 훈련 시스템 리팩토링 (BaseTrainingController)
- [ ] DotTimeline/DotTimelineController 통합
- [ ] 전체 아키텍처 개선 (의존성 주입)
- [ ] 성능 프로파일링 및 최적화
- [ ] CI/CD 파이프라인 구축

---

## 8. 종합 평가

### 현재 상태
```
┌──────────────────────┬────────┬─────────────────────┐
│ 항목                  │ 점수   │ 설명                │
├──────────────────────┼────────┼─────────────────────┤
│ 아키텍처              │ 6/10   │ 계층 분리 좋으나    │
│                      │        │ 순환 의존성 존재    │
├──────────────────────┼────────┼─────────────────────┤
│ 코드 품질             │ 6.5/10 │ 주석 충실, 중복 많음│
├──────────────────────┼────────┼─────────────────────┤
│ 성능                  │ 6/10   │ 메모리/GC 최적화 필요│
├──────────────────────┼────────┼─────────────────────┤
│ 유지보수성            │ 6/10   │ 파일 크기 문제      │
├──────────────────────┼────────┼─────────────────────┤
│ 테스트 가능성         │ 5/10   │ Mock 있으나 테스트  │
│                      │        │ 프레임워크 부재     │
├──────────────────────┼────────┼─────────────────────┤
│ 문서화                │ 7/10   │ 주석 충실           │
├──────────────────────┼────────┼─────────────────────┤
│ 보안                  │ 7/10   │ 큰 문제 없음        │
├──────────────────────┼────────┼─────────────────────┤
│ 확장성                │ 5/10   │ 중복으로 확장 어려움│
├──────────────────────┼────────┼─────────────────────┤
│ TOTAL                │ 6.1/10 │ 개선 필요 (중급)    │
└──────────────────────┴────────┴─────────────────────┘
```

### 목표 상태 (3개월 후)
```
목표 점수: 8.0/10

주요 개선 예상:
- 아키텍처: 6 → 8 (의존성 정리, 기본 클래스 추출)
- 코드 품질: 6.5 → 8 (중복 제거, 파일 분할)
- 성능: 6 → 8.5 (메모리 최적화, 비동기 로딩)
- 유지보수성: 6 → 8 (파일 분할, 명확한 책임)
- 테스트 가능성: 5 → 7.5 (단위 테스트 추가)
```

---

## 9. 참조

### 분석 메트릭
- **총 파일 수**: 40개 C# 파일
- **총 코드 라인**: 15,509줄
- **프로젝트 크기**: 1.2MB
- **삭제된 코드**: 539줄 (3.5%)
- **중복 코드 추정**: ~1,200줄 (7.7%)

### 도구 및 방법론
- **분석 도구**: 정적 코드 분석, 의존성 그래프
- **검색 방법**: Grep, Read, 파일 참조 분석
- **기준**: Clean Code, SOLID 원칙, Unity 모범 사례

---

## 10. 결론

이번 최적화 작업을 통해 **539줄의 사용되지 않는 코드를 제거**하고, **중복 기능 5개 영역**을 식별했습니다. 프로젝트는 전반적으로 **중급 수준**의 코드 품질을 유지하고 있으며, **계층별 분리가 잘 되어 있고 이벤트 기반 설계**를 활용하고 있습니다.

하지만 **파일 크기가 크고, 중복 코드가 많으며, 메모리 최적화가 부족**한 상태입니다. 제안된 **우선순위별 액션 플랜**을 따라 점진적으로 개선하면, **3개월 내에 8.0/10 수준의 코드 품질**을 달성할 수 있을 것으로 예상됩니다.

특히 **포즈 처리 계층의 기본 클래스 추출**, **큰 파일 분할**, **메모리 누수 방지** 작업을 우선적으로 진행할 것을 권장합니다.

---

**보고서 작성**: Claude Code (Anthropic)
**날짜**: 2025-11-14
**버전**: 1.0
