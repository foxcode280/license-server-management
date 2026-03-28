BEGIN;

ALTER TABLE public.subscriptions
    ADD COLUMN IF NOT EXISTS status_description text;

CREATE OR REPLACE FUNCTION public.sp_get_device_os_types()
RETURNS TABLE(
    id bigint,
    os_name character varying,
    description text,
    status character varying
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT dot.id,
           dot.os_name,
           dot.description,
           dot.status
    FROM public.device_os_types dot
    ORDER BY dot.id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_reject_subscription(
    IN p_subscription_id bigint,
    IN p_reason text,
    IN p_updated_by bigint,
    IN p_updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.subscriptions
    SET status = 'REJECTED',
        status_description = COALESCE(NULLIF(p_reason, ''), 'Subscription rejected'),
        updated_by = p_updated_by,
        updated_at = p_updated_at
    WHERE id = p_subscription_id;
END;
$$;

COMMIT;
