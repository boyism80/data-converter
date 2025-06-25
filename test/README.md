# Data Converter Go Tests

이 폴더는 data-converter에서 생성된 Go 코드를 테스트하는 독립적인 테스트 환경입니다.

## 폴더 구조

```
test/
├── generated/          # 자동 생성된 Go 모델 코드
│   ├── model.go       # data-converter에서 생성된 Go 모델 (자동 생성)
│   └── go.mod         # 생성된 모델의 Go 모듈 정의
├── model_test.go      # Go 모델을 테스트하는 테스트 코드
├── go.mod             # 테스트 프로젝트의 Go 모듈 정의
├── update-model.bat   # Windows용 모델 업데이트 스크립트
├── update-model.sh    # Linux/Mac용 모델 업데이트 스크립트
└── README.md          # 이 파일
```

## 모델 업데이트

**중요**: `generated/model.go` 파일은 자동 생성되므로 직접 수정하지 마세요!

### Windows
```bash
.\update-model.bat
```

### Linux/Mac
```bash
./update-model.sh
```

이 스크립트들은:
1. data-converter를 실행하여 최신 모델 코드 생성
2. 생성된 `model.go`를 `generated/` 폴더로 자동 복사

## 테스트 실행

### 기본 테스트 실행
```bash
go test -v
```

### 개별 테스트 실행
```bash
# 시간 변환 테스트
go test -v -run TestTimeConversion

# 컨테이너 타입 테스트
go test -v -run TestContainerTypes

# JSON 로딩 테스트 (JSON 경로 필요)
go test -v -run TestLoadContainer -args -json="../bin/output/json/server"
```

### JSON 경로 지정하여 전체 테스트
```bash
go test -v -args -json="../bin/output/json/server"
```

## 테스트 종류

### 1. TestTimeConversion
- `time.Duration` 타입 변환이 올바르게 작동하는지 테스트
- `MobSpawn.Rezen` 필드의 시간 값 검증

### 2. TestLoadContainer
- 커맨드라인으로 제공된 JSON 경로에서 모든 JSON 파일을 로드
- 각 JSON 파일의 파싱 가능 여부 검증
- JSON 경로가 제공되지 않으면 테스트 스킵

### 3. TestContainerTypes
- `Container` 구조체의 기본적인 타입 동작 테스트
- Achievement 추가/조회 기능 검증

## 사용 예시

```bash
# 1. 모델 업데이트
.\update-model.bat

# 2. 기본 테스트 실행
go test -v

# 3. JSON 데이터로 전체 테스트
go test -v -args -json="../bin/output/json/server"
```

## 주의사항

- `generated/model.go`는 자동 생성 파일이므로 직접 수정하지 마세요
- 모델이 변경되었다면 `update-model` 스크립트를 실행하여 최신 버전으로 업데이트하세요
- JSON 로딩 테스트는 data-converter가 JSON 파일을 생성한 후에만 실행 가능합니다 