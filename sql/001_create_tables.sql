-- ============================================================
-- Dynamic-Crawler: Supabase 테이블 생성 스크립트
-- Supabase Dashboard → SQL Editor에서 실행
-- ============================================================
-- 사이트 설정
CREATE TABLE IF NOT EXISTS sites (
    id SERIAL PRIMARY KEY,
    site_key TEXT NOT NULL UNIQUE,
    base_url TEXT NOT NULL,
    max_concurrent_collects INT NOT NULL DEFAULT 2,
    max_concurrent_downloads INT NOT NULL DEFAULT 4,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
-- 게시글
CREATE TABLE IF NOT EXISTS posts (
    id BIGSERIAL PRIMARY KEY,
    site_key TEXT NOT NULL,
    external_id TEXT NOT NULL,
    url TEXT NOT NULL,
    title TEXT,
    status TEXT NOT NULL DEFAULT 'Discovered',
    retry_count INT NOT NULL DEFAULT 0,
    next_retry_at TIMESTAMPTZ,
    lease_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    CONSTRAINT uq_posts_site_external UNIQUE (site_key, external_id)
);
CREATE INDEX IF NOT EXISTS idx_posts_status_lease ON posts (status, lease_until);
CREATE INDEX IF NOT EXISTS idx_posts_site_key ON posts (site_key);
-- 미디어
CREATE TABLE IF NOT EXISTS media (
    id BIGSERIAL PRIMARY KEY,
    post_id BIGINT NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    media_url TEXT NOT NULL,
    content_type TEXT,
    sha256 TEXT,
    byte_size BIGINT,
    local_path TEXT,
    status TEXT NOT NULL DEFAULT 'PendingDownload',
    retry_count INT NOT NULL DEFAULT 0,
    next_retry_at TIMESTAMPTZ,
    lease_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_media_sha256 ON media (sha256)
WHERE sha256 IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_media_status_lease ON media (status, lease_until);
CREATE INDEX IF NOT EXISTS idx_media_post_id ON media (post_id);
-- 댓글
CREATE TABLE IF NOT EXISTS comments (
    id BIGSERIAL PRIMARY KEY,
    post_id BIGINT NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    author TEXT,
    content TEXT NOT NULL,
    commented_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_comments_post_id ON comments (post_id);
-- 초기 사이트 데이터
INSERT INTO sites (site_key, base_url)
VALUES ('aagag', 'https://aagag.com') ON CONFLICT (site_key) DO NOTHING;