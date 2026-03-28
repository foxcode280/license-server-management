-- Company add/edit/ban/details procedures aligned to licensemanagerFE-main
BEGIN;

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
           CASE WHEN s.status IN ('ACTIVE', 'APPROVED', 'LICENSE_ISSUED') THEN s.updated_at ELSE NULL END AS approved_at
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

COMMIT;
