# License Server Management API QA Suite

Base URL: `http://localhost:5000/api`
Auth: `Bearer <accessToken>`

## Current API Coverage Review
Implemented in current codebase:
- Companies: `GET`, `GET by id`, `GET details`, `POST`, `PUT`, `PATCH /ban`
- Subscription Plans: `GET`, `GET by id`, `POST`, `PUT`, `PATCH /deactivate`
- Subscriptions: `POST`, `POST /{id}/approve`
- Users: `GET`, `GET by id`, `POST`, `PUT`, `PATCH /deactivate`
- Auth: `POST /auth/login`, `POST /auth/logout`, `POST /auth/refresh`

Requested but not currently implemented:
- Company delete
- Plan delete
- Plan ban endpoint name separate from deactivate
- Subscription reject
- Subscription status view/list
- User delete
- Pagination on list endpoints

Use those missing endpoints as gap tests and expected failures until implemented.

## Common Headers
```http
Authorization: Bearer <accessToken>
Content-Type: application/json
Accept: application/json
```

## Auth
### Login
Request JSON:
```json
{
  "email": "superadmin@metronux.com",
  "password": "Admin@123"
}
```
Curl:
```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "superadmin@metronux.com",
    "password": "Admin@123"
  }'
```
Expected status: `200 OK`
Expected response:
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<refresh-token>",
  "email": "superadmin@metronux.com",
  "role": "SUPERADMIN"
}
```
Negative case: invalid password -> expected `401` or `400` depending on current controller behavior.

## Company APIs
### Add Company
Request JSON:
```json
{
  "name": "TechCorp Solutions",
  "industry": "Technology",
  "contactPerson": "John Admin",
  "primaryMobile": "+1-555-1000",
  "alternateMobile": "+1-555-1001",
  "email": "admin@techcorp.com",
  "status": "Active",
  "statusDescription": "Initial onboarding"
}
```
Curl:
```bash
curl -X POST "http://localhost:5000/api/companies" \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TechCorp Solutions",
    "industry": "Technology",
    "contactPerson": "John Admin",
    "primaryMobile": "+1-555-1000",
    "alternateMobile": "+1-555-1001",
    "email": "admin@techcorp.com",
    "status": "Active",
    "statusDescription": "Initial onboarding"
  }'
```
Expected status: `200 OK`
Expected response shape:
```json
{
  "id": 101,
  "name": "TechCorp Solutions",
  "email": "admin@techcorp.com",
  "industry": "Technology",
  "contactPerson": "John Admin",
  "primaryMobile": "+1-555-1000",
  "alternateMobile": "+1-555-1001",
  "status": "Active",
  "statusDescription": "Initial onboarding",
  "linkedSubscriptions": [],
  "linkedLicenses": []
}
```
Test cases:
- Valid create -> `200`
- Missing `name` -> `400`
- Missing `email` -> `400`
- Duplicate company check: API currently has no `company_code` field and no visible duplicate-code contract. Mark as gap unless DB constraint exists.
- Unauthorized -> `401`

### Edit Company
Expected status: `200 OK`
Test cases:
- Update contact person and status description
- Update to banned-like state only through `/ban`; regular update should not be used as substitute unless product team confirms
- Invalid company id -> `404`

### Ban Company
Request JSON:
```json
{
  "reason": "Compliance hold for QA scenario"
}
```
Expected status: `200 OK`
Expected response:
```json
{
  "id": 101,
  "message": "Company banned successfully."
}
```
Validation:
- Verify subsequent `GET /api/companies/{id}` shows suspended/banned state
- Verify linked subscriptions/licenses are restricted according to DB procedure behavior

### Delete Company
Current status: not implemented
Suggested gap test:
```bash
curl -X DELETE "http://localhost:5000/api/companies/101" \
  -H "Authorization: Bearer <accessToken>"
```
Expected current result: `404 Not Found`

## Subscription Plan APIs
### Add Plan
Request JSON:
```json
{
  "planCode": "",
  "planName": "Business Plus",
  "productId": 1,
  "mode": "PERIODIC",
  "status": "ACTIVE",
  "price": 4999,
  "durationDays": 365,
  "billingLabel": "365 Days (1 Year)",
  "deviceLimit": 100,
  "deviceLimitLabel": "100 Devices",
  "description": "Designed for large-scale enterprise deployments",
  "highlights": [
    "Priority onboarding",
    "Flexible deployment"
  ],
  "features": [
    "24/7 Support",
    "Advanced Analytics"
  ],
  "isActive": true
}
```
Expected status: `200 OK`
Expected response shape:
```json
{
  "id": 10,
  "planCode": "PLAN-XXXX",
  "planName": "Business Plus",
  "productId": 1,
  "productName": "Metronux EMS",
  "mode": "PERIODIC",
  "status": "ACTIVE",
  "price": 4999,
  "durationDays": 365,
  "billingLabel": "365 Days (1 Year)",
  "deviceLimit": 100,
  "deviceLimitLabel": "100 Devices",
  "description": "Designed for large-scale enterprise deployments",
  "highlights": ["Priority onboarding", "Flexible deployment"],
  "features": ["24/7 Support", "Advanced Analytics"],
  "isActive": true
}
```
Negative tests:
- `durationDays < 0` -> `400`
- `deviceLimit < 0` -> `400`
- missing `planName` -> `400`
- unauthorized -> `401`

### Edit Plan
Test cases:
- Modify `deviceLimit` from `100` to `120` -> `200`
- Modify highlights/features -> `200`
- Invalid id -> `404`

### Disable/Ban Plan
Current implemented endpoint: `PATCH /api/subscription-plans/{id}/deactivate`
Expected status: `200 OK`
Expected body:
```json
{
  "id": 10,
  "message": "Subscription plan deactivated successfully."
}
```

### Delete Plan
Current status: not implemented
Expected current result: `404`

## Manage Subscription APIs
### Create Subscription Request - Default Plan
Request JSON:
```json
{
  "companyId": 1,
  "planCategory": "DEFAULT",
  "planId": 2,
  "licenseMode": "ONLINE",
  "allocations": [
    { "osTypeId": 1, "allocatedCount": 40 },
    { "osTypeId": 2, "allocatedCount": 30 },
    { "osTypeId": 3, "allocatedCount": 20 },
    { "osTypeId": 4, "allocatedCount": 10 }
  ]
}
```
Expected status: `200 OK`
Expected response shape:
```json
{
  "id": 200,
  "companyId": 1,
  "companyName": "TechCorp Solutions",
  "planId": 2,
  "planName": "Professional",
  "planCategory": "DEFAULT",
  "status": "PENDING",
  "startDate": "2026-03-22T00:00:00",
  "endDate": "2027-03-22T00:00:00",
  "licenseMode": "ONLINE",
  "deviceLimit": 100,
  "totalAllocated": 100,
  "allocations": [
    { "osTypeId": 1, "osName": "WINDOWS", "allocatedCount": 40 }
  ]
}
```
Negative tests:
- total allocation > plan limit -> `400`
- missing `planId` for default -> `400`
- no allocations -> `400`
- invalid `companyId` -> DB exception surfaced as `500` or `400` depending on runtime path

### Create Subscription Request - Custom Plan
Request JSON:
```json
{
  "companyId": 1,
  "planCategory": "CUSTOM",
  "productId": 1,
  "startDate": "2026-03-22T00:00:00",
  "durationDays": 30,
  "licenseMode": "ONLINE",
  "allocations": [
    { "osTypeId": 1, "allocatedCount": 5 },
    { "osTypeId": 2, "allocatedCount": 10 },
    { "osTypeId": 3, "allocatedCount": 0 },
    { "osTypeId": 4, "allocatedCount": 0 }
  ]
}
```
Negative tests:
- missing `productId` -> `400`
- missing `durationDays` -> `400`
- allocation total `0` -> DB validation failure

### Approve Subscription
Curl:
```bash
curl -X POST "http://localhost:5000/api/subscriptions/200/approve" \
  -H "Authorization: Bearer <accessToken>"
```
Expected status: `200 OK`
Expected response:
```json
{
  "subscriptionId": 200,
  "message": "Subscription approved successfully."
}
```
Negative tests:
- approve non-existent subscription -> `400` or `404` depending on SP/controller behavior
- approve already approved subscription -> `400`

### Reject Subscription
Current status: not implemented
Expected current result: `404`

### View Subscription Status
Current status: not implemented
Expected current result: `404`

### Duplicate Active Subscription Prevention
Current status: business rule not exposed in current controller layer. Treat as DB/business gap test unless implemented later.

### Expired Subscription Handling
Current status: no direct status-view endpoint. Validate later via license generation/activation workflows after subscription dates expire.

## User APIs
### Add User
Request JSON:
```json
{
  "name": "QA User",
  "email": "qa.user@example.com",
  "role": "VIEWER",
  "designation": "QA Engineer",
  "mobile": "+1-555-3000",
  "alternateMobile": "+1-555-3001",
  "isDisabled": false
}
```
Expected status: `200 OK`
Expected response shape:
```json
{
  "id": 301,
  "name": "QA User",
  "email": "qa.user@example.com",
  "role": "VIEWER",
  "designation": "QA Engineer",
  "mobile": "+1-555-3000",
  "alternateMobile": "+1-555-3001",
  "isDisabled": false
}
```
Notes:
- current API does not take `password`, `company_id`, or `status` in the request body
- server generates a temporary password internally

Negative tests:
- duplicate email -> expected DB constraint error if present
- invalid email -> `400`
- missing role -> `400`

### Update User
Test cases:
- update `role` from `VIEWER` to `ADMIN` -> `200`
- invalid user id -> `404`

### Delete User
Current implemented behavior is deactivate, not delete:
- `PATCH /api/users/{id}/deactivate`
Expected status: `200 OK`
Expected response:
```json
{
  "id": 301,
  "message": "User deactivated successfully."
}
```
True delete endpoint currently not implemented.

## Authentication Failure Scenario
Example:
```bash
curl -X GET "http://localhost:5000/api/companies" \
  -H "Accept: application/json"
```
Expected status: `401 Unauthorized`
Expected header contains bearer challenge.

## Pagination
Not applicable right now.
Current list APIs do not expose `page`, `pageSize`, `limit`, or `offset` parameters.
If query params are sent, they are ignored by the current controllers.

## Recommended QA Execution Order
1. Login and capture bearer token
2. Add company
3. Add plan
4. Create subscription request
5. Approve subscription
6. Add user
7. Edit company / plan / user
8. Ban company / deactivate plan / deactivate user
9. Run negative and unauthorized tests
10. Run not-implemented endpoint gap tests
