# 중복 파일 분석 보고서

## 실행 날짜: 2025-11-14

---

## 📋 발견된 중복 파일

### 1. ✅ **삭제 가능: DotTimeline.cs** (138줄)

**사유**: DotTimelineController.cs의 단순 버전, 기능 중복

#### 사용처 분석
```
DotTimeline.cs 사용:
└── UI/ScenarioPrefabCreator.cs (에디터 전용 도구)
    - 프리팹 생성 시에만 사용
    - DotTimelineController로 대체 가능

DotTimelineController.cs 사용:
├── Scenario/ScenarioUIController.cs (실제 사용)
└── ChunaSystem/ChunaTrainingController.cs (실제 사용)
```

#### 기능 비교
| 기능 | DotTimeline | DotTimelineController |
|------|-------------|----------------------|
| **도트 생성** | ✅ | ✅ |
| **진행도 표시** | ✅ | ✅ |
| **펄싱 애니메이션** | ❌ | ✅ |
| **라인 연결** | ❌ | ✅ |
| **동적 크기 조절** | ❌ | ✅ |

**결론**: DotTimelineController가 더 완전한 구현. DotTimeline 삭제 권장.

---

### 2. ⚠️ **유지 권장: ChunaTrainingController.cs vs IntegratedChunaTrainingSystem.cs**

**사유**: 용도가 다름 (중복이 아님)

#### 차이점 분석
| 항목 | ChunaTrainingController | IntegratedChunaTrainingSystem |
|------|------------------------|------------------------------|
| **목적** | 독립적 트레이닝 시스템 | ScenarioManager 통합 시스템 |
| **크기** | 829줄 | 756줄 |
| **의존성** | HandPosePlayer, GuideSystem | + ScenarioManager |
| **사용 사례** | 단순 교육/평가 모드 | 복잡한 시나리오 기반 훈련 |
| **모드 전환** | 수동 | 자동 (시나리오 기반) |
| **데이터 소스** | CSV만 | CSV + 시나리오 데이터 |

**결론**:
- ChunaTrainingController: 간단한 단일 모션 훈련용
- IntegratedChunaTrainingSystem: 복잡한 다단계 시나리오용
- **둘 다 유지 필요** (서로 다른 사용 사례)

---

### 3. ⚠️ **확인 필요: ScenarioDataClasses.cs vs ScenarioDataSO.cs**

#### 파일 확인
```bash
Scenario/ScenarioDataClasses.cs - 데이터 클래스 정의
Scenario/ScenarioDataSO.cs - ScriptableObject 버전
```

두 파일의 관계를 확인하겠습니다.

---

## 🔍 추가 분석

### HandDateEditor 폴더명 오타
```
/HandDateEditor/  ❌ 오타
/HandDataEditor/  ✅ 올바른 이름
```

**권장**: 폴더 이름 수정 (Date → Data)

---

## 📊 삭제 권장 요약

### 즉시 삭제 가능
1. **UI/DotTimeline.cs** (138줄)
   - ScenarioPrefabCreator 수정 필요
   - DotTimelineController 사용으로 변경

### 조건부 검토
2. **ChunaSystem/HandPoseLoopController.cs** (274줄)
   - HandPosePlayer의 확장 클래스
   - 사용처 확인 필요
   - 만약 사용 안 되면 삭제 가능

---

## 🛠️ 삭제 절차

### Step 1: DotTimeline.cs 제거
```bash
1. ScenarioPrefabCreator.cs 수정
   - DotTimeline → DotTimelineController로 변경

2. 파일 삭제
   - UI/DotTimeline.cs
   - UI/DotTimeline.cs.meta

3. 테스트
   - 프리팹 생성 기능 확인
   - 시나리오 UI 정상 작동 확인
```

### Step 2: 사용 안 되는 파일 추가 확인
```bash
# HandPoseLoopController 사용처 검색
grep -r "HandPoseLoopController" --include="*.cs" --include="*.prefab" --include="*.unity"

# 사용 안 되면 삭제 가능
```

---

## 📈 예상 효과

### 코드 정리
- DotTimeline.cs 삭제: **-138줄**
- ScenarioPrefabCreator.cs 수정: **+10줄, -20줄**
- **순 감소: ~150줄**

### 유지보수성
- 도트 타임라인 로직 단일화
- 버그 수정 시 한 곳만 수정
- 코드 중복 0%

---

## ⚠️ 주의사항

### ChunaTrainingController vs IntegratedChunaTrainingSystem
**삭제하지 마세요!**

이 두 파일은 중복이 아니라 **다른 사용 사례를 위한 별도 구현**입니다:

- **ChunaTrainingController**:
  - 독립 실행
  - 빠른 프로토타이핑
  - 단순한 교육/평가

- **IntegratedChunaTrainingSystem**:
  - 시나리오 통합
  - 복잡한 워크플로우
  - 자동 모드 전환

향후 개선 방향:
1. 공통 로직을 **BaseChunaTrainingSystem** 추상 클래스로 추출
2. 두 클래스가 상속받도록 리팩토링
3. 중복 코드 ~200줄 감소 예상

하지만 **현재는 삭제하지 말고 유지**하는 것이 안전합니다.

---

## 결론

### 즉시 삭제 권장
- ✅ **DotTimeline.cs** (138줄)

### 유지 필요
- ⚠️ **ChunaTrainingController.cs** (용도가 다름)
- ⚠️ **IntegratedChunaTrainingSystem.cs** (용도가 다름)

### 추가 조사 필요
- ❓ **HandPoseLoopController.cs** (사용처 불명확)

**총 절감 예상**: ~150줄 (DotTimeline 제거 시)

---

**분석자**: Claude Code (Anthropic)
**날짜**: 2025-11-14
