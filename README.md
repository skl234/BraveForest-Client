# Brave Forest — Unity Client

Unity 2022.3.38f1 기반 MMORPG 클라이언트입니다. 로그인, 캐릭터 선택, 마을/비기너존 이동, 전사·궁수 전투 연출, 채널·채팅·지도 UI를 구현합니다.

## 시연 영상 및 서버

- [BraveForest 플레이 영상 보기](https://youtu.be/IJQr8NuZfT0)
- [C++ MMO 서버 소스](https://github.com/skl234/BraveForest-Server)

이 저장소의 기본 공개 범위는 프로젝트 전용 C# 코드, 씬 연결 구조, 서버에 전달하는 필드 데이터 및 프로젝트 설정입니다. 외부 모델·텍스처·폰트·이펙트 에셋 원본은 제외합니다. **에셋이 생략된 소스 검토용 저장소이므로 그대로 열어서 동일한 게임을 실행할 수 있는 배포본은 아닙니다.**

## 구조

- `Assets/MYFolder/Script`: 네트워크, 씬, 오브젝트, 전투, UI 클래스
- `Assets/MYFolder/Editor`: 공통 UI 구성, 서버 데이터 내보내기, 지도 생성 도구
- `Assets/MYFolder/Scenes`: LoginScene, TownScene, BeginnerZoneScene
- `Assets/MYFolder/ExportedFieldData`: 서버용 Zone/Cell/Sector/몬스터 스폰 데이터
- `Packages`, `ProjectSettings`: Unity 환경 및 프로젝트 설정

UI는 씬/프리팹에 미리 배치하고 실행 중에는 값과 활성 상태를 갱신합니다. 네트워크 연결은 씬 변경 후에도 유지하는 NetClient가 담당하며, 씬별 네트워크 클래스는 해당 패킷 처리와 연결을 담당합니다.

## 씬과 데이터

`LoginScene`에서 시작하고 캐릭터 입장 응답의 ZoneId에 따라 `TownScene` 또는 `BeginnerZoneScene`으로 이동합니다. 마을은 ZoneId 1, 비기너존은 ZoneId 2입니다.

`FieldDataExporter`는 이동 불가 셀과 섹터 크기를, `MonsterSpawnExporter`는 몬스터 배치와 타입 설정을 내보냅니다. `ServerFileDataSetup.Export()`는 존·포탈·스폰 파일을 서버의 Data 폴더로 복사합니다. 기본 서버 경로는 Unity 프로젝트와 같은 상위 폴더에 있는 `MMONetwork2`입니다.

`ZoneMapBuilder`는 저장된 씬을 렌더링해 지도 이미지를 만듭니다. `ZoneMapUI`는 그 이미지 위에 플레이어 위치와 포탈을 표시합니다.

구형 외형 프리팹에서 참조하는 스크립트도 포함합니다. 온라인 이동과 전투는 `Object/Player`, `Object/Monster`, `Network/ZoneSceneNetwork`를 기준으로 확인할 수 있습니다.

전체 프로젝트 실행에는 정식으로 확보한 외부 에셋과 서버/DB 환경이 필요합니다. 사용 에셋의 원본 라이선스와 제작자 표기는 로컬 프로젝트에 유지합니다.
