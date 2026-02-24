-- ============================================================
-- Dynamic-Crawler: Supabase RPC 함수 (Lease/Lock 패턴)
-- Supabase Dashboard → SQL Editor에서 실행
-- ============================================================
-- 다음 크롤링 대상 게시글을 lease 획득 (FOR UPDATE SKIP LOCKED)
CREATE OR REPLACE FUNCTION claim_next_post(p_site_key TEXT, p_lease_seconds INT) RETURNS SETOF posts AS $$ BEGIN RETURN QUERY
UPDATE posts
SET status = 'Collecting',
    lease_until = NOW() + (p_lease_seconds || ' seconds')::INTERVAL,
    updated_at = NOW()
WHERE id = (
        SELECT id
        FROM posts
        WHERE site_key = p_site_key
            AND status = 'Discovered'
            AND (
                lease_until IS NULL
                OR lease_until < NOW()
            )
        ORDER BY created_at
        LIMIT 1 FOR
        UPDATE SKIP LOCKED
    )
RETURNING *;
END;
$$ LANGUAGE plpgsql;
-- 다음 다운로드 대상 미디어를 lease 획득
CREATE OR REPLACE FUNCTION claim_next_media(p_site_key TEXT, p_lease_seconds INT) RETURNS SETOF media AS $$ BEGIN RETURN QUERY
UPDATE media
SET status = 'Downloading',
    lease_until = NOW() + (p_lease_seconds || ' seconds')::INTERVAL
WHERE id = (
        SELECT m.id
        FROM media m
            INNER JOIN posts p ON m.post_id = p.id
        WHERE p.site_key = p_site_key
            AND m.status = 'PendingDownload'
            AND (
                m.lease_until IS NULL
                OR m.lease_until < NOW()
            )
        ORDER BY m.created_at
        LIMIT 1 FOR
        UPDATE SKIP LOCKED
    )
RETURNING *;
END;
$$ LANGUAGE plpgsql;
-- orphaned 상태 롤백 (서비스 재시작 시 호출)
CREATE OR REPLACE FUNCTION rollback_orphaned_posts() RETURNS INT AS $$
DECLARE affected INT;
BEGIN
UPDATE posts
SET status = 'Discovered',
    lease_until = NULL,
    updated_at = NOW()
WHERE status = 'Collecting'
    AND lease_until < NOW();
GET DIAGNOSTICS affected = ROW_COUNT;
RETURN affected;
END;
$$ LANGUAGE plpgsql;
CREATE OR REPLACE FUNCTION rollback_orphaned_media() RETURNS INT AS $$
DECLARE affected INT;
BEGIN
UPDATE media
SET status = 'PendingDownload',
    lease_until = NULL
WHERE status = 'Downloading'
    AND lease_until < NOW();
GET DIAGNOSTICS affected = ROW_COUNT;
RETURN affected;
END;
$$ LANGUAGE plpgsql;