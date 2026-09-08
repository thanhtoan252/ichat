using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// Đo chất lượng retrieval. Không có bước này thì mọi con số trong RagOptions chỉ là
// phỏng đoán, và không có cách nào biết một thay đổi làm hệ thống tốt lên hay tệ đi.

var baseUrl = Environment.GetEnvironmentVariable("ICHAT_URL") ?? "http://localhost:8080";
var command = args.Length > 0 ? args[0] : "eval";

using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) };
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

switch (command)
{
    case "seed":
        await SeedAsync();

        break;

    case "compare":
        await CompareAsync();

        break;

    default:
        await RunAndPrintAsync("default", rewrite: true, prependNote: null);

        break;
}

async Task SeedAsync()
{
    var corpusDirectory = Path.Combine(AppContext.BaseDirectory, "corpus");

    if (!Directory.Exists(corpusDirectory))
    {
        Console.Error.WriteLine($"Corpus directory not found: {corpusDirectory}");

        return;
    }

    foreach (var path in Directory.GetFiles(corpusDirectory, "*.md").OrderBy(path => path))
    {
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(await File.ReadAllBytesAsync(path));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/markdown");
        form.Add(content, "file", Path.GetFileName(path));
        form.Add(new StringContent(Path.GetFileNameWithoutExtension(path)), "title");

        var response = await client.PostAsync("/api/v1/documents", form);
        Console.WriteLine($"  {Path.GetFileName(path),-24} -> {(int)response.StatusCode}");
    }

    Console.WriteLine("Corpus seeded. Wait for the worker to finish indexing, then run `eval`.");
}

async Task CompareAsync()
{
    // So sánh trực tiếp hai cấu hình. Nhóm "followup" là thứ chứng minh query rewriting
    // có tác dụng: tắt rewriting thì recall của nhóm này sụt hẳn.
    var withRewriting = await RunAndPrintAsync("QueryRewriting ON", rewrite: true, prependNote: null);
    var withoutRewriting = await RunAndPrintAsync("QueryRewriting OFF", rewrite: false, prependNote: null);

    Console.WriteLine();
    Console.WriteLine("recall@8 compared by question group");
    Console.WriteLine(new string('─', 62));
    Console.WriteLine($"{"group",-14}{"on",10}{"off",10}{"delta",16}");
    Console.WriteLine(new string('─', 62));

    foreach (var tag in withRewriting.ByTag.Keys.OrderBy(tag => tag))
    {
        var on = withRewriting.ByTag[tag].Recall;
        var off = withoutRewriting.ByTag.TryGetValue(tag, out var value) ? value.Recall : 0;
        Console.WriteLine($"{tag,-14}{on,10:P0}{off,10:P0}{on - off,16:P0}");
    }

    Console.WriteLine(new string('─', 62));
}

async Task<EvalSummary> RunAndPrintAsync(string label, bool rewrite, string? prependNote)
{
    var goldenSetPath = Path.Combine(AppContext.BaseDirectory, "golden-set.json");
    var goldenSet = JsonSerializer.Deserialize<List<GoldenItem>>(await File.ReadAllTextAsync(goldenSetPath), jsonOptions)
        ?? throw new InvalidOperationException("Could not read golden-set.json.");

    var results = new List<ItemResult>();

    foreach (var item in goldenSet)
    {
        var request = new
        {
            query = item.Question,
            topK = 8,
            mode = "Hybrid",
            rewrite,
            history = item.History,
            applyMmr = true,
            expandNeighbors = true,
            rerank = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request, jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"  [error {(int)response.StatusCode}] {item.Question}");
            results.Add(new ItemResult
            {
                Tag = item.Tag,
                Found = false,
                Rank = 0,
                FullTextEmpty = false
            });

            continue;
        }

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var stages = payload.GetProperty("stages");

        var fullTextEmpty = stages.TryGetProperty("fulltext", out var fullText)
            && fullText.GetProperty("count").GetInt32() == 0;

        var hits = stages.GetProperty("final").GetProperty("top").EnumerateArray()
            .Select(hit => hit.GetProperty("snippet").GetString() ?? string.Empty)
            .ToList();

        var rank = 0;
        for (var i = 0; i < hits.Count; i++)
        {
            if (item.ExpectedAnswerContains.Any(expected => hits[i].Contains(expected, StringComparison.OrdinalIgnoreCase)))
            {
                rank = i + 1;

                break;
            }
        }

        results.Add(new ItemResult
        {
            Tag = item.Tag,
            Found = rank > 0,
            Rank = rank,
            FullTextEmpty = fullTextEmpty
        });
    }

    var summary = Summarise(results);

    Console.WriteLine();
    Console.WriteLine($"╔══ Eval result: {label} ══");
    if (prependNote is not null)
    {
        Console.WriteLine($"║ {prependNote}");
    }

    Console.WriteLine($"║ questions                  : {results.Count}");
    Console.WriteLine($"║ recall@8                   : {summary.Recall:P1}");
    Console.WriteLine($"║ MRR                        : {summary.Mrr:F3}");
    Console.WriteLine($"║ full-text empty rate       : {summary.FullTextEmptyRate:P1}");
    Console.WriteLine("╠══ by group ══");
    Console.WriteLine($"║ {"group",-14}{"recall@8",10}{"MRR",8}{"n",5}");

    foreach (var (tag, group) in summary.ByTag.OrderBy(entry => entry.Key))
    {
        Console.WriteLine($"║ {tag,-14}{group.Recall,10:P0}{group.Mrr,8:F3}{group.Count,5}");
    }

    Console.WriteLine("╚════════════════════════════");

    return summary;
}

static EvalSummary Summarise(List<ItemResult> results)
{
    static (double Recall, double Mrr, int Count) Aggregate(IReadOnlyCollection<ItemResult> items)
    {
        if (items.Count == 0)
        {
            return (0, 0, 0);
        }

        return (
            items.Count(item => item.Found) / (double)items.Count,
            items.Sum(item => item.Rank > 0 ? 1.0 / item.Rank : 0) / items.Count,
            items.Count);
    }

    var overall = Aggregate(results);

    var byTag = results
        .GroupBy(result => result.Tag)
        .ToDictionary(group => group.Key, group => Aggregate(group.ToList()));

    var emptyRate = results.Count == 0 ? 0 : results.Count(result => result.FullTextEmpty) / (double)results.Count;

    return new EvalSummary
    {
        Recall = overall.Recall,
        Mrr = overall.Mrr,
        FullTextEmptyRate = emptyRate,
        ByTag = byTag
    };
}
