# AutoBounty 리포트 (재실행 / 리포트 전용)

- 작성 시각: 2026-02-03 (KST)
- 프로젝트: `C:\Users\User\Documents\GitHub\hwing`
- Unity: 6000.2.15f1 (URP)
- 실행 모드: **리포트 전용(프로젝트 파일 수정 없음)**

## 1) 스냅샷(현재 상태 기록)
- Git 브랜치: `git-branch.txt`
- Git 변경 상태: `git-status.txt`
- Git 변경 요약 통계: `git-diff-stat.txt`

## 2) Unity 배치모드 실행
실행 커맨드(대략):
- `Unity.exe -batchmode -nographics -projectPath <proj> -quit -logFile <report>\unity-batch.log`

생성/수집 파일:
- `unity-batch.log`
- `Editor.log` (원본: `%LOCALAPPDATA%\Unity\Editor\Editor.log`에서 복사)
- `Editor.tail.txt` (마지막 200줄 발췌)
- `Editor.errors.txt` (키워드 기반 발췌)

### 관찰 내용
- 배치 로그가 `return code 1`로 종료됨 (`unity-batch.log` 참고).
- **중요:** 프로젝트를 Services에 연결했다고 해도, 배치 실행 환경에서 Unity Connect 관련 호출이 계속 실패할 수 있음(아래 F1).

## 3) 발견 사항(원인 후보)

### F1) Unity Connect / Cloud Project 관련 요청 실패가 계속 발생 (HTTP 403)
근거(`Editor.errors.txt`):
- `Project ID request failed ... HTTP error code 403`
- `Unknown Unity Connect error (400) ... legacy/v1/projects/<guid>`

의미(추정):
- 현재 프로젝트가 참조하는 Cloud Project(또는 Organization/권한)가 **배치 실행 시점에서 접근 불가**로 판단되고 있음.
- UI에서 링크가 보이더라도, 실제로는
  - 다른 조직/계정으로 연결돼 있거나,
  - 해당 프로젝트 멤버 권한이 부족하거나,
  - 로컬 캐시가 오래되어 잘못된 프로젝트 GUID로 요청하는 경우가 있을 수 있음.

권장 다음 조치(우선순위 순):
1) Unity Dashboard(웹)에서 해당 프로젝트가 **현재 계정/조직에 실제로 보이는지** 확인
2) Unity 에디터에서 **Project Settings → Services**에서
   - Organization이 맞는지
   - 프로젝트 멤버/권한이 정상인지 확인
3) 임시 우회(로컬 전용 작업이면): Services 연결을 끊거나(가능한 경우) 관련 기능을 비활성화해서 배치 실행이 네트워크/권한 이슈에 덜 민감하게 만들기

### F2) D3D12 진단 인터페이스(info queue) 조회 불가 (0x80004002)
근거:
- `d3d12: failed to query info queue interface (0x80004002).`

영향:
- 대체로 무해한 편(진단용 인터페이스 미지원). 렌더링 오류와 함께 나타날 때만 의미가 커짐.

## 4) 결론
- 이번 재실행에서도 **Unity Connect 관련 403이 계속 발생**했고, 배치 실행은 `return code 1`로 종료됨.
- 즉, “프로젝트 링크”만으로는 해결이 안 됐고, **권한/조직/대시보드 상 프로젝트 접근성** 쪽을 더 확인해야 함.

## 5) 다음 AutoBounty 개선 제안(리포트 품질)
- 배치 실행을 `-executeMethod` 기반으로 바꿔서
  - 컴파일 완료/실패를 더 명확히 판정하고
  - 로그에서 403 같은 네트워크/서비스 이슈를 별도 섹션으로 분리해서 보여주기

---
AutoBounty(로컬 런너)가 생성함
