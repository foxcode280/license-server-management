BEGIN;

ALTER TABLE public.products
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS updated_at timestamp without time zone;

ALTER TABLE public.product_features
    ADD COLUMN IF NOT EXISTS description text,
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS updated_at timestamp without time zone;

ALTER TABLE public.subscription_plans
    ADD COLUMN IF NOT EXISTS duration_days integer,
    ADD COLUMN IF NOT EXISTS device_limit integer,
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true;

UPDATE public.subscription_plans
SET duration_days = COALESCE(duration_days, COALESCE(duration_months, 0) * 30),
    device_limit = COALESCE(device_limit, COALESCE(max_devices, 0))
WHERE duration_days IS NULL
   OR device_limit IS NULL;

CREATE OR REPLACE FUNCTION public.sp_get_users()
RETURNS TABLE(
    id bigint,
    username character varying,
    email character varying,
    role character varying,
    is_active boolean,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.name AS username, u.email, u.role, u.is_active, u.created_at, u.updated_at
    FROM public.users u
    ORDER BY u.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_user_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    username character varying,
    email character varying,
    role character varying,
    is_active boolean,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.name AS username, u.email, u.role, u.is_active, u.created_at, u.updated_at
    FROM public.users u
    WHERE u.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_user(
    p_id bigint,
    p_username character varying,
    p_email character varying,
    p_role character varying,
    p_is_active boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    username character varying,
    email character varying,
    role character varying,
    is_active boolean,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.users
    SET name = p_username,
        email = p_email,
        role = p_role,
        is_active = p_is_active,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE users.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_user_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_user(p_id bigint, p_updated_by bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.users
    SET is_active = false,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_companies()
RETURNS TABLE(
    id bigint,
    company_name character varying,
    email character varying,
    contact_number character varying,
    is_active boolean,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id,
           c.company_name,
           c.email,
           c.phone AS contact_number,
           CASE WHEN c.status = 'ACTIVE' THEN true ELSE false END AS is_active,
           c.created_at,
           c.updated_at
    FROM public.companies c
    ORDER BY c.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_company_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    company_name character varying,
    email character varying,
    contact_number character varying,
    is_active boolean,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id,
           c.company_name,
           c.email,
           c.phone AS contact_number,
           CASE WHEN c.status = 'ACTIVE' THEN true ELSE false END AS is_active,
           c.created_at,
           c.updated_at
    FROM public.companies c
    WHERE c.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_company(
    p_id bigint,
    p_company_name character varying,
    p_email character varying,
    p_contact_number character varying,
    p_is_active boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    company_name character varying,
    email character varying,
    contact_number character varying,
    is_active boolean,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.companies
    SET company_name = p_company_name,
        email = p_email,
        phone = p_contact_number,
        status = CASE WHEN p_is_active THEN 'ACTIVE' ELSE 'INACTIVE' END,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE companies.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_company_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_company(p_id bigint, p_updated_by bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.companies
    SET status = 'INACTIVE',
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_products()
RETURNS TABLE(
    id bigint,
    product_name character varying,
    product_code character varying,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.product_name, p.product_code, COALESCE(p.is_active, true)
    FROM public.products p
    ORDER BY p.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_product_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    product_name character varying,
    product_code character varying,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.product_name, p.product_code, COALESCE(p.is_active, true)
    FROM public.products p
    WHERE p.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_product(
    p_id bigint,
    p_product_name character varying,
    p_product_code character varying,
    p_is_active boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    product_name character varying,
    product_code character varying,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.products
    SET product_name = p_product_name,
        product_code = p_product_code,
        is_active = p_is_active,
        updated_at = NOW()
    WHERE products.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_product_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_product(p_id bigint, p_updated_by bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.products
    SET is_active = false,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_product_features()
RETURNS TABLE(
    id bigint,
    product_id bigint,
    feature_name character varying,
    description text,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT pf.id, pf.product_id, pf.feature_name, COALESCE(pf.description, ''), COALESCE(pf.is_active, true)
    FROM public.product_features pf
    ORDER BY pf.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_product_feature_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    product_id bigint,
    feature_name character varying,
    description text,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT pf.id, pf.product_id, pf.feature_name, COALESCE(pf.description, ''), COALESCE(pf.is_active, true)
    FROM public.product_features pf
    WHERE pf.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_product_feature(
    p_id bigint,
    p_product_id bigint,
    p_feature_name character varying,
    p_description text,
    p_is_active boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    product_id bigint,
    feature_name character varying,
    description text,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.product_features
    SET product_id = p_product_id,
        feature_name = p_feature_name,
        description = p_description,
        is_active = p_is_active,
        updated_at = NOW()
    WHERE product_features.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_product_feature_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_product_feature(p_id bigint, p_updated_by bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.product_features
    SET is_active = false,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_subscription_plans()
RETURNS TABLE(
    id bigint,
    plan_name character varying,
    product_id bigint,
    duration_days integer,
    device_limit integer,
    price numeric,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT sp.id,
           sp.plan_name,
           sp.product_id,
           COALESCE(sp.duration_days, 0),
           COALESCE(sp.device_limit, 0),
           COALESCE(sp.price, 0),
           COALESCE(sp.is_active, true)
    FROM public.subscription_plans sp
    ORDER BY sp.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_subscription_plan_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    plan_name character varying,
    product_id bigint,
    duration_days integer,
    device_limit integer,
    price numeric,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT sp.id,
           sp.plan_name,
           sp.product_id,
           COALESCE(sp.duration_days, 0),
           COALESCE(sp.device_limit, 0),
           COALESCE(sp.price, 0),
           COALESCE(sp.is_active, true)
    FROM public.subscription_plans sp
    WHERE sp.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_subscription_plan(
    p_id bigint,
    p_plan_name character varying,
    p_product_id bigint,
    p_duration_days integer,
    p_device_limit integer,
    p_price numeric,
    p_is_active boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    plan_name character varying,
    product_id bigint,
    duration_days integer,
    device_limit integer,
    price numeric,
    is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.subscription_plans
    SET plan_name = p_plan_name,
        product_id = p_product_id,
        duration_days = p_duration_days,
        device_limit = p_device_limit,
        price = p_price,
        is_active = p_is_active,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE subscription_plans.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_subscription_plan_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_subscription_plan(p_id bigint, p_updated_by bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.subscription_plans
    SET is_active = false,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

COMMIT;
