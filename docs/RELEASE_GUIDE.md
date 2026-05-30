# 릴리스 작성 가이드

이 프로젝트는 GitHub Release 본문을 한국어로 작성하는 것을 기준으로 합니다.

## 릴리스 전에 수정할 파일

릴리스 태그를 올리기 전에 아래 파일을 먼저 확인하세요.

```text
RELEASE_NOTES.md
CHANGELOG.md
README.md
```

`RELEASE_NOTES.md`의 `{{TAG}}`는 GitHub Actions에서 실제 태그 이름으로 자동 치환됩니다.

## 릴리스 생성 명령

예시:

```bash
git tag v0.2.0
git push origin v0.2.0
```

태그를 푸시하면 `.github/workflows/release.yml`이 실행되고, 다음 파일이 Release Asset으로 업로드됩니다.

```text
VeilView.exe
VeilView-win-x64.zip
```

## 수동 릴리스 본문 예시

```markdown
# VeilView v0.2.0

VeilView는 키보드 포커스를 기존 활성 창에 보존하면서 웹페이지를 마우스로 탐색할 수 있는 Windows용 플로팅 탭 브라우저입니다.

## 주요 변경점

- 내부 탭 기능 추가
- 새 창/새 탭 요청을 VeilView 내부 탭으로 처리
- 투명도 버튼을 0%, 30%, 70% 세 단계로 정리
- 단일 실행 파일 배포 지원

## 다운로드

일반 사용자는 `VeilView.exe`만 다운로드하면 됩니다.

## 실행 조건

- Windows 10 1809 이상 또는 Windows 11
- Microsoft Edge WebView2 Runtime 필요
```
