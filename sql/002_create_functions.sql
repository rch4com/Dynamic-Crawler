-- ============================================================
-- Dynamic-Crawler: Supabase RPC functions
-- Run in the Supabase SQL Editor
-- ============================================================

CREATE OR REPLACE FUNCTION claim_next_post(p_site_key TEXT, p_lease_seconds INT)
RETURNS SETOF posts AS $$
BEGIN
    RETURN QUERY
    UPDATE posts
    SET status = 'Collecting',
        lease_until = NOW() + (p_lease_seconds || ' seconds')::INTERVAL,
        updated_at = NOW()
    WHERE id = (
        SELECT id
        FROM posts
        WHERE site_key = p_site_key
            AND status = 'Discovered'
            AND (lease_until IS NULL OR lease_until < NOW())
            AND (next_retry_at IS NULL OR next_retry_at <= NOW())
        ORDER BY created_at
        LIMIT 1 FOR UPDATE SKIP LOCKED
    )
    RETURNING *;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION claim_next_media(p_site_key TEXT, p_lease_seconds INT)
RETURNS SETOF media AS $$
BEGIN
    RETURN QUERY
    UPDATE media
    SET status = 'Downloading',
        lease_until = NOW() + (p_lease_seconds || ' seconds')::INTERVAL
    WHERE id = (
        SELECT m.id
        FROM media m
        INNER JOIN posts p ON m.post_id = p.id
        WHERE p.site_key = p_site_key
            AND m.status = 'PendingDownload'
            AND (m.lease_until IS NULL OR m.lease_until < NOW())
            AND (m.next_retry_at IS NULL OR m.next_retry_at <= NOW())
        ORDER BY m.created_at
        LIMIT 1 FOR UPDATE SKIP LOCKED
    )
    RETURNING *;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION rollback_orphaned_posts()
RETURNS INT AS $$
DECLARE affected INT;
BEGIN
    UPDATE posts
    SET status = 'Discovered',
        retry_count = retry_count + 1,
        next_retry_at = NOW() + make_interval(mins => CAST(POWER(2, retry_count + 1) AS INT)),
        lease_until = NULL,
        updated_at = NOW()
    WHERE status = 'Collecting'
        AND lease_until < NOW();

    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION rollback_orphaned_media()
RETURNS INT AS $$
DECLARE affected INT;
BEGIN
    UPDATE media
    SET status = 'PendingDownload',
        retry_count = retry_count + 1,
        next_retry_at = NOW() + make_interval(mins => CAST(POWER(2, retry_count + 1) AS INT)),
        lease_until = NULL
    WHERE status = 'Downloading'
        AND lease_until < NOW();

    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION replace_post_comments(p_post_id BIGINT, p_comments JSONB)
RETURNS VOID AS $$
BEGIN
    DELETE FROM comments WHERE post_id = p_post_id;

    INSERT INTO comments (post_id, author, content, commented_at)
    SELECT
        p_post_id,
        comment_item->>'author',
        comment_item->>'content',
        NULLIF(comment_item->>'commented_at', '')::timestamptz
    FROM jsonb_array_elements(COALESCE(p_comments, '[]'::jsonb)) AS comment_item;
END;
$$ LANGUAGE plpgsql;
