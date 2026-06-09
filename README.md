# XR UI UX Team Speed - VR Racing Game

이 저장소는 Unity로 제작한 VR 레이싱 게임 프로젝트의 과제 제출용 코드 저장소입니다.

원본 Unity 프로젝트에는 차량 모델, 트랙 에셋, 텍스처, 오디오, XR 패키지, Unity 캐시 파일 등 용량이 큰 파일이 많이 포함되어 있습니다. 그래서 이 GitHub 저장소에는 전체 Unity 프로젝트를 그대로 올리지 않고, 프로젝트 구현을 확인할 수 있는 핵심 스크립트와 설명 문서만 정리해서 업로드했습니다.

## 프로젝트 개요

이 프로젝트는 Meta Quest 3 VR 환경과 Logitech G29 Driving Force 레이싱 휠을 사용해 플레이하는 Unity 기반 자동차 레이싱 게임입니다.

플레이어는 VR 로비에서 `Practice` 또는 `Quick Match`를 선택하고, 자동 변속 또는 수동 변속을 고른 뒤 운전석 시점에서 레이스를 진행합니다. 프로젝트에는 튜토리얼 맵, AI 상대 차량, G29 핸들/페달/버튼 입력, 레이스 HUD, 미니맵, 랩 타임, 순위 계산, 일시정지 메뉴, 결과 UI, 수동 변속 시스템이 포함되어 있습니다.

## 포함된 내용

- `Scripts/`: Unity 프로젝트에서 사용한 핵심 C# 스크립트
- `Scripts/Editor/`: 씬 구성과 세팅을 돕기 위해 사용한 Unity Editor 스크립트
- `PROJECT_PRESENTATION_BRIEF.md`: PPT 제작을 위한 프로젝트 상세 설명 문서
- `Packages/manifest.json`: Unity 패키지 의존성 참고용 파일

## 포함하지 않은 내용

이 저장소는 전체 프로젝트 배포용이 아니라 과제 제출과 코드 확인용이므로 아래 항목은 제외했습니다.

- Unity 자동 생성 폴더: `Library/`, `Temp/`, `Logs/`, `obj/`
- 대용량 모델, 텍스처, 오디오, 서드파티 에셋
- 빌드 결과물
- 전체 씬 에셋과 프리팹 의존성

따라서 이 저장소만 clone해서 Unity에서 바로 실행 가능한 완성 프로젝트를 복원할 수는 없습니다. 대신 구현 방식과 핵심 코드를 확인하기 위한 제출용 저장소입니다.

## 주요 기능

- Meta Quest 3 기반 VR 운전석 레이싱 경험
- Logitech G29 핸들, 페달, 십자키, 버튼 입력 지원
- 로비에서 Practice / Quick Match 선택
- 자동 변속과 수동 변속 선택 지원
- 클러치와 기어 업/다운 버튼을 사용하는 수동 변속 시스템
- WheelCollider 기반 플레이어 차량 조작
- 웨이포인트 기반 AI 상대 차량
- 카운트다운, 랩 카운터, 타이머, 랩타임, 순위, 결과 UI
- G29 입력으로 조작 가능한 월드 스페이스 VR HUD와 일시정지 메뉴
- 차량 내부 Quad에 표시되는 RenderTexture 기반 미니맵
- 순차적으로 조작법을 알려주는 튜토리얼 시스템
- 도로 이탈 시 차량 재배치 기능
- 엔진음과 배경음 관리

## 주요 스크립트

- `CarController.cs`
  - 플레이어 차량 물리, G29 입력, 가속, 브레이크, 조향, 후진, 계기판, 도로 이탈 출력 감소 처리

- `CarTransmissionController.cs`
  - 자동/수동 변속 선택, 클러치 입력, 기어 변경 조건, 수동 기어별 토크 계산

- `RaceSessionManager.cs`
  - 레이스 진행, HUD, 랩타임, 순위 계산, 결과 UI, 튜토리얼 모드, 미니맵, 엔진 사운드 마스터 볼륨 관리

- `CarControllerWaypointAi.cs`
  - AI 차량의 웨이포인트 기반 주행과 레이스 시작/종료 상태 처리

- `RaceMinimapController.cs`
  - 미니맵 카메라, RenderTexture, 플레이어/AI 위치 점 표시

- `G29PauseMenu.cs`
  - G29와 키보드 입력을 이용한 일시정지 메뉴 조작

- `LobbyLicensePrompt.cs`
  - 로비에서 자동/수동 변속 선택 UI 처리

- `WheelNavManager.cs`
  - 로비 카드 선택을 위한 G29 십자키와 선택 버튼 입력 처리

- `CarResetController.cs`
  - 차량이 도로를 벗어났을 때 안전한 위치로 재배치하는 기능

## 씬 흐름

```text
LobbyScene
  -> Practice 카드 선택
    -> 자동 / 수동 변속 선택
    -> MainTrack 튜토리얼 씬

  -> Quick Match 카드 선택
    -> 자동 / 수동 변속 선택
    -> RealMainTrack 레이스 씬
```

## 발표 자료용 설명 문서

프로젝트 전체 설명과 PPT 슬라이드 구성 추천은 아래 문서에 정리되어 있습니다.

`PROJECT_PRESENTATION_BRIEF.md`
