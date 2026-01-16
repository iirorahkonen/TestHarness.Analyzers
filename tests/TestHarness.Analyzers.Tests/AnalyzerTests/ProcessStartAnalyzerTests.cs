using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.Infrastructure;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class ProcessStartAnalyzerTests
{
    [Fact]
    public async Task ProcessStart_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Diagnostics;

            public class ProcessRunner
            {
                public void Run()
                {
                    {|#0:Process.Start("notepad.exe")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ProcessStartAnalyzer>
            .Diagnostic("SEAM018")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ProcessStartWithInfo_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Diagnostics;

            public class ProcessRunner
            {
                public void Run()
                {
                    var startInfo = {|#0:new ProcessStartInfo("notepad.exe")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ProcessStartAnalyzer>
            .Diagnostic("SEAM018")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewProcessWithStart_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Diagnostics;

            public class ProcessRunner
            {
                public void Run()
                {
                    {|#0:new Process()|}.Start();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ProcessStartAnalyzer>
            .Diagnostic("SEAM018")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewProcessWithoutStart_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Diagnostics;

            public class ProcessRunner
            {
                public Process Create()
                {
                    return new Process();
                }
            }
            """;

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InjectedProcessRunner_ShouldNotReportDiagnostic()
    {
        const string source = """
            public interface IProcessRunner
            {
                void Start(string fileName);
            }

            public class CommandExecutor
            {
                private readonly IProcessRunner _processRunner;

                public CommandExecutor(IProcessRunner processRunner)
                {
                    _processRunner = processRunner;
                }

                public void Execute(string command)
                {
                    _processRunner.Start(command);
                }
            }
            """;

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ProcessStartWithProcessStartInfo_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Diagnostics;

            public class ProcessRunner
            {
                public void Run()
                {
                    var info = new ProcessStartInfo { FileName = "notepad.exe" };
                    {|#0:Process.Start(info)|};
                }
            }
            """;

        // Note: new ProcessStartInfo will also be flagged
        var expected1 = CSharpAnalyzerVerifier<ProcessStartAnalyzer>
            .Diagnostic("SEAM018")
            .WithSpan(7, 24, 7, 67);

        var expected2 = CSharpAnalyzerVerifier<ProcessStartAnalyzer>
            .Diagnostic("SEAM018")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
    }

    [Fact]
    public async Task ProcessStartReturningProcess_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Diagnostics;

            public class ProcessRunner
            {
                public Process Run()
                {
                    return {|#0:Process.Start("notepad.exe")|}!;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ProcessStartAnalyzer>
            .Diagnostic("SEAM018")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ProcessStartAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
