-- Master + Demo data seed script
-- Includes:
-- 1. Master data
-- 2. Default users: SuperAdmin and Admin
-- 3. One sample company
-- 4. One sample subscription
-- 5. Sample OS allocations
--
-- Recommended order:
-- 1. Apply base schema
-- 2. Apply license_db_procedures_functions.sql
-- 3. Apply final_api_db_sync.sql
-- 4. Apply subscription_plan_ui_sync.sql
-- 5. Apply subscription_creation_process_sync.sql
-- 6. Apply this script

BEGIN;

--------------------------------------------------
-- ROLES
--------------------------------------------------

INSERT INTO public.roles (role_name, description, created_at)
SELECT 'SUPERADMIN', 'Full rights across all modules and license operations', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.roles WHERE UPPER(role_name) = 'SUPERADMIN'
);

INSERT INTO public.roles (role_name, description, created_at)
SELECT 'ADMIN', 'Full access to license manager operations', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.roles WHERE UPPER(role_name) = 'ADMIN'
);

INSERT INTO public.roles (role_name, description, created_at)
SELECT 'MANAGER', 'Operational access for approvals and license handling', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.roles WHERE UPPER(role_name) = 'MANAGER'
);

INSERT INTO public.roles (role_name, description, created_at)
SELECT 'VIEWER', 'Read-only access to dashboards and master data', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.roles WHERE UPPER(role_name) = 'VIEWER'
);

--------------------------------------------------
-- DEVICE OS TYPES
--------------------------------------------------

INSERT INTO public.device_os_types (os_name, description, status, created_at)
SELECT 'WINDOWS', 'Microsoft Windows endpoints', 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.device_os_types WHERE UPPER(os_name) = 'WINDOWS'
);

INSERT INTO public.device_os_types (os_name, description, status, created_at)
SELECT 'LINUX', 'Linux endpoints and servers', 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.device_os_types WHERE UPPER(os_name) = 'LINUX'
);

INSERT INTO public.device_os_types (os_name, description, status, created_at)
SELECT 'ANDROID', 'Android devices', 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.device_os_types WHERE UPPER(os_name) = 'ANDROID'
);

INSERT INTO public.device_os_types (os_name, description, status, created_at)
SELECT 'IOS', 'Apple iOS devices', 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.device_os_types WHERE UPPER(os_name) = 'IOS'
);

--------------------------------------------------
-- LICENSE STATUSES
--------------------------------------------------

INSERT INTO public.license_statuses (name, description)
SELECT 'ACTIVE', 'License is active and valid for use'
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_statuses WHERE UPPER(name) = 'ACTIVE'
);

INSERT INTO public.license_statuses (name, description)
SELECT 'REVOKED', 'License has been revoked and cannot be used'
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_statuses WHERE UPPER(name) = 'REVOKED'
);

INSERT INTO public.license_statuses (name, description)
SELECT 'EXPIRED', 'License validity period has ended'
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_statuses WHERE UPPER(name) = 'EXPIRED'
);

INSERT INTO public.license_statuses (name, description)
SELECT 'SUSPENDED', 'License is temporarily disabled'
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_statuses WHERE UPPER(name) = 'SUSPENDED'
);

--------------------------------------------------
-- LICENSE TYPES
--------------------------------------------------

INSERT INTO public.license_types (name, description, created_at)
SELECT 'TRIAL', 'Trial license with limited duration', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_types WHERE UPPER(name) = 'TRIAL'
);

INSERT INTO public.license_types (name, description, created_at)
SELECT 'PERIODIC', 'Time-bound paid subscription license', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_types WHERE UPPER(name) = 'PERIODIC'
);

INSERT INTO public.license_types (name, description, created_at)
SELECT 'PERPETUAL', 'Perpetual license without expiry date', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.license_types WHERE UPPER(name) = 'PERPETUAL'
);

--------------------------------------------------
-- PRODUCTS
--------------------------------------------------

INSERT INTO public.products (product_name, product_code, version, is_active, created_at)
SELECT 'Metronux EMS', 'METRONUX_EMS', '1.0.0', true, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.products WHERE UPPER(product_code) = 'METRONUX_EMS'
);

INSERT INTO public.products (product_name, product_code, version, is_active, created_at)
SELECT 'Metronux Screen Pro', 'SCREEN_PRO', '1.0.0', true, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.products WHERE UPPER(product_code) = 'SCREEN_PRO'
);

--------------------------------------------------
-- PRODUCT FEATURES
--------------------------------------------------

INSERT INTO public.product_features (product_id, feature_name, description, is_active, created_at)
SELECT p.id, 'API_ACCESS', 'Access to API integrations and service endpoints', true, NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.product_features pf
      WHERE pf.product_id = p.id
        AND UPPER(pf.feature_name) = 'API_ACCESS'
  );

INSERT INTO public.product_features (product_id, feature_name, description, is_active, created_at)
SELECT p.id, 'REPORTING', 'Reporting and dashboard capabilities', true, NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.product_features pf
      WHERE pf.product_id = p.id
        AND UPPER(pf.feature_name) = 'REPORTING'
  );

INSERT INTO public.product_features (product_id, feature_name, description, is_active, created_at)
SELECT p.id, 'ADVANCED_ANALYTICS', 'Advanced analytics and trend insights', true, NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.product_features pf
      WHERE pf.product_id = p.id
        AND UPPER(pf.feature_name) = 'ADVANCED_ANALYTICS'
  );

INSERT INTO public.product_features (product_id, feature_name, description, is_active, created_at)
SELECT p.id, 'DEVICE_MONITORING', 'Real-time device monitoring and health visibility', true, NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.product_features pf
      WHERE pf.product_id = p.id
        AND UPPER(pf.feature_name) = 'DEVICE_MONITORING'
  );

INSERT INTO public.product_features (product_id, feature_name, description, is_active, created_at)
SELECT p.id, 'REMOTE_CONTROL', 'Remote control and operational actions', true, NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'SCREEN_PRO'
  AND NOT EXISTS (
      SELECT 1
      FROM public.product_features pf
      WHERE pf.product_id = p.id
        AND UPPER(pf.feature_name) = 'REMOTE_CONTROL'
  );

INSERT INTO public.product_features (product_id, feature_name, description, is_active, created_at)
SELECT p.id, 'SCREEN_CAPTURE', 'Capture and review screen activity', true, NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'SCREEN_PRO'
  AND NOT EXISTS (
      SELECT 1
      FROM public.product_features pf
      WHERE pf.product_id = p.id
        AND UPPER(pf.feature_name) = 'SCREEN_CAPTURE'
  );

--------------------------------------------------
-- SUBSCRIPTION PLANS
--------------------------------------------------

INSERT INTO public.subscription_plans (
    plan_name,
    description,
    license_duration_type,
    duration_months,
    max_devices,
    price,
    product_id,
    duration_days,
    device_limit,
    is_active,
    created_at
)
SELECT
    'Trial Plan',
    'Starter trial plan for evaluation',
    'TRIAL',
    1,
    10,
    0,
    p.id,
    30,
    10,
    true,
    NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.subscription_plans sp
      WHERE UPPER(sp.plan_name) = 'TRIAL PLAN'
        AND sp.product_id = p.id
  );

INSERT INTO public.subscription_plans (
    plan_name,
    description,
    license_duration_type,
    duration_months,
    max_devices,
    price,
    product_id,
    duration_days,
    device_limit,
    is_active,
    created_at
)
SELECT
    'Pro Monthly',
    'Monthly professional plan',
    'PERIODIC',
    1,
    25,
    4999.00,
    p.id,
    30,
    25,
    true,
    NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.subscription_plans sp
      WHERE UPPER(sp.plan_name) = 'PRO MONTHLY'
        AND sp.product_id = p.id
  );

INSERT INTO public.subscription_plans (
    plan_name,
    description,
    license_duration_type,
    duration_months,
    max_devices,
    price,
    product_id,
    duration_days,
    device_limit,
    is_active,
    created_at
)
SELECT
    'Pro Annual',
    'Annual professional plan',
    'PERIODIC',
    12,
    50,
    49999.00,
    p.id,
    365,
    50,
    true,
    NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.subscription_plans sp
      WHERE UPPER(sp.plan_name) = 'PRO ANNUAL'
        AND sp.product_id = p.id
  );

INSERT INTO public.subscription_plans (
    plan_name,
    description,
    license_duration_type,
    duration_months,
    max_devices,
    price,
    product_id,
    duration_days,
    device_limit,
    is_active,
    created_at
)
SELECT
    'Enterprise Perpetual',
    'Enterprise perpetual license plan',
    'PERPETUAL',
    NULL,
    100,
    149999.00,
    p.id,
    0,
    100,
    true,
    NOW()
FROM public.products p
WHERE UPPER(p.product_code) = 'METRONUX_EMS'
  AND NOT EXISTS (
      SELECT 1
      FROM public.subscription_plans sp
      WHERE UPPER(sp.plan_name) = 'ENTERPRISE PERPETUAL'
        AND sp.product_id = p.id
  );

--------------------------------------------------
-- DEFAULT USERS
--------------------------------------------------

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM public.users WHERE LOWER(email) = 'superadmin@metronux.com'
    ) THEN
        UPDATE public.users
        SET name = 'SuperAdmin',
            role = 'SUPERADMIN',
            password_hash = crypt('Admin@123', gen_salt('bf')),
            is_active = true,
            designation = COALESCE(designation, 'System Super Administrator'),
            primary_mobile = COALESCE(primary_mobile, '+91-9000000001'),
            theme = COALESCE(theme, 'light'),
            menu_position = COALESCE(menu_position, 'sidebar'),
            updated_at = NOW()
        WHERE LOWER(email) = 'superadmin@metronux.com';
    ELSE
        INSERT INTO public.users (
            name,
            email,
            password_hash,
            role,
            is_active,
            designation,
            primary_mobile,
            theme,
            menu_position,
            created_at,
            updated_at
        )
        VALUES (
            'SuperAdmin',
            'superadmin@metronux.com',
            crypt('Admin@123', gen_salt('bf')),
            'SUPERADMIN',
            true,
            'System Super Administrator',
            '+91-9000000001',
            'light',
            'sidebar',
            NOW(),
            NOW()
        );
    END IF;
END;
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM public.users WHERE LOWER(email) = 'admin@metronux.com'
    ) THEN
        UPDATE public.users
        SET name = 'Admin',
            role = 'ADMIN',
            password_hash = crypt('Admin@123', gen_salt('bf')),
            is_active = true,
            designation = COALESCE(designation, 'License Administrator'),
            primary_mobile = COALESCE(primary_mobile, '+91-9000000002'),
            theme = COALESCE(theme, 'light'),
            menu_position = COALESCE(menu_position, 'sidebar'),
            updated_at = NOW()
        WHERE LOWER(email) = 'admin@metronux.com';
    ELSE
        INSERT INTO public.users (
            name,
            email,
            password_hash,
            role,
            is_active,
            designation,
            primary_mobile,
            theme,
            menu_position,
            created_at,
            updated_at
        )
        VALUES (
            'Admin',
            'admin@metronux.com',
            crypt('Admin@123', gen_salt('bf')),
            'ADMIN',
            true,
            'License Administrator',
            '+91-9000000002',
            'light',
            'sidebar',
            NOW(),
            NOW()
        );
    END IF;
END;
$$;

--------------------------------------------------
-- SAMPLE COMPANY
--------------------------------------------------

DO $$
DECLARE
    v_admin_user_id bigint;
BEGIN
    SELECT id INTO v_admin_user_id
    FROM public.users
    WHERE LOWER(email) = 'admin@metronux.com'
    LIMIT 1;

    IF NOT EXISTS (
        SELECT 1 FROM public.companies WHERE LOWER(company_name) = 'metronux demo company'
    ) THEN
        INSERT INTO public.companies (
            company_name,
            contact_person,
            email,
            phone,
            status,
            industry,
            primary_mobile,
            alternate_mobile,
            status_description,
            created_by,
            created_at,
            updated_by,
            updated_at
        )
        VALUES (
            'Metronux Demo Company',
            'Demo Contact',
            'it@metronux-demo.com',
            '+91-8000000001',
            'ACTIVE',
            'Technology',
            '+91-8000000001',
            '+91-8000000009',
            'Active demo customer for testing',
            v_admin_user_id,
            NOW(),
            v_admin_user_id,
            NOW()
        );
    END IF;
END;
$$;

--------------------------------------------------
-- SAMPLE SUBSCRIPTION AND OS ALLOCATIONS
--------------------------------------------------

DO $$
DECLARE
    v_admin_user_id bigint;
    v_company_id bigint;
    v_plan_id bigint;
    v_subscription_id bigint;
BEGIN
    SELECT id INTO v_admin_user_id
    FROM public.users
    WHERE LOWER(email) = 'admin@metronux.com'
    LIMIT 1;

    SELECT id INTO v_company_id
    FROM public.companies
    WHERE LOWER(company_name) = 'metronux demo company'
    LIMIT 1;

    SELECT id INTO v_plan_id
    FROM public.subscription_plans
    WHERE UPPER(plan_name) = 'TRIAL PLAN'
    ORDER BY id
    LIMIT 1;

    IF v_company_id IS NULL THEN
        RAISE EXCEPTION 'Sample company not found. Seed company creation failed.';
    END IF;

    IF v_plan_id IS NULL THEN
        RAISE EXCEPTION 'Trial Plan not found. Master data seeding failed.';
    END IF;

    SELECT id INTO v_subscription_id
    FROM public.subscriptions
    WHERE company_id = v_company_id
      AND plan_id = v_plan_id
    ORDER BY id DESC
    LIMIT 1;

    IF v_subscription_id IS NULL THEN
        INSERT INTO public.subscriptions (
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
        VALUES (
            v_company_id,
            v_plan_id,
            15,
            NOW(),
            NOW() + INTERVAL '30 days',
            'APPROVED',
            v_admin_user_id,
            NOW(),
            v_admin_user_id,
            NOW(),
            'ONLINE'
        )
        RETURNING id INTO v_subscription_id;
    ELSE
        UPDATE public.subscriptions
        SET device_count = 15,
            status = 'APPROVED',
            license_mode = 'ONLINE',
            updated_by = v_admin_user_id,
            updated_at = NOW()
        WHERE id = v_subscription_id;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM public.subscription_license_allocations sla
        JOIN public.device_os_types os ON os.id = sla.os_type_id
        WHERE sla.subscription_id = v_subscription_id
          AND UPPER(os.os_name) = 'WINDOWS'
    ) THEN
        INSERT INTO public.subscription_license_allocations (
            subscription_id,
            os_type_id,
            allocated_count,
            created_at
        )
        SELECT v_subscription_id, os.id, 5, NOW()
        FROM public.device_os_types os
        WHERE UPPER(os.os_name) = 'WINDOWS';
    ELSE
        UPDATE public.subscription_license_allocations sla
        SET allocated_count = 5,
            updated_at = NOW()
        FROM public.device_os_types os
        WHERE sla.os_type_id = os.id
          AND sla.subscription_id = v_subscription_id
          AND UPPER(os.os_name) = 'WINDOWS';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM public.subscription_license_allocations sla
        JOIN public.device_os_types os ON os.id = sla.os_type_id
        WHERE sla.subscription_id = v_subscription_id
          AND UPPER(os.os_name) = 'LINUX'
    ) THEN
        INSERT INTO public.subscription_license_allocations (
            subscription_id,
            os_type_id,
            allocated_count,
            created_at
        )
        SELECT v_subscription_id, os.id, 10, NOW()
        FROM public.device_os_types os
        WHERE UPPER(os.os_name) = 'LINUX';
    ELSE
        UPDATE public.subscription_license_allocations sla
        SET allocated_count = 10,
            updated_at = NOW()
        FROM public.device_os_types os
        WHERE sla.os_type_id = os.id
          AND sla.subscription_id = v_subscription_id
          AND UPPER(os.os_name) = 'LINUX';
    END IF;
END;
$$;

--------------------------------------------------
-- DEFAULT PLAN MASTER SYNC
--------------------------------------------------

DO $$
DECLARE
    v_product_id bigint;
BEGIN
    SELECT id INTO v_product_id
    FROM public.products
    WHERE UPPER(product_code) = 'METRONUX_EMS'
    ORDER BY id
    LIMIT 1;

    IF v_product_id IS NULL THEN
        RAISE EXCEPTION 'Product METRONUX_EMS not found. Seed master data first.';
    END IF;

    INSERT INTO public.subscription_plans (
        plan_code,
        plan_name,
        description,
        license_duration_type,
        duration_months,
        max_devices,
        price,
        product_id,
        duration_days,
        device_limit,
        is_active,
        billing_label,
        device_limit_label,
        status,
        highlights,
        features,
        plan_category,
        created_at,
        updated_at
    )
    SELECT
        'PLAN-TRIAL',
        'Trial',
        'Trial plan for evaluation and testing purposes',
        'Periodic',
        1,
        10,
        0,
        v_product_id,
        10,
        10,
        true,
        '10 Days',
        '10 Devices',
        'ACTIVE',
        '["Designed for evaluation and testing purposes","Limited validity of 10 days from activation","Supports up to 10 devices","Ideal for new users to explore system features","No renewal or upgrade restrictions (can be upgraded to paid plans)"]'::jsonb,
        '["Email Support","Basic Analytics"]'::jsonb,
        'DEFAULT',
        NOW(),
        NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM public.subscription_plans WHERE UPPER(plan_code) = 'PLAN-TRIAL' OR UPPER(plan_name) IN ('TRIAL PLAN', 'TRIAL')
    );

    UPDATE public.subscription_plans
    SET plan_code = 'PLAN-TRIAL',
        plan_name = 'Trial',
        description = 'Trial plan for evaluation and testing purposes',
        license_duration_type = 'Periodic',
        duration_days = 10,
        duration_months = 1,
        device_limit = 10,
        max_devices = 10,
        price = 0,
        product_id = v_product_id,
        billing_label = '10 Days',
        device_limit_label = '10 Devices',
        status = 'ACTIVE',
        is_active = true,
        plan_category = 'DEFAULT',
        highlights = '["Designed for evaluation and testing purposes","Limited validity of 10 days from activation","Supports up to 10 devices","Ideal for new users to explore system features","No renewal or upgrade restrictions (can be upgraded to paid plans)"]'::jsonb,
        features = '["Email Support","Basic Analytics"]'::jsonb,
        updated_at = NOW()
    WHERE UPPER(plan_code) = 'PLAN-TRIAL'
       OR UPPER(plan_name) IN ('TRIAL PLAN', 'TRIAL');

    INSERT INTO public.subscription_plans (
        plan_code,
        plan_name,
        description,
        license_duration_type,
        duration_months,
        max_devices,
        price,
        product_id,
        duration_days,
        device_limit,
        is_active,
        billing_label,
        device_limit_label,
        status,
        highlights,
        features,
        plan_category,
        created_at,
        updated_at
    )
    SELECT
        'PLAN-STANDARD',
        'Standard',
        'Annual standard plan for small to medium-sized businesses',
        'Periodic',
        12,
        50,
        4999.00,
        v_product_id,
        365,
        50,
        true,
        '365 Days (1 Year)',
        '50 Devices',
        'ACTIVE',
        '["Suitable for small to medium-sized businesses","Annual subscription with full feature access","Supports up to 50 devices","Includes standard support and updates","Renewable upon expiry"]'::jsonb,
        '["Email Support","Basic Analytics"]'::jsonb,
        'DEFAULT',
        NOW(),
        NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM public.subscription_plans WHERE UPPER(plan_code) = 'PLAN-STANDARD' OR UPPER(plan_name) IN ('PRO MONTHLY', 'STANDARD')
    );

    UPDATE public.subscription_plans
    SET plan_code = 'PLAN-STANDARD',
        plan_name = 'Standard',
        description = 'Annual standard plan for small to medium-sized businesses',
        license_duration_type = 'Periodic',
        duration_days = 365,
        duration_months = 12,
        device_limit = 50,
        max_devices = 50,
        price = 4999.00,
        product_id = v_product_id,
        billing_label = '365 Days (1 Year)',
        device_limit_label = '50 Devices',
        status = 'ACTIVE',
        is_active = true,
        plan_category = 'DEFAULT',
        highlights = '["Suitable for small to medium-sized businesses","Annual subscription with full feature access","Supports up to 50 devices","Includes standard support and updates","Renewable upon expiry"]'::jsonb,
        features = '["Email Support","Basic Analytics"]'::jsonb,
        updated_at = NOW()
    WHERE UPPER(plan_code) = 'PLAN-STANDARD'
       OR UPPER(plan_name) IN ('PRO MONTHLY', 'STANDARD');

    INSERT INTO public.subscription_plans (
        plan_code,
        plan_name,
        description,
        license_duration_type,
        duration_months,
        max_devices,
        price,
        product_id,
        duration_days,
        device_limit,
        is_active,
        billing_label,
        device_limit_label,
        status,
        highlights,
        features,
        plan_category,
        created_at,
        updated_at
    )
    SELECT
        'PLAN-PROFESSIONAL',
        'Professional',
        'Annual professional plan for growing businesses',
        'Periodic',
        12,
        100,
        49999.00,
        v_product_id,
        365,
        100,
        true,
        '365 Days (1 Year)',
        '100 Devices',
        'ACTIVE',
        '["Suitable for small to medium-sized businesses","Annual subscription with full feature access","Supports up to 100 devices","Includes standard support and updates","Renewable upon expiry"]'::jsonb,
        '["24/7 Support","Basic Analytics"]'::jsonb,
        'DEFAULT',
        NOW(),
        NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM public.subscription_plans WHERE UPPER(plan_code) = 'PLAN-PROFESSIONAL' OR UPPER(plan_name) IN ('PRO ANNUAL', 'PROFESSIONAL')
    );

    UPDATE public.subscription_plans
    SET plan_code = 'PLAN-PROFESSIONAL',
        plan_name = 'Professional',
        description = 'Annual professional plan for growing businesses',
        license_duration_type = 'Periodic',
        duration_days = 365,
        duration_months = 12,
        device_limit = 100,
        max_devices = 100,
        price = 49999.00,
        product_id = v_product_id,
        billing_label = '365 Days (1 Year)',
        device_limit_label = '100 Devices',
        status = 'ACTIVE',
        is_active = true,
        plan_category = 'DEFAULT',
        highlights = '["Suitable for small to medium-sized businesses","Annual subscription with full feature access","Supports up to 100 devices","Includes standard support and updates","Renewable upon expiry"]'::jsonb,
        features = '["24/7 Support","Basic Analytics"]'::jsonb,
        updated_at = NOW()
    WHERE UPPER(plan_code) = 'PLAN-PROFESSIONAL'
       OR UPPER(plan_name) IN ('PRO ANNUAL', 'PROFESSIONAL');

    INSERT INTO public.subscription_plans (
        plan_code,
        plan_name,
        description,
        license_duration_type,
        duration_months,
        max_devices,
        price,
        product_id,
        duration_days,
        device_limit,
        is_active,
        billing_label,
        device_limit_label,
        status,
        highlights,
        features,
        plan_category,
        created_at,
        updated_at
    )
    SELECT
        'PLAN-ENTERPRISE',
        'Enterprise',
        'Perpetual enterprise plan for large-scale deployments',
        'Perpetual',
        NULL,
        0,
        149999.00,
        v_product_id,
        0,
        0,
        true,
        'Perpetual',
        'Unlimited Devices',
        'ACTIVE',
        '["Designed for large-scale enterprise deployments","Lifetime validity with no expiration","Supports unlimited devices","Includes advanced features and priority support","One-time purchase (no renewal required)"]'::jsonb,
        '["Custom Branding","24/7 Support","Advanced Analytics"]'::jsonb,
        'DEFAULT',
        NOW(),
        NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM public.subscription_plans WHERE UPPER(plan_code) = 'PLAN-ENTERPRISE' OR UPPER(plan_name) IN ('ENTERPRISE PERPETUAL', 'ENTERPRISE')
    );

    UPDATE public.subscription_plans
    SET plan_code = 'PLAN-ENTERPRISE',
        plan_name = 'Enterprise',
        description = 'Perpetual enterprise plan for large-scale deployments',
        license_duration_type = 'Perpetual',
        duration_days = 0,
        duration_months = NULL,
        device_limit = 0,
        max_devices = 0,
        price = 149999.00,
        product_id = v_product_id,
        billing_label = 'Perpetual',
        device_limit_label = 'Unlimited Devices',
        status = 'ACTIVE',
        is_active = true,
        plan_category = 'DEFAULT',
        highlights = '["Designed for large-scale enterprise deployments","Lifetime validity with no expiration","Supports unlimited devices","Includes advanced features and priority support","One-time purchase (no renewal required)"]'::jsonb,
        features = '["Custom Branding","24/7 Support","Advanced Analytics"]'::jsonb,
        updated_at = NOW()
    WHERE UPPER(plan_code) = 'PLAN-ENTERPRISE'
       OR UPPER(plan_name) IN ('ENTERPRISE PERPETUAL', 'ENTERPRISE');
END;
$$;

COMMIT;
