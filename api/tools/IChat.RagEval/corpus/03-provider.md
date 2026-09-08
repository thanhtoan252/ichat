# Da nha cung cap LLM

## Tach chat va embedding

Cau hinh chat va cau hinh embedding la hai khoi doc lap va co the chon khac nha cung cap.
Ly do rat cu the: Anthropic khong cung cap API embedding.
Neu muon Claude sinh cau tra loi thi vector van phai lay tu cho khac, vi du OpenAI hoac Voyage.

## Cac nha cung cap duoc ho tro

He thong ho tro OpenAI, Azure OpenAI, Anthropic va Google Gemini.
Moi nha cung cap deu bat buoc phai co API key; rieng Azure OpenAI con bat buoc phai co Endpoint.

## So chieu vector

Cot vector trong pgvector co dinh so chieu o cap schema, mac dinh la 1536 chieu.
Model text-embedding-3-small cho 1536 chieu, con text-embedding-3-large cho 3072 chieu.

Doi model embedding la breaking change o tang du lieu. Phai chay lai POST /api/v1/admin/reindex,
neu khong vector cu va moi se nam o hai khong gian khac nhau va search sai mot cach im lang.
