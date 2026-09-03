# Huong dan van hanh IChat

## Cai dat

Chay `docker compose up -d` de khoi dong Postgres kem pgvector va API.
API lang nghe tai cong 8080 khi chay bang Docker, va cong 5140 khi chay bang `dotnet run`.

## Cau hinh timeout

Timeout mac dinh cua provider chat la 120 giay. Gia tri nay doc tu appsettings.json,
khoa `Ai:Chat:TimeoutSeconds`. Vuot qua timeout thi ket noi SSE bi dong, phan da sinh
van duoc luu kem ghi chu interrupted.

## Reindex

Khi doi model embedding, bat buoc phai goi POST /api/v1/admin/reindex. Neu khong,
vector cu va vector moi nam o hai khong gian khac nhau va ket qua search sai am tham.

## Gioi han upload

Kich thuoc file toi da la 20MB. Chi nhan bon dinh dang: .docx, .pdf, .md va .txt.
File .doc cua Word 97-2003 khong duoc ho tro, phai luu lai thanh .docx.
