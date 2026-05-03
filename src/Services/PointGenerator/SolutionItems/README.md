# CustomerClub Service Template (Initial)

این template یک اسکلت اولیه برای ایجاد سرویس جدید است و روی BuildingBlocks های پروژه سوار می‌شود.

## Included projects
- `CustomerClub.PointGenerator.Api`
- `CustomerClub.PointGenerator.Application`
- `CustomerClub.PointGenerator.Domain`
- `CustomerClub.PointGenerator.Infrastructure`
- `CustomerClub.PointGenerator.Contracts`
- Unit/Application/Integration/Contract tests

## How to use (manual)
1. فولدر `service-template` را کپی کنید داخل `src/Services/<ServiceName>`.
2. عبارت `PointGenerator` را با نام سرویس (مثل `Member`) جایگزین کنید.
3. رفرنس‌های BuildingBlocks را مطابق نیاز سرویس تنظیم کنید.
4. endpoint نمونه و test نمونه را با use case واقعی سرویس جایگزین کنید.

## Baseline capabilities
- Service defaults wiring
- Health endpoints (`/health/live`, `/health/ready`)
- OpenAPI + ProblemDetails
- Correlation id propagation
- Sample endpoint + sample command + sample integration event
