using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BitScheduler.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        var projectRoot = FindProjectRoot();
        var repositoryRoot = Directory.GetParent(projectRoot)?.FullName
            ?? throw new InvalidOperationException("Could not determine the repository root.");
        var artifactsRoot = Path.Combine(repositoryRoot, "BenchmarkDotNet.Artifacts");
        var resultsRoot = Path.Combine(artifactsRoot, "results");
        var projectResultsRoot = Path.Combine(projectRoot, "BenchmarkResults");

        Directory.CreateDirectory(resultsRoot);
        Directory.CreateDirectory(projectResultsRoot);

        RunAndStore<DatabaseScheduleBenchmarks>(
            args,
            Path.Combine(artifactsRoot, nameof(DatabaseScheduleBenchmarks)),
            [
                Path.Combine(resultsRoot, "DatabaseScheduleBenchmarks.latest.md"),
                Path.Combine(projectResultsRoot, "DatabaseScheduleBenchmarks.latest.md")
            ]);

        RunAndStore<BitDayReserveFreeBenchmarks>(
            args,
            Path.Combine(artifactsRoot, nameof(BitDayReserveFreeBenchmarks)),
            [
                Path.Combine(resultsRoot, "BitDayReserveFreeBenchmarks.latest.md"),
                Path.Combine(projectResultsRoot, "BitDayReserveFreeBenchmarks.latest.md")
            ]);

        return 0;
    }

    private static void RunAndStore<TBenchmark>(string[] args, string artifactsPath, IReadOnlyList<string> outputFilePaths)
    {
        var summary = BenchmarkRunner.Run<TBenchmark>(CreateConfig(artifactsPath), args);
        WriteMarkdownSummary<TBenchmark>(summary, outputFilePaths);
    }

    private static IConfig CreateConfig(string artifactsPath)
    {
        return ManualConfig
            .Create(DefaultConfig.Instance)
            .AddExporter(MarkdownExporter.GitHub)
            .WithArtifactsPath(artifactsPath);
    }

    private static void WriteMarkdownSummary<TBenchmark>(Summary summary, IReadOnlyList<string> outputFilePaths)
    {
        var markdownReportPath = FindMarkdownReport<TBenchmark>(summary);
        var markdown = new StringBuilder()
            .AppendLine($"# {typeof(TBenchmark).Name}")
            .AppendLine()
            .AppendLine($"Generated: {DateTimeOffset.Now:O}")
            .AppendLine();

        if (markdownReportPath is not null)
        {
            markdown.AppendLine(File.ReadAllText(markdownReportPath));
        }
        else
        {
            markdown.AppendLine("## Benchmarks")
                .AppendLine();

            foreach (var report in summary.Reports)
            {
                markdown.AppendLine($"- `{report.BenchmarkCase.Descriptor.WorkloadMethod.Name}`");
            }
        }

        foreach (var outputFilePath in outputFilePaths)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
            File.WriteAllText(outputFilePath, markdown.ToString());
        }
    }

    private static string? FindMarkdownReport<TBenchmark>(Summary summary)
    {
        if (!Directory.Exists(summary.ResultsDirectoryPath))
        {
            return null;
        }

        return Directory
            .GetFiles(summary.ResultsDirectoryPath, "*-report-github.md", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileNameWithoutExtension(path).Contains(typeof(TBenchmark).Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BitScheduler.Benchmarks.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not determine the BitScheduler.Benchmarks project root.");
    }
}
