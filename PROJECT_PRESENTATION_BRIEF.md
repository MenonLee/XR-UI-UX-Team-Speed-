# VR Racing Game Project Brief

이 문서는 이 Unity 프로젝트를 처음 보는 AI가 프로젝트 구조와 기능을 빠르게 이해하고, 발표용 PPT를 만들 수 있도록 정리한 설명서입니다.

## 1. 프로젝트 한 줄 요약

Meta Quest 3 VR 환경에서 Logitech G29 스티어링 휠, 페달, 버튼 입력을 사용해 주행하는 자동차 레이싱 게임입니다. 플레이어는 로비에서 트랙을 선택하고 자동/수동 변속 방식을 고른 뒤, 튜토리얼 또는 AI 상대 차량이 있는 레이스를 진행합니다.

## 2. 프로젝트 목표

- VR 헤드셋을 착용한 상태에서 실제 운전석에 앉은 듯한 레이싱 경험 제공
- Logitech G29의 핸들, 엑셀, 브레이크, 클러치, 십자키, 버튼을 게임 조작에 연결
- 자동 변속과 수동 변속을 모두 지원
- 처음 플레이하는 사용자를 위한 튜토리얼 맵 제공
- AI 차량과 경쟁하는 Quick Match 레이스 제공
- HUD, 미니맵, 랩 카운터, 순위, 일시정지 메뉴, 종료 결과 UI 구현

## 3. 개발 환경

- Engine: Unity 6.3 LTS
- Render Pipeline: Universal Render Pipeline, URP
- XR Target: Meta Quest 3
- Input Device: Logitech G29 Driving Force Racing Wheel
- Input System: Unity Input System
- Main Platform: Windows Editor 테스트 및 VR 빌드

## 4. 주요 씬 구성

현재 Build Settings 씬 순서는 다음과 같습니다.

1. `Assets/Scenes/LobbyScene.unity`
   - 게임 시작 씬
   - Practice, Quick Match 카드 선택 UI
   - 트랙 선택 후 자동/수동 변속 선택 UI 표시

2. `Assets/Scenes/MainTrack.unity`
   - 튜토리얼 맵
   - Practice 카드에서 진입
   - 2랩 튜토리얼 구성
   - 순위표 대신 튜토리얼 완료 UI 표시

3. `Assets/Scenes/RealMainTrack.unity`
   - 실제 레이스 맵
   - Quick Match 카드에서 진입
   - `Race_Track_7` 맵 기반으로 구성
   - 플레이어 차량과 AI 상대 차량이 함께 주행

## 5. 전체 게임 흐름

```text
LobbyScene
  -> Practice 선택
    -> 자동/수동 선택
    -> MainTrack 튜토리얼 시작

  -> Quick Match 선택
    -> 자동/수동 선택
    -> RealMainTrack 레이스 시작
```

레이스 씬에 들어가면 카운트다운이 나오고, 카운트다운이 끝난 뒤 차량 조작이 활성화됩니다. 레이스 도중 HUD에는 시간, 랩, 순위, 변속 모드, 속도, 기어가 표시됩니다. 마지막 랩을 통과하면 결과 UI가 나오고 Retry, Lobby, Quit 버튼을 선택할 수 있습니다.

## 6. 로비 시스템

로비 씬은 VR 공간 안에 고정된 월드 스페이스 UI로 구성되어 있습니다.

주요 기능:

- G29 십자키로 카드 선택
- 동그라미 버튼으로 카드 확정
- `Practice` 카드: 튜토리얼 씬 `MainTrack`으로 이동
- `Quick Match` 카드: 레이스 씬 `RealMainTrack`으로 이동
- 카드 선택 후 바로 씬 이동하지 않고, 자동/수동 변속 선택 UI 표시

관련 스크립트:

- `Assets/Scripts/WheelNavManager.cs`
  - 로비 카드 선택 입력 처리
  - G29 십자키 입력을 받아 카드 포커스 이동
  - 동그라미 버튼으로 선택 확정

- `Assets/Scripts/CardHoverEffect.cs`
  - 카드 슬롯의 선택 효과와 씬 이동 요청 처리
  - 직접 씬을 로드하지 않고 `LobbyLicensePrompt.ShowForScene(sceneToLoad)`를 호출

- `Assets/Scripts/LobbyLicensePrompt.cs`
  - 자동/수동 변속 선택 팝업
  - 선택값을 `PlayerPrefs`의 `TransmissionMode`에 저장
  - 저장값은 레이스 씬의 차량 변속 시스템에서 읽음

## 7. G29 입력 체계

G29는 Unity Input System으로 연결되어 있습니다.

주요 입력:

- Steering: G29 핸들 축
- Accelerate: 엑셀 페달
- Brake: 브레이크 페달
- Clutch: 클러치 페달
- Gear Up: 기어 업 버튼
- Gear Down: 기어 다운 버튼
- ResetCar: 차량 리셋 버튼
- Pause: 네모 버튼 또는 ESC
- Menu Submit: 동그라미 버튼
- D-Pad: 로비, 일시정지 메뉴, 결과 UI 선택 이동

프로젝트에는 G29 입력 확인용 임시 스크립트도 있습니다.

- `Assets/Scripts/G29ButtonLogger.cs`
- `Assets/Scripts/G29CrossButtonTester.cs`

## 8. 플레이어 차량 시스템

플레이어 차량은 `CarController.cs`를 중심으로 동작합니다.

핵심 기능:

- WheelCollider 기반 물리 주행
- G29 핸들 입력 기반 조향
- 엑셀/브레이크 페달 입력
- 정지 상태에서 브레이크를 길게 밟으면 후진
- 실제 차량 내부 핸들 모델과 G29 입력 동기화
- 속도계, RPM 게이지 연동
- 엔진 사운드 연동
- 도로 외 주행 시 출력 감소
- 레이스 시작 전 또는 카운트다운 중 조작 잠금

관련 스크립트:

- `Assets/Scripts/CarController.cs`
  - 차량 물리, 조향, 가속, 브레이크, 후진, HUD용 속도/기어 값 제공

- `Assets/Scripts/CarEngineSound.cs`
  - 차량 속도와 입력값에 따라 엔진 사운드 볼륨/피치 조절

- `Assets/Scripts/CarResetController.cs`
  - 세모 버튼 또는 리셋 입력으로 차량을 안전 위치에 재배치
  - 도로 또는 가까운 경로 중심을 기준으로 리스폰
  - 일시정지 메뉴가 열려 있을 때는 리셋 입력을 무시

## 9. 자동/수동 변속 시스템

자동 변속과 수동 변속은 `CarTransmissionController.cs`에서 관리합니다.

자동 모드:

- 기존 차량 조작 방식 유지
- 엑셀, 브레이크만으로 주행
- HUD 기어 표시: `D`

수동 모드:

- 클러치를 밟아야 기어 변경 가능
- Gear Up / Gear Down 버튼 사용
- 1단부터 6단까지 지원
- N단, R단은 별도 표시하지 않음
- 후진은 기존처럼 브레이크를 길게 밟는 방식 유지
- 너무 낮은 속도에서 높은 기어를 사용하면 가속이 제한됨
- 일정 속도 이상이 되어야 다음 기어로 변속 가능

수동 기어 설정 예시:

```text
1단 최대 속도: 약 95 km/h
2단 최대 속도: 약 140 km/h
3단 최대 속도: 약 185 km/h
4단 최대 속도: 약 230 km/h
5단 최대 속도: 약 280 km/h
6단 최대 속도: 약 340 km/h
```

이 시스템은 실제 수동 차량의 감각을 단순화해서 구현한 것입니다. 낮은 단수에서는 초반 가속이 강하고, 높은 단수에서는 더 높은 최고 속도를 낼 수 있습니다.

## 10. 레이스 세션 시스템

레이스 전체 진행은 `RaceSessionManager.cs`가 담당합니다.

핵심 기능:

- 레이스 시작/종료 상태 관리
- 카운트다운 종료 후 차량 출발 허용
- 랩 카운터
- 전체 타이머
- 랩타임 기록
- 실시간 순위 계산
- 결과 UI 표시
- Retry, Lobby, Quit 버튼 처리
- AI 차량 목록 등록
- 엔진 사운드 마스터 볼륨 조절
- VR용 월드 스페이스 HUD 생성
- 미니맵 생성 및 갱신
- 튜토리얼 모드 자동 활성화

관련 스크립트:

- `Assets/Scripts/RaceSessionManager.cs`
  - 레이스 시스템의 중심 스크립트

- `Assets/Scripts/RaceCountdown.cs`
  - 시작 전 카운트다운
  - 카운트다운 중에는 시간이 흐르지 않고 차량도 움직이지 않도록 처리

- `Assets/Scripts/FinishLineTrigger.cs`
  - 피니쉬 라인 통과 감지
  - 처음 피니쉬라인 통과는 시작 위치 보정 때문에 무시 가능

- `Assets/Scripts/RaceCheckpointTrigger.cs`
  - 체크포인트 통과 기록
  - 순위 계산 정확도를 높이기 위해 사용

## 11. HUD와 UI

레이스 중 HUD는 VR에서 보기 편하도록 월드 스페이스 캔버스로 구성됩니다.

HUD 표시 항목:

- Time
- Lap
- Position
- Mode: Automatic 또는 Manual
- Speed
- Gear: 자동은 `D`, 수동은 현재 단수
- Lap Time 기록
- 튜토리얼 도움말

일시정지 메뉴:

- 네모 버튼 또는 ESC로 열기
- G29 십자키로 메뉴 이동
- 동그라미 버튼으로 선택
- Resume
- Return To Lobby
- Quit Game

결과 UI:

- 레이스 종료 시 표시
- 순위와 기록 표시
- Retry
- Lobby
- Quit

튜토리얼 완료 UI:

- 튜토리얼 씬에서는 일반 순위표 대신 `튜토리얼 완료!` 메시지 표시

관련 스크립트:

- `Assets/Scripts/G29PauseMenu.cs`
- `Assets/Scripts/PauseMenuUiBuilder.cs`
- `Assets/Scripts/RaceSessionManager.cs`

## 12. 튜토리얼 시스템

`MainTrack` 씬은 튜토리얼 맵으로 사용됩니다. `RaceSessionManager`는 씬 이름이 `MainTrack`이면 튜토리얼 모드를 자동으로 활성화합니다.

튜토리얼 특징:

- 전체 랩 수: 2랩
- 레이스 시작 후 중앙 HUD에 도움말 표시
- 모든 도움말은 한 번만 표시
- 도움말은 순서대로 진행되며, 이전 도움말이 끝나기 전에 다음 도움말이 끼어들지 않음
- 도로 이탈, 낮은 속도 높은 기어 같은 조건형 도움말도 대기열 방식으로 처리

튜토리얼 흐름:

```text
카운트다운 종료
  -> "출발합니다! 엑셀을 밟아 앞으로 전진하세요"
  -> 일정 거리 이동 후 "브레이크를 밟아 멈춰보세요"
  -> 수동 모드라면 1단 최고속도 근처에서 기어 업 안내
  -> 기어 변속 성공 시 높은 기어 설명
  -> 낮은 속도에서 높은 기어 사용 시 가속 제한 안내
  -> 도로 이탈 시 리셋 안내
  -> 1랩 완료 후 일시정지 메뉴 안내
  -> 일시정지 후 Resume하면 자유 주행 안내
  -> 2랩 완료 시 튜토리얼 완료 UI
```

튜토리얼은 조작법 학습이 목적이기 때문에 일반 레이스처럼 순위표를 보여주지 않습니다.

## 13. AI 차량 시스템

AI 차량은 플레이어와 같은 트랙 위를 주행하며, 웨이포인트 경로를 따라 이동합니다.

핵심 기능:

- `Path_1`, `Path_2` 같은 웨이포인트 경로 추종
- 목표 웨이포인트 방향으로 조향
- AI 차량별 시작 위치 배치
- 레이스 시작 전 대기
- 레이스 종료 시 엔진 사운드 정리
- 플레이어와 함께 순위 계산에 포함

관련 스크립트:

- `Assets/Scripts/CarControllerWaypointAi.cs`
  - AI 차량 경로 추종 및 주행 제어

- `Assets/Scripts/EnemyAiRaceBridge.cs`
  - AI 차량을 레이스 세션에 연결

- `Assets/Scripts/EnemyCarSpawner.cs`
  - AI 차량 배치/등록 관련 보조 기능

AI 차량은 단순히 직선으로 움직이는 것이 아니라, 트랙의 웨이포인트와 체크포인트 데이터를 이용해 레이스 진행 상황을 계산합니다.

## 14. 순위 계산 시스템

순위는 단순 거리 비교가 아니라 다음 요소를 함께 사용합니다.

- 완료한 랩 수
- 마지막으로 통과한 체크포인트
- 다음 체크포인트까지의 진행도
- 피니쉬 라인 통과 여부
- 최종 기록

체크포인트를 활용하는 이유는 플레이어 또는 AI가 길이 아닌 곳으로 크게 이탈했을 때 단순 직선거리만으로 순위가 잘못 계산되는 문제를 줄이기 위해서입니다.

## 15. 미니맵 시스템

미니맵은 플레이어 차량 내부의 `MiniMap` Quad에 표시됩니다.

핵심 방식:

- `RaceMinimapController.cs`가 런타임 RenderTexture 생성
- 미니맵 전용 카메라가 트랙을 위에서 촬영
- RenderTexture를 플레이어 차량 내부의 `MiniMap` Quad에 적용
- 플레이어와 AI 차량 위치를 색상 점으로 표시
- 미니맵 점은 별도 레이어에 배치되어 VR 메인 카메라에는 보이지 않음

AI 점 색상:

- 차량 이름에 포함된 색상 이름을 기준으로 표시
- 예: Yellow, Blue, Green, Purple, Orange, White 등

## 16. 차량 리셋 시스템

차량이 도로를 벗어나거나 뒤집힌 경우 세모 버튼으로 재배치할 수 있습니다.

리셋 특징:

- 즉시 이동하지 않고 짧은 딜레이 후 리스폰
- 차량 속도와 회전 속도 초기화
- WheelCollider 토크와 브레이크 힘 초기화
- 가능한 경우 가까운 경로 또는 체크포인트 기반 도로 중앙으로 복귀
- 일시정지 메뉴가 열려 있을 때는 Resume 입력이 리셋으로 오인되지 않도록 막음

## 17. 사운드 시스템

사운드는 크게 BGM과 엔진음으로 나뉩니다.

BGM:

- 씬 BGM은 `SceneBgmPlayer.cs`가 담당
- 레이스 종료 후에도 BGM은 계속 유지하도록 설정

엔진음:

- 플레이어 차량과 AI 차량에 각각 적용
- `CarEngineSound.cs`가 속도와 가속 입력에 따라 엔진음 반응 조절
- `RaceSessionManager`에서 플레이어 엔진 마스터 볼륨과 AI 엔진 마스터 볼륨을 한번에 조절 가능
- 레이스 종료 시 엔진음은 정리하지만 BGM은 유지

## 18. VR 연출 요소

로비 씬에는 시작 연출이 있습니다.

관련 기능:

- 로비 차량 하이빔 조명 연출
- 어두운 화면에서 라이트가 먼저 보이는 시네마틱 시작
- UI가 플레이어 머리에 붙어 움직이지 않도록 월드 공간에 배치
- VR 시점에서 메뉴와 HUD가 너무 멀거나 차체에 가려지지 않도록 거리와 위치 조정

관련 스크립트:

- `Assets/Scripts/DarkIntroController.cs`
- `Assets/Scripts/EngineStartEffect.cs`
- `Assets/Scripts/VRCameraCanvasPlacement.cs`

## 19. 구현 과정에서 해결한 주요 문제

발표에서 개발 난이도와 해결 과정을 보여줄 수 있는 포인트입니다.

- VR UI가 카메라에 붙어서 움직이는 문제
  - 화면 고정 UI가 아니라 월드 스페이스 UI로 배치하도록 수정

- 일시정지 메뉴가 너무 멀거나 차체에 가려지는 문제
  - VR HUD 거리와 메뉴 거리 조정

- G29 십자키와 동그라미 버튼 입력이 메뉴와 충돌하는 문제
  - 로비, 일시정지, 결과 UI 각각 입력 흐름 분리

- 시작 카운트다운 중 차량이 출발 가능한 문제
  - 카운트다운 완료 전 차량 조작 비활성화

- 랩 카운터가 시작 직후 피니쉬 라인을 지나며 증가하는 문제
  - 첫 피니쉬라인 통과를 무시하는 방식 적용

- 수동 변속에서 기어별 차이가 약한 문제
  - 기어별 최고속도, 저속 토크 제한, 고속 보조 힘을 따로 조정

- 순위 계산이 이상하게 바뀌는 문제
  - 웨이포인트와 체크포인트 기반 진행도 계산으로 개선

- 미니맵 점이 VR 시야에 직접 보이는 문제
  - 미니맵 전용 레이어와 카메라 culling mask로 분리

- 일시정지 Resume 시 차량이 리셋되는 문제
  - 메뉴 입력과 차량 리셋 입력 충돌 방지

## 20. 발표용 핵심 장점

- VR 헤드셋과 실제 레이싱 휠을 결합한 몰입형 조작 경험
- 자동/수동 변속 선택으로 난이도와 플레이 스타일 선택 가능
- 초보자를 위한 튜토리얼 맵과 도움말 시스템
- AI 상대 차량과 실시간 순위 경쟁
- 차량 내부 Quad 기반 미니맵으로 운전석 몰입감 유지
- VR 환경에 맞춰 HUD와 메뉴를 월드 스페이스로 설계
- 실제 입력 장치의 버튼 충돌 문제를 해결한 커스텀 입력 처리

## 21. PPT 추천 구성

다음 순서로 PPT를 만들면 프로젝트가 잘 전달됩니다.

### Slide 1. Title

제목 예시:

`Meta Quest 3 & Logitech G29 기반 VR Racing Game`

부제 예시:

`실제 레이싱 휠 조작과 VR 운전석 경험을 결합한 Unity 레이싱 프로젝트`

### Slide 2. Project Overview

내용:

- Unity URP 기반 VR 레이싱 게임
- Meta Quest 3 시점
- Logitech G29 조작
- 튜토리얼과 AI 레이스 제공

### Slide 3. Hardware & Control

내용:

- G29 핸들: 조향
- 엑셀/브레이크: 가속, 제동, 후진
- 클러치: 수동 변속
- 십자키/동그라미/네모/세모: UI 선택, 일시정지, 리셋

### Slide 4. Scene Flow

내용:

- LobbyScene
- Practice -> MainTrack
- Quick Match -> RealMainTrack
- Auto/Manual 선택 흐름

### Slide 5. Vehicle System

내용:

- WheelCollider 기반 물리 주행
- 핸들/페달 입력
- 속도계, RPM, 엔진음
- 도로 이탈 시 출력 감소

### Slide 6. Transmission System

내용:

- 자동 모드: 간단한 주행
- 수동 모드: 클러치 + 기어 업/다운
- 기어별 최고속도와 저속 토크 제한

### Slide 7. Tutorial Mode

내용:

- 2랩 튜토리얼
- 엑셀, 브레이크, 수동 변속, 리셋, 일시정지 안내
- 모든 안내는 한 번만 표시

### Slide 8. Race System

내용:

- 카운트다운
- 랩 카운터
- 타이머와 랩타임
- 체크포인트 기반 순위 계산
- 결과 UI

### Slide 9. AI Opponents

내용:

- 웨이포인트 기반 주행
- 여러 AI 차량
- 순위 시스템에 포함
- 플레이어와 경쟁

### Slide 10. VR UI & Minimap

내용:

- 월드 스페이스 HUD
- G29 입력 기반 메뉴 조작
- 차량 내부 Quad 미니맵
- AI별 색상 점 표시

### Slide 11. Problems & Solutions

내용:

- VR UI 위치 문제 해결
- G29 입력 충돌 해결
- 랩/순위 계산 개선
- 미니맵 레이어 분리
- Resume 시 차량 리셋 문제 해결

### Slide 12. Result & Future Work

내용:

- 완성된 기능 요약
- 향후 개선 가능성:
  - AI 주행 품질 개선
  - 트랙/차량 추가
  - VR 성능 최적화
  - 충돌 안정성 개선
  - 멀티플레이 확장

## 22. 시연 추천 순서

발표 중 실제 시연을 한다면 다음 순서가 자연스럽습니다.

1. 로비 시작 화면
2. Practice 선택
3. 자동/수동 선택 UI
4. 튜토리얼 카운트다운
5. 엑셀/브레이크/기어 도움말
6. 일시정지 메뉴 조작
7. Quick Match 진입
8. AI 차량과 주행
9. HUD, 미니맵, 순위 확인
10. 레이스 종료 결과 UI

## 23. Claude에게 PPT 생성을 요청할 때 사용할 프롬프트 예시

```text
아래 Markdown은 Unity로 만든 VR 레이싱 게임 프로젝트 설명서입니다.
이 내용을 바탕으로 12장 정도의 발표용 PPT 구성을 만들어주세요.

조건:
- 발표 대상은 이 프로젝트를 처음 보는 사람입니다.
- 기술 설명과 시연 흐름이 모두 들어가야 합니다.
- 각 슬라이드는 제목, 핵심 bullet, 발표자 노트로 구성해주세요.
- 너무 코드 중심이 아니라, 구현 의도와 사용자 경험이 잘 드러나게 구성해주세요.
- Meta Quest 3, Logitech G29, Unity, VR UI, 자동/수동 변속, AI 레이스를 핵심 키워드로 강조해주세요.
```

## 24. 핵심 키워드

- Unity
- URP
- Meta Quest 3
- VR Racing Game
- Logitech G29
- Steering Wheel Input
- WheelCollider
- Manual Transmission
- Automatic Transmission
- AI Opponents
- Waypoint Racing AI
- Checkpoint Ranking
- World Space UI
- VR HUD
- In-Car Minimap
- Tutorial System
- Pause Menu
- Race Result UI

