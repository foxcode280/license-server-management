# Metronux License Manager Process

## Purpose

This document defines the recommended license manager process for the Metronux license platform. It is based on the current system design and aligned to common industry-standard license server patterns:

- commercial subscription lifecycle is managed separately from technical license activation
- one subscription produces one immutable license
- activation binds a license to one EMS server
- device consumption is tracked separately from license issuance
- all critical operations are validated by the license server

## Core Principles

### 1. Subscription and license are different objects

- `Subscription` represents the commercial agreement
- `License` represents the signed and encrypted technical artifact generated from that subscription

### 2. License is immutable after generation

- once a license is generated, it should not be edited
- upgrades, renewals, plan changes, and commercial changes must create a new subscription and new license

### 3. Activation is separate from license generation

- generating a license does not mean it is active in EMS
- a license becomes operational only after EMS activation

### 4. Capacity is enforced by the license server

- purchased capacity is stored in `subscription_license_allocations`
- EMS reports consumption
- license server validates whether reported consumption exceeds purchased entitlement

### 5. Private signing keys stay on the license server

- EMS should only use the public verification key or decryption material required by the approved design
- private signing material must never be distributed to clients

## Target Lifecycle

### Stage 1. Subscription Request

Source:
- Website / portal

Process:
- company user purchases or requests a subscription plan
- portal sends the subscription request to license server
- license server creates a `subscriptions` row with status `PENDING`

Result:
- request is awaiting admin review

### Stage 2. Admin Review and Approval

Source:
- License Manager Admin

Process:
- admin reviews pending subscription
- business validation is performed:
  - company exists
  - plan exists
  - requested capacity is valid
  - overlap or upgrade policy is checked
- admin approves the subscription

Status transition:
- `PENDING -> APPROVED`

Result:
- subscription is eligible for license generation

### Stage 3. License Generation

Source:
- License Manager Admin

Process:
- admin generates the license for the approved subscription
- license server fetches:
  - company info
  - plan info
  - duration
  - mode
  - OS-wise allocations
  - product features
- license payload is built
- payload is signed with server private key
- signed payload is encrypted into `.lic`
- license is stored in `licenses`

Status transition:
- `APPROVED -> LICENSE_ISSUED`

Result:
- one encrypted license file exists for that subscription

Important rule:
- if a license already exists for the subscription, the system must return the existing license
- it must not regenerate a second different license for the same subscription

### Stage 4. License Download

Source:
- Admin or authorized company user

Process:
- user requests download
- license server checks that a license record already exists in DB
- server returns the stored `.lic`

Important rule:
- download must not generate a new license
- download must only return an already generated license

### Stage 5. EMS Verification

Source:
- EMS server

Process:
- EMS uploads or pastes `.lic`
- EMS or license server verifies:
  - payload can be decrypted
  - signature is valid
  - expiry is valid
  - subscription/license status is valid

Result:
- invalid licenses are rejected before activation

### Stage 6. EMS Activation

Source:
- EMS server

Process:
- EMS collects hardware identity
- recommended fingerprint inputs:
  - machine id
  - CPU identifier
  - motherboard identifier
  - MAC or approved network identifier
  - hostname
  - IP address
- EMS sends activation request to license server
- license server validates:
  - license exists
  - license is active
  - subscription is valid
  - license is not expired
  - EMS server is not already bound incorrectly
- activation record is created
- activation token is returned

Result:
- license is bound to one EMS server

Important rule:
- `license_activations` should represent EMS binding, not per-device endpoint consumption

### Stage 7. Consumption Sync

Source:
- EMS server periodic sync

Process:
- EMS periodically reports OS-wise consumed counts
- example:
  - Windows = 4
  - Linux = 8
- license server compares consumption with `subscription_license_allocations`
- if consumption exceeds allocation, server raises an overuse condition

Recommended table purpose:
- `subscription_license_allocations` = purchased capacity
- `license_consumption_summary` = latest reported usage

### Stage 8. Ongoing Validation

License server should continuously support:

- current activation status checks
- expiry checks
- overuse detection
- deactivation / suspension handling
- audit visibility for admin users

## Recommended Status Model

### Subscription statuses

- `PENDING`
- `APPROVED`
- `LICENSE_ISSUED`
- `ACTIVE`
- `SUPERSEDED`
- `EXPIRED`
- `CANCELLED`

### License statuses

- `ACTIVE`
- `REVOKED`
- `EXPIRED`

### Activation statuses

- `ACTIVE`
- `DEACTIVATED`
- `BLOCKED`

## Standard Business Scenarios

### Trial to paid upgrade

Scenario:
- company starts with trial
- later upgrades to paid plan

Rule:
- do not edit the old license
- create a new subscription
- approve and generate a new license
- mark old subscription as `SUPERSEDED` or `EXPIRED`

### Periodic renewal

Scenario:
- paid periodic subscription expires and is renewed

Rule:
- create a new subscription period
- generate a new license
- old subscription stays historical

### Perpetual purchase after periodic

Scenario:
- company purchases perpetual before the current periodic plan ends

Rule:
- create a new subscription
- issue a new perpetual license
- old periodic subscription is later marked `SUPERSEDED` or `EXPIRED` according to policy

### Device allocation change

Scenario:
- customer buys more Windows/Linux capacity

Rule:
- create a new subscription or amendment-driven new license event based on commercial policy
- avoid modifying the existing issued license in place

## Industry-Standard Data Responsibilities

### subscriptions

Stores:
- commercial approval state
- plan linkage
- company linkage
- validity period
- commercial status

### subscription_license_allocations

Stores:
- OS-wise purchased capacity

Example:
- Windows 5
- Linux 10

### licenses

Stores:
- immutable generated license artifact
- technical license metadata

### license_activations

Stores:
- EMS server binding
- activation token
- activation timestamps
- server identity

### license_consumption_summary

Stores:
- latest OS-wise device consumption reported by EMS

## Recommended API Flow

### Portal APIs

- create subscription request
- list subscription history

### Admin APIs

- list pending subscriptions
- approve subscription
- generate license
- download license
- deactivate subscription or license

### EMS APIs

- verify license
- activate license
- sync device consumption
- get usage summary

## Validation Rules

### Before approval

Validate:
- company exists
- product exists
- plan exists
- requested allocations are valid

### Before generation

Validate:
- subscription status is `APPROVED`
- no license already exists for the same subscription

### Before download

Validate:
- stored license exists

### Before activation

Validate:
- license exists
- subscription and license are active
- license not expired
- EMS binding rules pass

### During consumption sync

Validate:
- EMS activation exists
- license is active
- each `consumed_count <= allocated_count`

## Security Standard

- all APIs must require JWT except explicit public auth endpoints
- use role-based authorization for admin operations
- use audit columns: `created_by`, `updated_by`, `created_at`, `updated_at`
- use stored procedures for all DB writes and reads if that is the system standard
- private signing keys must be stored in secure server configuration
- all license downloads should be encrypted artifacts

## Audit and Support Requirements

System should maintain traceability for:

- who approved a subscription
- who generated a license
- when a license was issued
- which EMS server activated it
- latest consumption sync time
- deactivation and suspension events

## Recommended Operational Rules

- do not regenerate a license key once a license is already issued for a subscription
- do not modify historical license payloads
- do not trust EMS to decide entitlement limits
- do not store private signing keys in client applications
- keep old subscriptions for audit history

## Summary Flow

1. Portal creates subscription as `PENDING`
2. Admin reviews and approves subscription
3. License server generates one immutable encrypted license
4. License is stored and downloaded from DB
5. EMS verifies and activates the license
6. EMS periodically sends OS-wise consumption
7. License server validates usage against purchased allocation
8. Renewals and upgrades create new subscriptions and new licenses

