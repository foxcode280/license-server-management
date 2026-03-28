-- Final DB sync script for current API modules.
-- Reviewed against:
-- 1. Database Scripts\License_db\License_db_table_schema.sql
-- 2. Database Scripts\License_db\license_db_procedures_functions.sql
-- 3. Current ASP.NET Core repositories/controllers/services in LicenseManager.API
--
-- This script focuses on objects that are required by the current API code
-- but are missing from the base database scripts, or whose shape needs to be
-- extended for the FE-aligned APIs.
--
-- License workflow procedures such as:
--   sp_get_license_generation_context
--   sp_insert_license
--   sp_get_downloadable_license
--   sp_update_subscription_status
-- already exist in the reviewed DB scripts and are therefore not recreated here.

BEGIN;

--------------------------------------------------
-- USERS
--------------------------------------------------

ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS designation character varying(150),
    ADD COLUMN IF NOT EXISTS primary_mobile character varying(50),
    ADD COLUMN IF NOT EXISTS alternate_mobile character varying(50),
    ADD COLUMN IF NOT EXISTS last_login timestamp without time zone,
    ADD COLUMN IF NOT EXISTS theme character varying(20) DEFAULT 'light',
    ADD COLUMN IF NOT EXISTS menu_position character varying(20) DEFAULT 'sidebar',
    ADD COLUMN IF NOT EXISTS profile_photo text;

CREATE OR REPLACE FUNCTION public.sp_get_users()
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    role character varying,
    designation character varying,
    mobile character varying,
    alternate_mobile character varying,
    last_login timestamp without time zone,
    is_disabled boolean,
    theme character varying,
    menu_position character varying,
    profile_photo text,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id,
           u.name,
           u.email,
           u.role,
           u.designation,
           u.primary_mobile AS mobile,
           u.alternate_mobile,
           u.last_login,
           NOT COALESCE(u.is_active, true) AS is_disabled,
           COALESCE(u.theme, 'light'),
           COALESCE(u.menu_position, 'sidebar'),
           u.profile_photo,
           u.created_at,
           u.updated_at
    FROM public.users u
    ORDER BY u.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_user_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    role character varying,
    designation character varying,
    mobile character varying,
    alternate_mobile character varying,
    last_login timestamp without time zone,
    is_disabled boolean,
    theme character varying,
    menu_position character varying,
    profile_photo text,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id,
           u.name,
           u.email,
           u.role,
           u.designation,
           u.primary_mobile AS mobile,
           u.alternate_mobile,
           u.last_login,
           NOT COALESCE(u.is_active, true) AS is_disabled,
           COALESCE(u.theme, 'light'),
           COALESCE(u.menu_position, 'sidebar'),
           u.profile_photo,
           u.created_at,
           u.updated_at
    FROM public.users u
    WHERE u.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_insert_user(
    p_name character varying,
    p_email character varying,
    p_role character varying,
    p_designation character varying,
    p_mobile character varying,
    p_alternate_mobile character varying,
    p_is_disabled boolean,
    p_password_hash text,
    p_created_by bigint
)
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    role character varying,
    designation character varying,
    mobile character varying,
    alternate_mobile character varying,
    last_login timestamp without time zone,
    is_disabled boolean,
    theme character varying,
    menu_position character varying,
    profile_photo text,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id bigint;
BEGIN
    INSERT INTO public.users(
        name,
        email,
        password_hash,
        role,
        is_active,
        designation,
        primary_mobile,
        alternate_mobile,
        created_by,
        created_at,
        updated_by,
        updated_at,
        theme,
        menu_position
    )
    VALUES(
        p_name,
        p_email,
        p_password_hash,
        p_role,
        NOT p_is_disabled,
        p_designation,
        p_mobile,
        p_alternate_mobile,
        p_created_by,
        NOW(),
        p_created_by,
        NOW(),
        'light',
        'sidebar'
    )
    RETURNING users.id INTO v_id;

    RETURN QUERY SELECT * FROM public.sp_get_user_by_id(v_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_user(
    p_id bigint,
    p_name character varying,
    p_email character varying,
    p_role character varying,
    p_designation character varying,
    p_mobile character varying,
    p_alternate_mobile character varying,
    p_is_disabled boolean,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    role character varying,
    designation character varying,
    mobile character varying,
    alternate_mobile character varying,
    last_login timestamp without time zone,
    is_disabled boolean,
    theme character varying,
    menu_position character varying,
    profile_photo text,
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.users
    SET name = p_name,
        email = p_email,
        role = p_role,
        designation = p_designation,
        primary_mobile = p_mobile,
        alternate_mobile = p_alternate_mobile,
        is_active = NOT p_is_disabled,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE users.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_user_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_user(
    p_id bigint,
    p_updated_by bigint
)
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

--------------------------------------------------
-- COMPANIES
--------------------------------------------------

ALTER TABLE public.companies
    ADD COLUMN IF NOT EXISTS industry character varying(150),
    ADD COLUMN IF NOT EXISTS primary_mobile character varying(50),
    ADD COLUMN IF NOT EXISTS alternate_mobile character varying(50),
    ADD COLUMN IF NOT EXISTS status_description text;

UPDATE public.companies
SET primary_mobile = COALESCE(primary_mobile, phone)
WHERE primary_mobile IS NULL;

ALTER TABLE public.licenses
    ADD COLUMN IF NOT EXISTS license_code character varying(100);

CREATE OR REPLACE FUNCTION public.sp_get_companies()
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    industry character varying,
    contact_person character varying,
    primary_mobile character varying,
    alternate_mobile character varying,
    status character varying,
    status_description text,
    linked_subscriptions text[],
    linked_licenses text[],
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id,
           c.company_name AS name,
           c.email,
           c.industry,
           c.contact_person,
           COALESCE(c.primary_mobile, c.phone) AS primary_mobile,
           c.alternate_mobile,
           c.status,
           c.status_description,
           COALESCE(ARRAY(
               SELECT s.id::text
               FROM public.subscriptions s
               WHERE s.company_id = c.id
               ORDER BY s.id
           ), ARRAY[]::text[]),
           COALESCE(ARRAY(
               SELECT COALESCE(NULLIF(l.license_code, ''), l.id::text)
               FROM public.licenses l
               JOIN public.subscriptions s2 ON s2.id = l.subscription_id
               WHERE s2.company_id = c.id
               ORDER BY l.id
           ), ARRAY[]::text[]),
           c.created_at,
           c.updated_at
    FROM public.companies c
    ORDER BY c.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_company_by_id(p_id bigint)
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    industry character varying,
    contact_person character varying,
    primary_mobile character varying,
    alternate_mobile character varying,
    status character varying,
    status_description text,
    linked_subscriptions text[],
    linked_licenses text[],
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT *
    FROM public.sp_get_companies()
    WHERE sp_get_companies.id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_insert_company(
    p_name character varying,
    p_industry character varying,
    p_contact_person character varying,
    p_primary_mobile character varying,
    p_alternate_mobile character varying,
    p_email character varying,
    p_status character varying,
    p_status_description text,
    p_created_by bigint
)
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    industry character varying,
    contact_person character varying,
    primary_mobile character varying,
    alternate_mobile character varying,
    status character varying,
    status_description text,
    linked_subscriptions text[],
    linked_licenses text[],
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id bigint;
BEGIN
    INSERT INTO public.companies(
        company_name,
        contact_person,
        email,
        phone,
        status,
        created_by,
        created_at,
        updated_by,
        updated_at,
        industry,
        primary_mobile,
        alternate_mobile,
        status_description
    )
    VALUES(
        p_name,
        p_contact_person,
        p_email,
        p_primary_mobile,
        p_status,
        p_created_by,
        NOW(),
        p_created_by,
        NOW(),
        p_industry,
        p_primary_mobile,
        p_alternate_mobile,
        p_status_description
    )
    RETURNING companies.id INTO v_id;

    RETURN QUERY SELECT * FROM public.sp_get_company_by_id(v_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_update_company(
    p_id bigint,
    p_name character varying,
    p_industry character varying,
    p_contact_person character varying,
    p_primary_mobile character varying,
    p_alternate_mobile character varying,
    p_email character varying,
    p_status character varying,
    p_status_description text,
    p_updated_by bigint
)
RETURNS TABLE(
    id bigint,
    name character varying,
    email character varying,
    industry character varying,
    contact_person character varying,
    primary_mobile character varying,
    alternate_mobile character varying,
    status character varying,
    status_description text,
    linked_subscriptions text[],
    linked_licenses text[],
    created_at timestamp without time zone,
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.companies
    SET company_name = p_name,
        industry = p_industry,
        contact_person = p_contact_person,
        primary_mobile = p_primary_mobile,
        phone = p_primary_mobile,
        alternate_mobile = p_alternate_mobile,
        email = p_email,
        status = p_status,
        status_description = p_status_description,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE companies.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_company_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_ban_company(
    p_id bigint,
    p_reason text,
    p_updated_by bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.companies
    SET status = 'Suspended',
        status_description = COALESCE(p_reason, 'Banned from the system'),
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;

    UPDATE public.subscriptions
    SET status = 'Rejected',
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE company_id = p_id
      AND status IN ('Pending', 'PENDING', 'APPROVED', 'Active', 'ACTIVE');

    UPDATE public.licenses l
    SET status = 'REVOKED',
        updated_by = p_updated_by,
        updated_at = NOW()
    FROM public.subscriptions s
    WHERE s.id = l.subscription_id
      AND s.company_id = p_id
      AND l.status IN ('ACTIVE', 'ISSUED');
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_company_subscription_details(p_company_id bigint)
RETURNS TABLE(
    id bigint,
    plan_name character varying,
    status character varying,
    start_date timestamp without time zone,
    end_date timestamp without time zone,
    requested_at timestamp without time zone,
    approved_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT s.id,
           sp.plan_name,
           s.status,
           s.start_date,
           s.end_date,
           s.created_at AS requested_at,
           CASE
               WHEN s.status IN ('ACTIVE', 'APPROVED', 'LICENSE_ISSUED')
               THEN s.updated_at
               ELSE NULL
           END AS approved_at
    FROM public.subscriptions s
    LEFT JOIN public.subscription_plans sp ON sp.id = s.plan_id
    WHERE s.company_id = p_company_id
    ORDER BY s.id DESC;
END;
$$;

CREATE OR REPLACE FUNCTION public.sp_get_company_license_details(p_company_id bigint)
RETURNS TABLE(
    id bigint,
    license_code character varying,
    subscription_id bigint,
    status character varying,
    expiry_date timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT l.id,
           COALESCE(NULLIF(l.license_code, ''), l.id::character varying),
           l.subscription_id,
           l.status,
           l.expiry_date
    FROM public.licenses l
    JOIN public.subscriptions s ON s.id = l.subscription_id
    WHERE s.company_id = p_company_id
    ORDER BY l.id DESC;
END;
$$;

--------------------------------------------------
-- PRODUCTS
--------------------------------------------------

ALTER TABLE public.products
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS updated_by bigint,
    ADD COLUMN IF NOT EXISTS updated_at timestamp without time zone;

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
    SELECT p.id,
           p.product_name,
           p.product_code,
           COALESCE(p.is_active, true)
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
    SELECT p.id,
           p.product_name,
           p.product_code,
           COALESCE(p.is_active, true)
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
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE products.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_product_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_product(
    p_id bigint,
    p_updated_by bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.products
    SET is_active = false,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

--------------------------------------------------
-- PRODUCT FEATURES
--------------------------------------------------

ALTER TABLE public.product_features
    ADD COLUMN IF NOT EXISTS description text,
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS updated_by bigint,
    ADD COLUMN IF NOT EXISTS updated_at timestamp without time zone;

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
    SELECT pf.id,
           pf.product_id,
           pf.feature_name,
           COALESCE(pf.description, ''),
           COALESCE(pf.is_active, true)
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
    SELECT pf.id,
           pf.product_id,
           pf.feature_name,
           COALESCE(pf.description, ''),
           COALESCE(pf.is_active, true)
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
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE product_features.id = p_id;

    RETURN QUERY SELECT * FROM public.sp_get_product_feature_by_id(p_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_product_feature(
    p_id bigint,
    p_updated_by bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.product_features
    SET is_active = false,
        updated_by = p_updated_by,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

--------------------------------------------------
-- SUBSCRIPTION PLANS
--------------------------------------------------

ALTER TABLE public.subscription_plans
    ADD COLUMN IF NOT EXISTS duration_days integer,
    ADD COLUMN IF NOT EXISTS device_limit integer,
    ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true;

UPDATE public.subscription_plans
SET duration_days = COALESCE(duration_days, COALESCE(duration_months, 0) * 30),
    device_limit = COALESCE(device_limit, COALESCE(max_devices, 0))
WHERE duration_days IS NULL
   OR device_limit IS NULL;

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

CREATE OR REPLACE PROCEDURE public.sp_deactivate_subscription_plan(
    p_id bigint,
    p_updated_by bigint
)
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
