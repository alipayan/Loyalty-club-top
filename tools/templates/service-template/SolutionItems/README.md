# CustomerClub Service Template (Initial)

این template یک اسکلت اولیه برای ایجاد سرویس جدید است و روی BuildingBlocks های پروژه سوار می‌شود.

## Included projects
- `CustomerClub.ServiceTemplate.Api`
- `CustomerClub.ServiceTemplate.Application`
- `CustomerClub.ServiceTemplate.Domain`
- `CustomerClub.ServiceTemplate.Infrastructure`
- `CustomerClub.ServiceTemplate.Contracts`
- Unit/Application/Integration/Contract tests

## How to use (manual)
1. فولدر `service-template` را کپی کنید داخل `src/Services/<ServiceName>`.
2. عبارت `ServiceTemplate` را با نام سرویس (مثل `Member`) جایگزین کنید.
3. رفرنس‌های BuildingBlocks را مطابق نیاز سرویس تنظیم کنید.
4. endpoint نمونه و test نمونه را با use case واقعی سرویس جایگزین کنید.

## Baseline capabilities
- Service defaults wiring
- Health endpoints (`/health/live`, `/health/ready`)
- OpenAPI + ProblemDetails
- Correlation id propagation
- Sample endpoint + sample command + sample integration event
