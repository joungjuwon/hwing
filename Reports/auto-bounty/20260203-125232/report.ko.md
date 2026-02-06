# AutoBounty 리포트 (리포트 전용 / 파일 수정 없음)

- 작성 시각: 2026-02-03 12:52 (KST)
- 프로젝트: `C:\Users\User\Documents\GitHub\hwing`
- Unity: 6000.2.15f1 (URP)

## 1) 스냅샷(현재 상태 기록)
- Git 브랜치: `git-branch.txt` 참고
- Git 변경 상태: `git-status.txt`
- Git 변경 요약 통계: `git-diff-stat.txt`

## 2) Unity 배치모드 실행(컴파일/로그 수집)
실행 커맨드(대략):
- `Unity.exe -batchmode -nographics -projectPath <proj> -quit -logFile <report>\unity-batch.log`

생성/수집 파일:
- `unity-batch.log`
- `Editor.log` (원본: `%LOCALAPPDATA%\Unity\Editor\Editor.log`에서 복사)
- `Editor.tail.txt` (Editor.log 마지막 부분 발췌)
- `Editor.errors.txt` (Editor.log에서 주요 키워드 라인 발췌)

### 관찰 내용
- 배치 로그가 `Exiting ... return code 1`로 끝남 (`unity-batch.log` 참고).
- 배치 로그 본문에는 진단 정보가 거의 없고, 실제로 확인할 만한 단서는 `Editor.errors.txt` 쪽에 더 많음.

## 3) 발견 사항(원인 후보)

### F1) Unity Connect / Cloud Project ID 요청 실패 (HTTP 403)
근거(`Editor.tail.txt` / `Editor.errors.txt`):
- `Project ID request failed ... HTTP error code 403`
- `Unknown Unity Connect error (400) ... legacy/v1/projects/<guid>`

영향:
- 헤드리스/배치 실행이 불안정해지거나, 서비스 상태에 따라 0이 아닌 종료 코드로 끝날 수 있음.

권장 다음 조치:
- Unity 에디터에서 **Project Settings → Services** 확인
  - 연결된 Cloud Project에 현재 계정이 접근 가능한지 확인하거나
  - 로컬 전용 워크플로라면 Services 연결을 해제/비활성화하거나
  - 접근 불가한 프로젝트를 가리키는 경우 Cloud Project ID를 재설정/초기화

### F2) 셰이더 fallback(대체 셰이더) 미발견 경고
근거:
- `Shader 'Standard': fallback shader 'VertexLit' not found`
- `Shader 'Hidden/Internal-GUIRoundedRect' ... fallback ... not found`

영향:
- 보통은 경고 수준이지만, 렌더 파이프라인/패키지 불일치 또는 내장 셰이더 누락 가능성을 시사할 수 있음.

권장 다음 조치:
- URP 및 관련 패키지 버전 조합이 일관적인지 확인
- `ProjectSettings/GraphicsSettings.asset` 및 URP 에셋 할당 상태 확인

### F3) D3D12 진단 인터페이스(info queue) 조회 불가 (0x80004002)
근거:
- `d3d12: failed to query info queue interface (0x80004002).`

영향:
- 대체로 무해한 편(진단용 인터페이스가 없는 상황). 실제 렌더링 문제와 함께 발생할 때만 의미가 커짐.

### F4) Bee 캐싱 클라이언트 관련 실패 라인
근거:
- `Failure while invoking caching client ...`
- `move_path failed: No error`

영향:
- 빌드/컴파일 파이프라인이 느려지거나 간헐적인 문제로 이어질 수 있음.

권장 다음 조치:
- 지속적으로 반복될 경우 `Library/Bee` 정리(직접 수동, 재임포트 시간이 늘어나는 점을 감안)

## 4) 다음 실행 개선안
(여전히 리포트 전용으로 더 ‘판정 가능’하게 만들기)
- `-executeMethod`를 추가해서 스크립트 컴파일/임포트 완료를 기다린 뒤, **PASS/FAIL 요약을 명확히 출력**하도록 개선
- 워크플로에 맞다면 배치 실행 중 Services 체크를 최소화/우회(가능한 범위에서)

---
AutoBounty(로컬 런너)가 생성함
