# Cai dat he thong IChat

## Yeu cau moi truong

He thong yeu cau .NET 10 va PostgreSQL 17 co extension pgvector phien ban 0.8 tro len.
Bo nho toi thieu cho service api la 512 MB.

## Cau hinh bien moi truong

De cau hinh bien moi truong cho ung dung, ban mo file appsettings.json va dat khoa
ConnectionStrings__Default. API key khong bao gio duoc ghi truc tiep vao file cau hinh,
chi ghi TEN cua bien moi truong chua key do.

Gia tri timeout mac dinh cho mot luot chat la 120 giay. Rieng utility chat dung timeout
15 giay vi no chi lam cac tac vu ngan.

## Chay bang Docker

Chay lenh docker compose up de khoi dong ca postgres va api. Cong mac dinh cua api la 8080.
