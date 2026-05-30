# VeilView

**VeilView**는 Windows에서 사용할 수 있는 가벼운 플로팅 탭 브라우저입니다.
브라우저 화면은 마우스로 클릭하고 스크롤할 수 있지만, 키보드 입력 포커스는 사용자가 원래 사용하던 창에 남겨두는 것을 목표로 합니다.

예를 들어 메인 작업 창은 그대로 키보드 입력을 받게 두고, VeilView에서는 웹페이지를 마우스로만 탐색하는 식으로 사용할 수 있습니다.

> VeilView는 특정 게임, 특정 서비스, 특정 프로그램과 제휴되어 있지 않은 독립 프로젝트입니다.  
> 다른 프로세스의 메모리 읽기, 패킷 변조, DLL 인젝션, 글로벌 키보드 훅, 자동 입력 매크로 기능은 포함하지 않습니다.

## 주요 기능

- WebView2 기반 플로팅 브라우저
- VeilView 내부 탭 지원
- 웹페이지의 `새 탭에서 열기`, `새 창에서 열기`, `target="_blank"`, `window.open()` 요청을 VeilView 내부 탭으로 처리
- 키보드 포커스를 기존 활성 창에 유지하는 **키보드 보존** 모드
- 주소창이나 웹페이지에 직접 입력할 때 사용하는 **주소 입력** 모드
- 뒤로 / 앞으로 / 새로고침 / URL 이동
- 항상 위 표시 토글
- 투명도 버튼 `0%`, `30%`, `70%`
- 마지막 탭 목록, 선택 탭, 창 위치, 창 크기, 투명도 저장
- 배포용 단일 실행 파일 `VeilView.exe` 빌드 지원

## 화면 용어

| 표시 | 의미 |
| --- | --- |
| `키보드 보존` | VeilView를 클릭해도 키보드 포커스를 가져오지 않는 상태 |
| `주소 입력` | 주소창이나 웹페이지에 글자를 입력할 수 있는 상태 |
| `보존 복귀` | 입력 상태를 끝내고 다시 키보드 보존 상태로 돌아가는 버튼 |
| `0%` / `30%` / `70%` | 창 투명도 선택 버튼 |

## 실행 요구 사항

- Windows 10 1809 이상 또는 Windows 11
- x64 환경 권장
- Microsoft Edge WebView2 Runtime 필요

대부분의 Windows 10/11 환경에는 WebView2 Runtime이 이미 설치되어 있습니다. 실행 시 WebView2 관련 오류가 발생하면 `REPAIR_WEBVIEW2_RUNTIME.cmd`를 실행하거나 Microsoft Edge WebView2 Runtime을 설치하면 됩니다.

## 빌드 요구 사항

- .NET 8 SDK

`build.ps1`은 PC에 .NET 8 SDK가 없으면 프로젝트 폴더의 `.dotnet` 폴더 안에 SDK를 자동으로 설치합니다. 관리자 권한은 필요하지 않습니다.

## 빠른 빌드

압축을 푼 뒤 프로젝트 루트에서 실행합니다.

```powershell
.\BUILD_SINGLE_EXE.cmd
```

PowerShell에서 직접 실행하려면 다음 명령을 사용합니다.

```powershell
.\build.ps1
```

성공하면 아래 파일이 생성됩니다.

```text
dist\VeilView.exe
```

`dist\VeilView.exe`는 단일 실행 파일 배포용입니다. 바탕화면이나 다른 폴더로 파일 하나만 옮겨 실행할 수 있도록 빌드됩니다.

## 폴더형 빌드

일부 환경에서 단일 실행 파일 방식이 WebView2 네이티브 로더 문제를 일으키면 폴더형 빌드를 사용할 수 있습니다.

```powershell
.\BUILD_PORTABLE_FOLDER.cmd
```

성공하면 아래 폴더가 생성됩니다.

```text
dist-folder\
```

이 방식은 `dist-folder` 안의 파일들을 모두 함께 보관해야 합니다. `exe`만 따로 빼서 실행하면 안 됩니다.

## 사용 방법

1. 키보드 입력을 유지하고 싶은 창을 먼저 클릭합니다.
2. `VeilView.exe`를 실행합니다.
3. 상태가 `키보드 보존`이면 VeilView를 마우스로 클릭하거나 스크롤해도 키보드 포커스는 기존 창에 남습니다.
4. 주소창이나 웹페이지 입력칸에 글자를 입력해야 할 때는 `주소 입력`을 누릅니다.
5. 입력이 끝나면 Enter, `이동`, 또는 `보존 복귀`를 사용해 다시 키보드 보존 상태로 돌아갑니다.

## 탭 사용 방법

- `+`: VeilView 내부 새 탭 열기
- `×`: 현재 탭 닫기
- 웹페이지에서 `새 탭에서 열기` 또는 `새 창에서 열기`를 선택하면 외부 브라우저가 아니라 VeilView 내부 탭으로 열립니다.
- 프로그램 종료 시 열린 탭 목록과 선택된 탭이 저장되고, 다음 실행 때 복원됩니다.

## 투명도

VeilView는 투명도를 세 단계로만 변경합니다.

| 버튼 | 의미 | 실제 창 불투명도 |
| --- | --- | --- |
| `0%` | 투명도 없음 | 100% |
| `30%` | 중간 투명도 | 70% |
| `70%` | 높은 투명도 | 30% |

기본값은 `0%`입니다. 기존 설정 때문에 투명하게 시작한다면 `0%` 버튼을 한 번 누르고 종료하면 다음 실행부터 저장됩니다.

## 명령행 옵션

예시:

```powershell
VeilView.exe --url https://www.youtube.com --tab https://www.google.com --x 100 --y 100 --width 960 --height 540 --transparency 30 --topmost true
```

| 옵션 | 설명 |
| --- | --- |
| `--url` | 시작 URL. 지정하면 저장된 탭 대신 이 URL 하나로 시작합니다. |
| `--tab` | 시작 탭을 추가합니다. 여러 번 지정할 수 있습니다. |
| `--x` / `--y` | 시작 위치를 지정합니다. |
| `--width` / `--height` | 시작 크기를 지정합니다. |
| `--transparency` | 투명도를 지정합니다. `0`, `30`, `70` 중 가장 가까운 값으로 보정됩니다. |
| `--opacity` | 호환용 옵션입니다. 가능하면 `--transparency`를 사용하세요. |
| `--topmost` | 항상 위 표시 여부입니다. `true` 또는 `false`를 사용합니다. |

## 설정 저장 위치

```text
%LOCALAPPDATA%\VeilView\settings.json
%LOCALAPPDATA%\VeilView\WebView2UserData\
```

설정을 완전히 초기화하려면 위 폴더를 삭제하면 됩니다.

## GitHub에 올리는 방법

프로젝트 폴더 안에서 다음 명령을 실행합니다.

```bash
git init
git add .
git commit -m "VeilView 초기 릴리스"
git branch -M main
git remote add origin https://github.com/YOUR_NAME/VeilView.git
git push -u origin main
```

`VeilView_v0.2.0` 같은 상위 폴더를 한 번 더 감싸서 올리지 말고, 이 저장소의 파일들이 GitHub 저장소 루트에 바로 보이도록 올리는 것을 권장합니다.

## GitHub Actions

- `.github/workflows/build.yml`: push 또는 pull request마다 Windows x64 단일 실행 파일을 빌드하고 artifact로 업로드합니다.
- `.github/workflows/release.yml`: `v0.2.0` 같은 태그를 푸시하면 GitHub Release를 만들고 `VeilView.exe`와 압축 파일을 업로드합니다.

릴리스 생성 예시:

```bash
git tag v0.2.0
git push origin v0.2.0
```

릴리스 본문은 `RELEASE_NOTES.md`를 사용합니다. 릴리스 전에 이 파일의 내용을 원하는 한국어 안내문으로 수정한 뒤 태그를 푸시하세요.

## 배포 권장 방식

일반 사용자는 소스 전체를 받을 필요가 없습니다. GitHub Release에는 다음 파일을 올리는 것을 권장합니다.

```text
VeilView.exe
```

단, 실행 PC에 WebView2 Runtime이 없으면 실행 전 Runtime 설치가 필요합니다. README와 릴리스 본문에 이 내용을 함께 안내하는 것을 권장합니다.

## 동작 원리

VeilView는 Windows 창 스타일과 마우스 활성화 메시지 처리를 이용해, 창을 마우스로 조작하되 키보드 포커스를 가져오지 않도록 구성합니다.

브라우저 렌더링에는 Microsoft Edge WebView2를 사용합니다. VeilView는 다른 프로세스의 메모리를 읽거나, 키 입력을 생성하거나, 대상 프로그램에 코드를 주입하지 않습니다.

## 알려진 제한

- 웹페이지 안에 글자를 입력하려면 `주소 입력` 상태가 필요합니다.
- 일부 전체화면/독점 모드 프로그램은 일반 창 포커스 규칙과 다르게 동작할 수 있습니다.
- 각 서비스나 게임의 이용 약관은 사용자가 직접 확인해야 합니다.
- WebView2 Runtime이 없는 PC에서는 실행 전에 Runtime 설치가 필요합니다.

## 라이선스

MIT License를 사용합니다. 자세한 내용은 `LICENSE` 파일을 확인하세요.
