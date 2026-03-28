-- Subscription creation process sync
-- Supports two categories:
-- 1. DEFAULT -> existing subscription plan is selected
-- 2. CUSTOM  -> custom plan snapshot is created automatically and linked to the subscription

BEGIN;

ALTER TABLE public.subscription_plans
    ADD COLUMN IF NOT EXISTS plan_category character varying(20) DEFAULT 'CUSTOM';

UPDATE public.subscription_plans
SET plan_category = CASE
    WHEN UPPER(plan_name) IN ('TRIAL PLAN', 'PRO MONTHLY', 'PRO ANNUAL', 'ENTERPRISE PERPETUAL') THEN 'DEFAULT'
    ELSE COALESCE(NULLIF(plan_category, ''), 'CUSTOM')
END
WHERE plan_category IS NULL
   OR plan_category = '';

CREATE OR REPLACE FUNCTION public.sp_get_subscription_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    company_id bigint,
    company_name character varying,
    plan_id bigint,
    plan_name character varying,
    plan_category character varying,
    status character varying,
    status_description text,
    start_date timestamp without time zone,
    end_date timestamp without time zone,
    requested_at timestamp without time zone,
    approved_at timestamp without time zone,
    license_mode character varying,
    device_limit integer,
    total_allocated integer,
    allocations jsonb
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT s.id,
           s.company_id,
           c.company_name,
           s.plan_id,
           sp.plan_name,
           COALESCE(sp.plan_category, 'CUSTOM'),
           s.status,
           s.status_description,
           s.start_date,
           s.end_date,
           s.created_at AS requested_at,
           CASE
               WHEN UPPER(COALESCE(s.status, '')) IN ('APPROVED', 'ACTIVE', 'LICENSE_ISSUED')
               THEN s.updated_at
               ELSE NULL
           END AS approved_at,
           s.license_mode,
           COALESCE(sp.device_limit, COALESCE(sp.max_devices, 0)),
           COALESCE((SELECT SUM(sla.allocated_count) FROM public.subscription_license_allocations sla WHERE sla.subscription_id = s.id), 0)::integer,
           COALESCE((
               SELECT jsonb_agg(
                   jsonb_build_object(
                       'osTypeId', sla.os_type_id,
                       'osName', os.os_name,
                       'allocatedCount', sla.allocated_count
                   )
                   ORDER BY os.os_name
               )
               FROM public.subscription_license_allocations sla
               JOIN public.device_os_types os ON os.id = sla.os_type_id
               WHERE sla.subscription_id = s.id
           ), '[]'::jsonb)
    FROM public.subscriptions s
    JOIN public.companies c ON c.id = s.company_id
    JOIN public.subscription_plans sp ON sp.id = s.plan_id
    WHERE s.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_subscriptions()
RETURNS TABLE(
    id bigint,
    company_id bigint,
    company_name character varying,
    plan_id bigint,
    plan_name character varying,
    plan_category character varying,
    status character varying,
    status_description text,
    start_date timestamp without time zone,
    end_date timestamp without time zone,
    requested_at timestamp without time zone,
    approved_at timestamp without time zone,
    license_mode character varying,
    device_limit integer,
    total_allocated integer,
    allocations jsonb
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT s.id,
           s.company_id,
           c.company_name,
           s.plan_id,
           sp.plan_name,
           COALESCE(sp.plan_category, 'CUSTOM'),
           s.status,
           s.status_description,
           s.start_date,
           s.end_date,
           s.created_at AS requested_at,
           CASE
               WHEN UPPER(COALESCE(s.status, '')) IN ('APPROVED', 'ACTIVE', 'LICENSE_ISSUED')
               THEN s.updated_at
               ELSE NULL
           END AS approved_at,
           s.license_mode,
           COALESCE(sp.device_limit, COALESCE(sp.max_devices, 0)),
           COALESCE((SELECT SUM(sla.allocated_count) FROM public.subscription_license_allocations sla WHERE sla.subscription_id = s.id), 0)::integer,
           COALESCE((
               SELECT jsonb_agg(
                   jsonb_build_object(
                       'osTypeId', sla.os_type_id,
                       'osName', os.os_name,
                       'allocatedCount', sla.allocated_count
                   )
                   ORDER BY os.os_name
               )
               FROM public.subscription_license_allocations sla
               JOIN public.device_os_types os ON os.id = sla.os_type_id
               WHERE sla.subscription_id = s.id
           ), '[]'::jsonb)
    FROM public.subscriptions s
    JOIN public.companies c ON c.id = s.company_id
    JOIN public.subscription_plans sp ON sp.id = s.plan_id
    ORDER BY s.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_create_subscription(
    p_company_id bigint,
    p_plan_category character varying,
    p_plan_id bigint,
    p_product_id bigint,
    p_start_date timestamp without time zone,
    p_duration_days integer,
    p_license_mode character varying,
    p_allocations jsonb,
    p_created_by bigint
)
RETURNS TABLE(
    id bigint,
    company_id bigint,
    company_name character varying,
    plan_id bigint,
    plan_name character varying,
    plan_category character varying,
    status character varying,
    status_description text,
    start_date timestamp without time zone,
    end_date timestamp without time zone,
    requested_at timestamp without time zone,
    approved_at timestamp without time zone,
    license_mode character varying,
    device_limit integer,
    total_allocated integer,
    allocations jsonb
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_company_name character varying;
    v_category character varying;
    v_plan_id bigint;
    v_plan_name character varying;
    v_plan_device_limit integer;
    v_plan_duration_days integer;
    v_start_date timestamp without time zone;
    v_end_date timestamp without time zone;
    v_total_allocated integer;
    v_subscription_id bigint;
    v_custom_plan_name character varying;
    v_custom_plan_code character varying;
BEGIN
    SELECT c.company_name
    INTO v_company_name
    FROM public.companies c
    WHERE c.id = p_company_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Company not found';
    END IF;

    IF p_allocations IS NULL OR jsonb_typeof(p_allocations) <> 'array' OR jsonb_array_length(p_allocations) = 0 THEN
        RAISE EXCEPTION 'At least one allocation is required';
    END IF;

    SELECT COALESCE(SUM((value ->> 'AllocatedCount')::integer), 0)
    INTO v_total_allocated
    FROM jsonb_array_elements(p_allocations) AS value;

    IF v_total_allocated <= 0 THEN
        RAISE EXCEPTION 'Total allocated devices must be greater than zero';
    END IF;

    v_category := UPPER(COALESCE(NULLIF(p_plan_category, ''), 'DEFAULT'));
    v_start_date := COALESCE(p_start_date, NOW());

    IF v_category = 'DEFAULT' THEN
        IF p_plan_id IS NULL OR p_plan_id <= 0 THEN
            RAISE EXCEPTION 'Default plan is required';
        END IF;

        SELECT sp.id,
               sp.plan_name,
               COALESCE(sp.device_limit, COALESCE(sp.max_devices, 0)),
               COALESCE(sp.duration_days, COALESCE(sp.duration_months, 0) * 30)
        INTO v_plan_id, v_plan_name, v_plan_device_limit, v_plan_duration_days
        FROM public.subscription_plans sp
        WHERE sp.id = p_plan_id
          AND COALESCE(sp.is_active, true) = true;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'Selected plan not found';
        END IF;

        IF v_total_allocated > COALESCE(v_plan_device_limit, 0) AND COALESCE(v_plan_device_limit, 0) > 0 THEN
            RAISE EXCEPTION 'Allocated devices exceed selected plan limit';
        END IF;
    ELSIF v_category = 'CUSTOM' THEN
        IF p_product_id IS NULL OR p_product_id <= 0 THEN
            RAISE EXCEPTION 'Product is required for custom subscription';
        END IF;

        IF p_duration_days IS NULL OR p_duration_days <= 0 THEN
            RAISE EXCEPTION 'Duration days is required for custom subscription';
        END IF;

        v_custom_plan_name := CONCAT('Custom Plan - ', v_company_name, ' - ', to_char(NOW(), 'YYYYMMDDHH24MISS'));
        v_custom_plan_code := CONCAT('CUST-', UPPER(SUBSTRING(REPLACE(gen_random_uuid()::text, '-', '') FROM 1 FOR 8)));

        INSERT INTO public.subscription_plans(
            plan_code,
            plan_name,
            description,
            license_duration_type,
            duration_months,
            max_devices,
            price,
            created_by,
            created_at,
            updated_by,
            updated_at,
            product_id,
            duration_days,
            device_limit,
            is_active,
            billing_label,
            device_limit_label,
            status,
            highlights,
            features,
            plan_category
        )
        VALUES(
            v_custom_plan_code,
            v_custom_plan_name,
            'Auto-generated custom plan snapshot from subscription request',
            'PERIODIC',
            CEIL(p_duration_days / 30.0)::integer,
            v_total_allocated,
            0,
            p_created_by,
            NOW(),
            p_created_by,
            NOW(),
            p_product_id,
            p_duration_days,
            v_total_allocated,
            true,
            CONCAT(p_duration_days, ' Days'),
            CONCAT(v_total_allocated, ' Devices'),
            'ACTIVE',
            '[]'::jsonb,
            '[]'::jsonb,
            'CUSTOM'
        )
        RETURNING id, plan_name, device_limit, duration_days
        INTO v_plan_id, v_plan_name, v_plan_device_limit, v_plan_duration_days;
    ELSE
        RAISE EXCEPTION 'Invalid plan category. Use DEFAULT or CUSTOM';
    END IF;

    v_end_date := CASE
        WHEN COALESCE(v_plan_duration_days, 0) > 0 THEN v_start_date + make_interval(days => v_plan_duration_days)
        ELSE NULL
    END;

    INSERT INTO public.subscriptions(
        company_id,
        plan_id,
        device_count,
        start_date,
        end_date,
        status,
        created_by,
        created_at,
        updated_by,
        updated_at,
        license_mode
    )
    VALUES(
        p_company_id,
        v_plan_id,
        v_total_allocated,
        v_start_date,
        v_end_date,
        'PENDING',
        p_created_by,
        NOW(),
        p_created_by,
        NOW(),
        COALESCE(NULLIF(p_license_mode, ''), 'ONLINE')
    )
    RETURNING subscriptions.id INTO v_subscription_id;

    INSERT INTO public.subscription_license_allocations(
        subscription_id,
        os_type_id,
        allocated_count,
        created_at,
        updated_at
    )
    SELECT v_subscription_id,
           (value ->> 'OsTypeId')::bigint,
           (value ->> 'AllocatedCount')::integer,
           NOW(),
           NOW()
    FROM jsonb_array_elements(p_allocations) AS value
    WHERE COALESCE((value ->> 'AllocatedCount')::integer, 0) > 0;

    RETURN QUERY SELECT * FROM public.sp_get_subscription_by_id(v_subscription_id);
END;
$$;

COMMIT;
