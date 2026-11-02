# Tich hop OpenAI

Chat, utility chat va embedding deu goi OpenAI API. Cac model duoc cau hinh rieng trong
`Ai:Chat:Model`, `Ai:UtilityChat:Model` va `Ai:Embedding:Model`; API key duoc nap tu
`Ai:Chat:ApiKey` va `Ai:Embedding:ApiKey`. Utility chat co the dung cung key chat neu
khong khai bao key rieng.

Cot vector cua pgvector co dinh 1536 chieu. `text-embedding-3-small` duoc cau hinh tra ve
1536 chieu. Doi model embedding can reindex toan bo tai lieu de vector cu va moi khong bi
tron lan.
