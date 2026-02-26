# DynamicCrawler.Data.Supabase

`DynamicCrawler.Data.Supabase`는 `Core` 프로젝트에서 정의한 Persistence Interface(`IRepository`)를 실체화(Implementation)하는 데이터 영속성 모듈입니다. 현재 DB로 Supabase(PostgreSQL 기반) 서버리스 인프라를 사용하고 있습니다.

## 주요 특징

- **Repository 구현체**  
  `IPostRepository`, `IMediaRepository` 등의 인터페이스를 상속하여, RLS(Row Level Security)가 적용된 `SupabasePostgrestClient` 기반의 RPC 및 Table 쿼리를 수행합니다.
- **DTO 및 Mappers**  
  Supabase의 `BaseModel`을 상속해야만 ORM 통신이 가능한 특성상, `SupabasePost`, `SupabaseMedia` 등의 내부 DTO 클래스를 두고, `PostMapper`, `MediaMapper`를 통해 Core 엔티티로 상태 손실 없이(null-safe 포함) 쌍방향 매핑을 수행합니다.
- **OnConflict(UPSERT) 충돌 제어**
  중복 데이터 삽입 에러(Unique Constraint)가 나지 않도록 복합 인덱스(예: `site_key,external_id`)를 기준으로 Upsert 충돌 제어 옵션을 투입하여 데이터 무결성을 유지합니다.
- **Health Checks 연계**
  DB 인프라의 활성 상태 확인을 위해 `SupabaseHealthCheck` 클래스를 제공하여 Worker가 시작/런타임에 서비스 생존 여부를 주기적으로 체크하게 도와줍니다.

## 교체 용이성
DB를 EF Core + MSSQL과 같은 전통적 형태로 변환하고자 한다면, 이 프로젝트 레벨만 새로 교체(`DynamicCrawler.Data.EFCore`)하여 Worker 계층에 등록하면 곧바로 작동하는 호환성을 갖추고 있습니다.
