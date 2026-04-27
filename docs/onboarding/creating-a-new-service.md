# Creating a New Service (Quick Start)

1. bounded context و scope سرویس را نهایی کنید.
2. در صورت نیاز ADR ثبت کنید.
3. سرویس را از template بسازید:
   - `./tools/scripts/new-service-template.sh <ServiceName>`
4. API / Event contracts سرویس را تکمیل کنید.
5. health / telemetry / auth baseline را بررسی کنید.
6. تست‌های پایه را از حالت placeholder خارج کنید.
7. README سرویس + runbook اولیه را بنویسید.
