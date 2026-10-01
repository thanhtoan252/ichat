# Pipeline RAG

## Chunking

Kich thuoc chunk muc tieu la 800 token, chong lan 120 token giua hai chunk ke nhau.
Chunk nho hon 80 token se duoc gop vao chunk ke. Bang khong bao gio bi cat doi.

## Hybrid search

He thong chay ba nhanh tim kiem song song: vector, full-text va trigram.
Nhanh trigram mac dinh bi tat, bat bang cach dat TrigramCandidates lon hon 0.

Ket qua ba nhanh duoc tron bang thuat toan Reciprocal Rank Fusion voi hang so k bang 60.
RRF la buoc fusion chu khong phai rerank.

## Khu trung lap

Sau khi fusion, he thong ap dung Maximal Marginal Relevance voi lambda bang 0.7
de tranh top-8 toan la cac manh chong lan cua cung mot trang.
Moi tai lieu chi duoc gop toi da 3 chunk vao ket qua cuoi.

## Mo rong lan can

Voi moi chunk duoc chon, he thong nap them 1 chunk truoc va 1 chunk sau trong cung tai lieu
bang mot truy van duy nhat. Chunk lan can chi lam giau context chu khong tro thanh citation.
