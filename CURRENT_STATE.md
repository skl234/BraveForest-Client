# 현재 Unity 자료 기준 — 2026-09-07

실제 게임은 `Assets/MYFolder/Scenes/LoginScene.unity`에서 시작합니다. 서버 입장 응답에 따라 마을 또는 비기너존으로 이동합니다.

| 용도 | 현재 파일 |
| --- | --- |
| 로그인 | Assets/MYFolder/Scenes/LoginScene.unity |
| 마을 | Assets/MYFolder/Scenes/TownScene.unity |
| 비기너존 | Assets/MYFolder/Scenes/BeginnerZoneScene.unity |
| 마을 지도 | Assets/MYFolder/Image/Map/TownMap.png |
| 비기너존 지도 | Assets/MYFolder/Image/Map/BeginnerZoneMap.png |
| 지도 생성 도구 | Assets/MYFolder/Editor/ZoneMapBuilder.cs |
| 공용 게임 UI | Assets/MYFolder/Prefab/UI/ZoneInterface.prefab |
| 현재 맵 전경 및 검증 자료 | Build/Validation/Current |

논리 필드 크기는 마을 100 × 100, 비기너존 220 × 220입니다. 비기너존의 SampleScene 폴더에 남아 있던 NavMesh 2개는 `Scenes/BeginnerZoneScene`으로 옮겼으며 GUID와 내용은 동일합니다. 현재 사용하는 지형·오브젝트 배치·포탈 좌표는 이번 정리에서 변경하지 않았습니다.

`Build/LocalArchive`는 변경 전 자료입니다. 현재 포트폴리오 설명이나 스크린샷 선택에 쓰지 않습니다. 에셋 제작사 제공 Demo 씬도 게임의 실제 맵이 아닙니다.

현재 지도 이미지는 `ZoneSceneInfo`에서 가져와 `ZoneMapUI`가 표시합니다. 전체 펼치기/축소, 플레이어 위치, 포탈 마커를 표시하며 다른 존까지 연결한 월드맵은 없습니다.

GitHub 공개용 설정은 **클라이언트 소스 검토용**입니다. 외부 에셋 원본을 제외했으므로 공개 저장소만 받아서는 동일 화면으로 실행되지 않습니다. 실행 가능한 전체 Unity 프로젝트는 라이선스 에셋이 설치된 현재 로컬 프로젝트입니다.
