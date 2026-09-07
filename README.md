# Brave Forest — Unity Client

Unity 2022.3.38f1 기반 MMORPG 클라이언트입니다. 로그인, 캐릭터 선택, 마을/비기너존 이동, 전사·궁수 전투 연출, 채널·채팅·지도 UI를 구현합니다.

## 시연 영상 및 서버

- [BraveForest 플레이 영상 보기](https://youtu.be/IJQr8NuZfT0)
- [C++ MMO 서버 소스](https://github.com/skl234/BraveForest-Server)

이 저장소의 기본 공개 범위는 프로젝트 전용 C# 코드, 씬 연결 구조, 서버에 전달하는 필드 데이터 및 프로젝트 설정입니다. 외부 모델·텍스처·폰트·이펙트 에셋 원본은 제외합니다. **에셋이 생략된 소스 검토용 저장소이므로 그대로 열어서 동일한 게임을 실행할 수 있는 배포본은 아닙니다.**

## 구조

- `Assets/MYFolder/Script`: 네트워크, 씬, 오브젝트, 전투, UI 클래스
- `Assets/MYFolder/Editor`: 프리팹 설정, 데이터 내보내기, 지도 갱신 및 검증 도구
- `Assets/MYFolder/Scenes`: LoginScene, TownScene, BeginnerZoneScene
- `Assets/MYFolder/ExportedFieldData`: 서버용 Zone/Cell/Sector/몬스터 스폰 데이터
- `Packages`, `ProjectSettings`: Unity 환경 및 프로젝트 설정

UI는 씬/프리팹에 미리 배치하고 실행 중에는 값과 활성 상태를 갱신합니다. 네트워크 연결은 씬 변경 후에도 유지하는 NetClient가 담당하며, 씬별 네트워크 클래스는 해당 패킷 처리와 연결을 담당합니다.

[현재 씬·지도 자료 기준](CURRENT_STATE.md)을 먼저 확인합니다. `Build/LocalArchive`와 에셋 제작사 Demo 씬은 현재 게임 자료가 아닙니다.

전체 프로젝트 실행에는 정식으로 확보한 외부 에셋과 서버/DB 환경이 필요합니다. 사용 에셋의 원본 라이선스와 제작자 표기는 로컬 프로젝트에 유지합니다.
