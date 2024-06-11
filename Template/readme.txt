# 엑셀 테이블 작성 가이드

## 엑셀 파일 이름 정의
- Enum을 정의하는 테이블인 경우 파일의 끝이 _Enum이어야 합니다.
- Const를 정의하는 테이블인 경우 파일의 끝이 _.Const이어야 합니다.
- 일반 테이블에는 적용사항 없습니다.

## 엑셀 시트 이름 정의
- 테이블 이름으로 정의하거나 .를 붙여 상세정보를 기입합니다.
  ```
  Reward
  Reward.Arena
  Reward.Raid
  ```
- 같은 테이블은 컨버터 실행시 하나로 합쳐집니다.
- 합쳐질 테이블에 정의된 컬럼은 반드시 일치해야 합니다.

## 컬럼 정의
 - 지원하는 타입 리스트
   - int
   - long
   - double
   - float
   - string
   - bool
   - DateTime
   - TimeSpan
   - dsl
   - DateRange
    ```
    2021-02-16 ~ 2021-03-16
    2021-02-16 00:00:00 ~ 2021-03-16 00:00:00
    뒤에 있는 시간은 포함되지 않음(2021-03-15 23:59:59까지만 적용)
    ```
   - list
    ```
    [string] : 가 & 나 & 다
    [int] : 1 & 2 & 3
    [$Character] : Character.airen & Character.foo

    (& 대신 줄개행 가능)
    ```
    - map
    ```
    {string:int}
     한글1 : 1
     한글2 : 2
     한글3 : 3

    {$Character:$Reward}
     Character.airen : 보상.경험치.C
     Character.foo : 보상.경험치.UC
    ```
- ?를 뒤에 붙여 nullable 타입임을 명시할 수 있습니다.
  ```
  int?
  DateTime?
  ```
- 외부 테이블 키를 참조하려면 컬럼 타입 앞에 $를 적습니다.
  ```
  $Reward
  $Character
  ```
- 외부 테이블의 특정 컬럼을 참조하려면 . 뒤에 컬럼 이름을 적습니다.
  ```
  $Reward.RewardList
  ```
- 테이블의 키를 정의하려면 키 컬럼 타입 앞에 *를 적습니다. 이 값은 중복될 수 없습니다.
  ```
  *string
  *int
  *$Character
  ```
- 그룹으로 묶이는 키 타입인 경우에는 괄호로 감싸줍니다.
  ```
  (int)
  ($Reward)
  ```

## Const 데이터 기입
 - Const 테이블에 정의된 값을 일반 테이블에서 사용하려는 경우 Const:{Const 시트명}:{Const 이름}으로 사용합니다. 
    ```
    Const:Raid:Fee
    ```

## DSL
 - 현재 지원하는 DSL 리스트 : design/converter/dsl.json 의 dsl 참고
    ```
{{~ for pair in dsl ~}}
    {{ pair.key }}({{~ for param in pair.value ~}}
{{ param.type }}{{if !for.last }}, {{ end }}{{ end }})
{{~ for param in pair.value ~}}
     - {{ param.type }}({{ param.desc }})
{{~ end ~}}
{{~ if !for.last ~}}

{{~ end ~}}
{{~ end ~}}
    ```
 - 줄개행 또는 &를 이용해서 리스트 형태로 사용할 수 있습니다.