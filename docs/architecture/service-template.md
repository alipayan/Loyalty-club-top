# Service Template (Initial)

template اولیه در مسیر زیر قرار دارد:

- `tools/templates/service-template`

## What you get
- لایه‌های Api/Application/Domain/Infrastructure/Contracts
- تست‌های Unit/Application/Integration/Contract
- Program bootstrap با service defaults + OpenAPI + health checks
- endpoint نمونه و command handler نمونه
- event contract نمونه
- Dockerfile و appsettings
- CI skeleton
- ADR link stub

## Create new service
```bash
./tools/scripts/new-service-template.sh Member
```

سپس:
1. نام‌ها و namespaceها را نهایی کنید.
2. sample artifacts را با use case واقعی جایگزین کنید.
3. contractها و docs سرویس را ثبت کنید.
