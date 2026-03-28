-- User add/edit procedures aligned to licensemanagerFE-main
BEGIN;

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

COMMIT;
