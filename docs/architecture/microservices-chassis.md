# Customer Club Microservices Chassis (Initial Implementation)

این نسخه، baseline اولیه chassis را در کد ایجاد می‌کند تا سرویس‌ها از روز اول استاندارد مشترک داشته باشند.

## Implemented building blocks
- `ServiceDefaults`: bootstrap پایه سرویس، health checks و correlation header.
- `Api`: problem details + OpenAPI wiring.
- `Observability`: naming و log-field conventions.
- `Messaging`: event envelope + publisher abstraction.
- `Security`: claim extraction helperها.
- `Persistence`: outbox record برای eventual consistency.
- `Contracts`: base integration event model.
- `Testing`: fake auth handler برای integration tests.

## Current scope
این پیاده‌سازی intentionally lightweight است تا تیم بتواند سریع MVP فنی را بالا بیاورد و در iterationهای بعدی جزئیات مثل OTel exporter، broker adapter واقعی، resiliency policy و outbox dispatcher را اضافه کند.
