-- License workflow migration
-- Target flow:
-- 1. Portal creates subscription with status PENDING
-- 2. Admin approves subscription
-- 3. Admin generates license once
-- 4. Download returns only an existing saved license
-- 5. EMS activates against OS-specific allocation limits

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

--------------------------------------------------
-- 1. Schema updates
--------------------------------------------------

ALTER TABLE public.subscriptions
    ALTER COLUMN status SET DEFAULT 'PENDING';

ALTER TABLE public.license_activations
    ADD COLUMN IF NOT EXISTS os_type_id bigint,
    ADD COLUMN IF NOT EXISTS status character varying(20) DEFAULT 'ACTIVE',
    ADD COLUMN IF NOT EXISTS activation_token text,
    ADD COLUMN IF NOT EXISTS deactivated_at timestamp without time zone,
    ADD COLUMN IF NOT EXISTS updated_by bigint,
    ADD COLUMN IF NOT EXISTS updated_at timestamp without time zone;

ALTER TABLE public.ems_license
    ADD COLUMN IF NOT EXISTS activation_token text,
    ADD COLUMN IF NOT EXISTS os_type_id bigint;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'ux_licenses_subscription_id'
    ) THEN
        CREATE UNIQUE INDEX ux_licenses_subscription_id
            ON public.licenses(subscription_id);
    END IF;
END;
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'ix_license_activations_license_os_status'
    ) THEN
        CREATE INDEX ix_license_activations_license_os_status
            ON public.license_activations(license_id, os_type_id, status);
    END IF;
END;
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'ix_subscription_license_allocations_subscription_os'
    ) THEN
        CREATE INDEX ix_subscription_license_allocations_subscription_os
            ON public.subscription_license_allocations(subscription_id, os_type_id);
    END IF;
END;
$$;

--------------------------------------------------
-- 2. Approve subscription
--------------------------------------------------

CREATE OR REPLACE PROCEDURE public.sp_approve_subscription(
    IN p_subscription_id bigint,
    IN p_updated_by bigint,
    IN p_updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_status varchar;
BEGIN
    SELECT status
    INTO v_status
    FROM public.subscriptions
    WHERE id = p_subscription_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Subscription not found';
    END IF;

    IF v_status <> 'PENDING' THEN
        RAISE EXCEPTION 'Only PENDING subscriptions can be approved';
    END IF;

    UPDATE public.subscriptions
    SET status = 'APPROVED',
        updated_by = p_updated_by,
        updated_at = p_updated_at
    WHERE id = p_subscription_id;
END;
$$;

--------------------------------------------------
-- 3. Get generation context
--------------------------------------------------

CREATE OR REPLACE FUNCTION public.sp_get_license_generation_context(
    p_subscription_id bigint
)
RETURNS TABLE(
    subscription_id bigint,
    subscription_status character varying,
    existing_license_key text
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        s.id,
        s.status,
        l.license_key
    FROM public.subscriptions s
    LEFT JOIN LATERAL (
        SELECT li.license_key
        FROM public.licenses li
        WHERE li.subscription_id = s.id
        ORDER BY li.issue_date DESC NULLS LAST, li.id DESC
        LIMIT 1
    ) l ON true
    WHERE s.id = p_subscription_id;
END;
$$;

--------------------------------------------------
-- 4. License payload helpers
--------------------------------------------------

CREATE OR REPLACE FUNCTION public.sp_get_license_payload(
    p_subscription_id bigint
)
RETURNS TABLE(
    company_id bigint,
    company_name character varying,
    plan_name character varying,
    license_duration_type character varying,
    license_mode character varying,
    start_date timestamp without time zone,
    expiry_date timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.id,
        c.company_name,
        p.plan_name,
        p.license_duration_type,
        s.license_mode,
        s.start_date,
        s.end_date
    FROM public.subscriptions s
    JOIN public.companies c ON c.id = s.company_id
    JOIN public.subscription_plans p ON p.id = s.plan_id
    WHERE s.id = p_subscription_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_license_allocations(
    p_subscription_id bigint
)
RETURNS TABLE(
    os_type_id bigint,
    allocated_count integer
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        sla.os_type_id,
        sla.allocated_count
    FROM public.subscription_license_allocations sla
    WHERE sla.subscription_id = p_subscription_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_product_features(
    p_subscription_id bigint
)
RETURNS TABLE(
    feature_name character varying
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT pf.feature_name
    FROM public.subscriptions s
    JOIN public.subscription_plans sp ON sp.id = s.plan_id
    JOIN public.products pr ON pr.id = sp.product_id
    JOIN public.product_features pf ON pf.product_id = pr.id
    WHERE s.id = p_subscription_id;
END;
$$;

--------------------------------------------------
-- 5. Insert license
--------------------------------------------------

CREATE OR REPLACE FUNCTION public.sp_insert_license(
    p_subscription_id bigint,
    p_license_key text,
    p_license_duration_type character varying,
    p_license_mode character varying,
    p_created_by bigint
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_expiry timestamp;
    v_status varchar;
BEGIN
    SELECT end_date, status
    INTO v_expiry, v_status
    FROM public.subscriptions
    WHERE id = p_subscription_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid subscription id';
    END IF;

    IF v_status <> 'APPROVED' THEN
        RAISE EXCEPTION 'License can only be generated for APPROVED subscriptions';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM public.licenses
        WHERE subscription_id = p_subscription_id
    ) THEN
        RAISE EXCEPTION 'License already generated for this subscription';
    END IF;

    INSERT INTO public.licenses(
        uuid,
        subscription_id,
        license_key,
        license_duration_type,
        issue_date,
        expiry_date,
        status,
        created_by,
        created_at,
        license_mode
    )
    VALUES(
        gen_random_uuid(),
        p_subscription_id,
        p_license_key,
        p_license_duration_type,
        NOW(),
        v_expiry,
        'ACTIVE',
        p_created_by,
        NOW(),
        p_license_mode
    );
END;
$$;

--------------------------------------------------
-- 6. Download only existing license
--------------------------------------------------

CREATE OR REPLACE FUNCTION public.sp_get_downloadable_license(
    p_subscription_id bigint
)
RETURNS TABLE(
    subscription_id bigint,
    subscription_status character varying,
    license_key text
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        s.id,
        s.status,
        l.license_key
    FROM public.subscriptions s
    JOIN public.licenses l
        ON l.subscription_id = s.id
    WHERE s.id = p_subscription_id
    ORDER BY l.issue_date DESC NULLS LAST, l.id DESC
    LIMIT 1;
END;
$$;

--------------------------------------------------
-- 7. Status update helper
--------------------------------------------------

CREATE OR REPLACE PROCEDURE public.sp_update_subscription_status(
    IN p_subscription_id bigint,
    IN p_status character varying,
    IN p_updated_by bigint,
    IN p_updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.subscriptions
    SET status = p_status,
        updated_by = p_updated_by,
        updated_at = p_updated_at
    WHERE id = p_subscription_id;
END;
$$;

--------------------------------------------------
-- 8. Activation with OS/device allocation check
--------------------------------------------------

CREATE OR REPLACE FUNCTION public.sp_activate_license(
    p_license_key text,
    p_machine_id text,
    p_hostname text,
    p_ip_address text,
    p_os_type_id bigint,
    p_created_by bigint
)
RETURNS TABLE(
    status_code text,
    status_message text,
    activation_token text
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_license_id bigint;
    v_subscription_id bigint;
    v_expiry_date timestamp;
    v_license_status text;
    v_subscription_status text;
    v_allocated_count int;
    v_activation_count int;
    v_token text;
BEGIN
    SELECT id, subscription_id, expiry_date, status
    INTO v_license_id, v_subscription_id, v_expiry_date, v_license_status
    FROM public.licenses
    WHERE license_key = p_license_key;

    IF NOT FOUND THEN
        RETURN QUERY SELECT 'INVALID_LICENSE', 'License key not found', NULL::text;
        RETURN;
    END IF;

    IF v_license_status <> 'ACTIVE' THEN
        RETURN QUERY SELECT 'LICENSE_INACTIVE', 'License is not active', NULL::text;
        RETURN;
    END IF;

    SELECT status
    INTO v_subscription_status
    FROM public.subscriptions
    WHERE id = v_subscription_id;

    IF v_subscription_status NOT IN ('LICENSE_ISSUED', 'ACTIVE') THEN
        RETURN QUERY SELECT 'SUBSCRIPTION_INACTIVE', 'Subscription not active for activation', NULL::text;
        RETURN;
    END IF;

    IF v_expiry_date IS NOT NULL AND v_expiry_date < NOW() THEN
        RETURN QUERY SELECT 'LICENSE_EXPIRED', 'License expired', NULL::text;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM public.license_activations
        WHERE license_id = v_license_id
          AND server_id = p_machine_id
          AND status = 'ACTIVE'
    ) THEN
        RETURN QUERY SELECT 'ALREADY_ACTIVATED', 'Machine already activated for this license', NULL::text;
        RETURN;
    END IF;

    SELECT allocated_count
    INTO v_allocated_count
    FROM public.subscription_license_allocations
    WHERE subscription_id = v_subscription_id
      AND os_type_id = p_os_type_id;

    IF v_allocated_count IS NULL THEN
        RETURN QUERY SELECT 'OS_NOT_ALLOWED', 'No device allocation configured for this OS', NULL::text;
        RETURN;
    END IF;

    SELECT COUNT(*)
    INTO v_activation_count
    FROM public.license_activations
    WHERE license_id = v_license_id
      AND os_type_id = p_os_type_id
      AND status = 'ACTIVE';

    IF v_activation_count >= v_allocated_count THEN
        RETURN QUERY SELECT 'ACTIVATION_LIMIT_EXCEEDED', 'Allocated device count exceeded for this OS', NULL::text;
        RETURN;
    END IF;

    v_token := encode(gen_random_bytes(32), 'base64');

    INSERT INTO public.license_activations(
        license_id,
        server_id,
        hostname,
        ip_address,
        os_type_id,
        activation_token,
        status,
        activated_at,
        created_by,
        created_at
    )
    VALUES(
        v_license_id,
        p_machine_id,
        p_hostname,
        p_ip_address,
        p_os_type_id,
        v_token,
        'ACTIVE',
        NOW(),
        p_created_by,
        NOW()
    );

    INSERT INTO public.ems_license(
        company_id,
        subscription_id,
        license_id,
        plan_id,
        server_id,
        hostname,
        ip_address,
        license_key,
        start_date,
        expiry_date,
        status,
        activated_at,
        created_at,
        activation_token,
        os_type_id
    )
    SELECT
        s.company_id,
        s.id,
        v_license_id,
        s.plan_id,
        p_machine_id,
        p_hostname,
        p_ip_address,
        p_license_key,
        s.start_date,
        l.expiry_date,
        'ACTIVE',
        NOW(),
        NOW(),
        v_token,
        p_os_type_id
    FROM public.subscriptions s
    JOIN public.licenses l ON l.id = v_license_id
    WHERE s.id = v_subscription_id;

    UPDATE public.subscriptions
    SET status = 'ACTIVE',
        updated_by = p_created_by,
        updated_at = NOW()
    WHERE id = v_subscription_id
      AND status = 'LICENSE_ISSUED';

    RETURN QUERY SELECT 'ACTIVATED', 'License activated successfully', v_token;
END;
$$;

--------------------------------------------------
-- 9. Optional deactivation helper
--------------------------------------------------

CREATE OR REPLACE PROCEDURE public.sp_deactivate_license_activation(
    IN p_license_id bigint,
    IN p_machine_id text,
    IN p_updated_by bigint,
    IN p_updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.license_activations
    SET status = 'DEACTIVATED',
        deactivated_at = p_updated_at,
        updated_by = p_updated_by,
        updated_at = p_updated_at
    WHERE license_id = p_license_id
      AND server_id = p_machine_id
      AND status = 'ACTIVE';
END;
$$;

COMMIT;

--------------------------------------------------
-- Test sequence
--------------------------------------------------
-- 1. Create subscription row with status PENDING
-- 2. Approve it
-- CALL public.sp_approve_subscription(1, 2, NOW());
--
-- 3. Check generation context
-- SELECT * FROM public.sp_get_license_generation_context(1);
--
-- 4. Save generated license from API
-- SELECT public.sp_insert_license(1, 'ENCRYPTED_LICENSE_TEXT', 'periodic', 'ONLINE', 2);
--
-- 5. Mark as issued
-- CALL public.sp_update_subscription_status(1, 'LICENSE_ISSUED', 2, NOW());
--
-- 6. Download existing license
-- SELECT * FROM public.sp_get_downloadable_license(1);
--
-- 7. Activate on EMS
-- SELECT * FROM public.sp_activate_license(
--     'ENCRYPTED_LICENSE_TEXT',
--     'MACHINE-001',
--     'EMS-SERVER',
--     '192.168.1.10',
--     1,
--     2
-- );
