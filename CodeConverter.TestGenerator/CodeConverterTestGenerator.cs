using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeConverter.TestGenerator;

[Generator]
public class CodeConverterTestGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization needed
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var vbInputFiles = context.AdditionalFiles
            .Where(at => at.Path.EndsWith(".input.vb"))
            .ToList();

        if (!vbInputFiles.Any())
            return;

        var sb = new StringBuilder();
        sb.AppendLine("using Xunit;");
        sb.AppendLine("using VerifyXunit;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace CodeConverter.Tests.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class GeneratedConverterTests");
        sb.AppendLine("    {");

        foreach (var inputFile in vbInputFiles)
        {
            string fullInputPath = inputFile.Path;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullInputPath);
            string testIdentifier = fileNameWithoutExtension.Replace(".input", "");
            string testMethodName = testIdentifier.Replace(".", "_") + "_VerifyConversion";
            string testFileDirectory = Path.GetDirectoryName(fullInputPath);
            string escapedInputPath = fullInputPath.Replace(@"\", @"\\");
            string escapedTestDirectory = testFileDirectory.Replace(@"\", @"\\");

            sb.AppendLine($"        [Fact]");
            sb.AppendLine($"        public static async Task {testMethodName}()");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            string inputFilePath = @\"{escapedInputPath}\";");
            sb.AppendLine($"            string inputVbCode = await File.ReadAllTextAsync(inputFilePath);");
            sb.AppendLine($"            string convertedCsCode = YourNamespace.YourConverter.Convert(inputVbCode); // Replace with your converter logic");
            sb.AppendLine($"            await Verifier.Verify(convertedCsCode)");
            sb.AppendLine($"                .UseFileName(\"{testIdentifier}\")");
            sb.AppendLine($"                .UseDirectory(@\"{escapedTestDirectory}\");");
            sb.AppendLine($"        }}");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"GeneratedConverterTests.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}