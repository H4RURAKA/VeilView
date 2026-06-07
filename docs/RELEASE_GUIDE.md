# 릴리스 가이드

## 릴리스 전 확인

```powershell
.\BUILD_SINGLE_EXE.cmd
```

확인 항목:

- `dist\VeilView.exe` 생성 여부
- 단일 exe 실행 여부
- 내부 탭 동작
- 새 탭에서 열기가 VeilView 내부 탭으로 열리는지
- `제스처` 설정창이 항상 위 상태에서도 보이는지
- `불투명도` 설정창이 항상 위 상태에서도 보이는지
- `↔` 제스처가 `직접 입력` / `작업창 복귀`를 전환하는지

## 태그 릴리스

```powershell
git add .
git commit -m "VeilView v0.3.2 릴리스 준비"
git push

git tag v0.3.2
git push origin v0.3.2
```

GitHub Actions가 성공하면 Release에 다음 파일이 첨부됩니다.

```text
VeilView.exe
VeilView-win-x64.zip
```

릴리스 본문은 `RELEASE_NOTES.md`를 사용합니다.
