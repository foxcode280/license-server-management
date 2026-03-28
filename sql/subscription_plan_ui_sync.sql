-- Subscription plan API sync for create/edit UI fields
BEGIN;

ALTER TABLE public.subscription_plans
    ADD COLUMN IF NOT EXISTS plan_code character varying(50),
    ADD COLUMN IF NOT EXISTS duration_days integer,
    ADD COLUMN IF NOT EXISTS device_limit integer,
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS billing_label character varying(150),
    ADD COLUMN IF NOT EXISTS device_limit_label character varying(150),
    ADD COLUMN IF NOT EXISTS status character varying(50) DEFAULT 'ACTIVE',
    ADD COLUMN IF NOT EXISTS highlights jsonb DEFAULT '[]'::jsonb,
    ADD COLUMN IF NOT EXISTS features jsonb DEFAULT '[]'::jsonb;

UPDATE public.subscription_plans
SET duration_days = COALESCE(duration_days, COALESCE(duration_months, 0) * 30),
    device_limit = COALESCE(device_limit, COALESCE(max_devices, 0)),
    is_active = COALESCE(is_active, true),
    status = COALESCE(NULLIF(status, ''), CASE WHEN COALESCE(is_active, true) THEN 'ACTIVE' ELSE 'INACTIVE' END),
    billing_label = COALESCE(NULLIF(billing_label, ''), CASE WHEN COALESCE(duration_days, COALESCE(duration_months, 0) * 30) > 0 THEN CONCAT(COALESCE(duration_days, COALESCE(duration_months, 0) * 30), ' Days') ELSE '' END),
    device_limit_label = COALESCE(NULLIF(device_limit_label, ''), CASE WHEN COALESCE(device_limit, COALESCE(max_devices, 0)) > 0 THEN CONCAT(COALESCE(device_limit, COALESCE(max_devices, 0)), ' Devices') ELSE 'Unlimited Devices' END),
    highlights = COALESCE(highlights, '[]'::jsonb),
    features = COALESCE(features, '[]'::jsonb),
    plan_code = COALESCE(NULLIF(plan_code, ''), CONCAT('PLAN-', LPAD(id::text, 6, '0')))
WHERE duration_days IS NULL
   OR device_limit IS NULL
   OR is_active IS NULL
   OR status IS NULL
   OR billing_label IS NULL
   OR device_limit_label IS NULL
   OR highlights IS NULL
   OR features IS NULL
   OR plan_code IS NULL
   OR plan_code = '';

CREATE UNIQUE INDEX IF NOT EXISTS ux_subscription_plans_plan_code
    ON public.subscription_plans(plan_code);

CREATE OR REPLACE FUNCTION public.sp_get_subscription_plans()
RETURNS TABLE(
    id bigint,
    plan_code character varying,
    plan_name character varying,
    product_id bigint,
    product_name character varying,
    mode character varying,
    status character varying,
    price numeric,
    duration_days integer,
    billing_label character varying,
    device_limit integer,
    device_limit_label character varying,
    description text,
    highlights jsonb,
    features jsonb,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT sp.id,
           COALESCE(sp.plan_code, CONCAT('PLAN-', LPAD(sp.id::text, 6, '0'))),
           sp.plan_name,
           sp.product_id,
           COALESCE(p.product_name, ''),
           COALESCE(sp.license_duration_type, ''),
           COALESCE(sp.status, CASE WHEN COALESCE(sp.is_active, true) THEN 'ACTIVE' ELSE 'INACTIVE' END),
           COALESCE(sp.price, 0),
           COALESCE(sp.duration_days, 0),
           COALESCE(sp.billing_label, ''),
           COALESCE(sp.device_limit, 0),
           COALESCE(sp.device_limit_label, ''),
           COALESCE(sp.description, ''),
           COALESCE(sp.highlights, '[]'::jsonb),
           COALESCE(sp.features, '[]'::jsonb),
           COALESCE(sp.is_active, true)
    FROM public.subscription_plans sp
    LEFT JOIN public.products p ON p.id = sp.product_id
    ORDER BY sp.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_subscription_plan_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    plan_code character varying,
    plan_name character varying,
    product_id bigint,
    product_name character varying,
    mode character varying,
    status character varying,
    price numeric,
    duration_days integer,
    billing_label character varying,
    device_limit integer,
    device_limit_label character varying,
    description text,
    highlights jsonb,
    features jsonb,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT *
    FROM public.sp_get_subscription_plans()
    WHERE sp_get_subscription_plans.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_insert_subscription_plan(
    p_plan_code character varying,
    p_plan_name character varying,
    p_product_id bigint,
    p_mode character varying,
    p_status character varying,
    p_price numeric,
    p_duration_days integer,
    p_billing_label character varying,
    p_device_limit integer,
    p_device_limit_label character varying,
    p_description text,
    p_highlights jsonb,
    p_features jsonb,
    p_is_active boolean,
    p_created_by bigint
)
RETURNS TABLE(
    id bigint,
    plan_code character varying,
    plan_name character varying,
    product_id bigint,
    product_name character varying,
    mode character varying,
    status character varying,
    price numeric,
    duration_days integer,
    billing_label character varying,
    device_limit integer,
    device_limit_label character varying,
    description text,
    highlights jsonb,
    features jsonb,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id bigint;
    v_plan_code character varying;
BEGIN
    v_plan_code := COALESCE(NULLIF(p_plan_code, ''), CONCAT('PLAN-', UPPER(SUBSTRING(REPLACE(gen_random_uuid()::text, '-', '') FROM 1 FOR 8))));

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
        features
    )
    VALUES(
        v_plan_code,
        p_plan_name,
        p_description,
        p_mode,
        CASE WHEN p_duration_days > 0 THEN CEIL(p_duration_days / 30.0)::integer ELSE NULL END,
        p_device_limit,
        p_price,
        p_created_by,
        NOW(),
        p_created_by,
        NOW(),
        p_product_id,
        p_duration_days,
        p_device_limit,
        p_is_active,
        p_billing_label,
        p_device_limit_label,
        COALESCE(NULLIF(p_status, ''), CASE WHEN p_is_active THEN 'ACTIVE' ELSE 'INACTIVE' END),
        COALESCE(p_highlights, '[]'::jsonb),
        COALESCE(p_features, '[]'::jsonb)
    )
    RETURNING subscription_plans.id INTO v_id;

    RETURN QUERY SELECT * FROM public.sp_get_subscription_plan_by_id(v_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_subscription_plan(
    p_id bigint,
    p_plan_code character varying,
    p_plan_name character varying,
    p_product_id bigint,
    p_mode character varying,
    p_status character varying,
    p_price numeric,
    p_duration_days integer,
    p_billing_label character varying,
    p_device_limit integer,
    p_device_limit_label character varying,
    p_description text,
    p_highlights jsonb,
    p_features jsonb,
    p_is_active boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    plan_code character varying,
    plan_name character varying,
    product_id bigint,
    product_name character varying,
    mode character varying,
    status character varying,
    price numeric,
    duration_days integer,
    billing_label character varying,
    device_limit integer,
    device_limit_label character varying,
    description text,
    highlights jsonb,
    features jsonb,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.subscription_plans
    SET plan_code = COALESCE(NULLIF(p_plan_code, ''), plan_code, CONCAT('PLAN-', LPAD(id::text, 6, '0'))),
        plan_name = p_plan_name,
        product_id = p_product_id,
        license_duration_type = p_mode,
        status = COALESCE(NULLIF(p_status, ''), CASE WHEN p_is_active THEN 'ACTIVE' ELSE 'INACTIVE' END),
        price = p_price,
        duration_days = p_duration_days,
        duration_months = CASE WHEN p_duration_days > 0 THEN CEIL(p_duration_days / 30.0)::integer ELSE NULL END,
        billing_label = p_billing_label,
        device_limit = p_device_limit,
        max_devices = p_device_limit,
        device_limit_label = p_device_limit_label,
        description = p_description,
        highlights = COALESCE(p_highlights, '[]'::jsonb),
        features = COALESCE(p_features, '[]'::jsonb),
        is_active = p_is_active,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE subscription_plans.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_subscription_plan_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_subscription_plan(
    p_id bigint,
    p_updated_by bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.subscription_plans
    SET is_active = false,
        status = 'INACTIVE',
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

COMMIT;
