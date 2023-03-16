// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using static SyntaxRewriterTest.BuiltInFileSets;

public static class SyntaxRewriterTest
{
    private static readonly string DefaultFileSet = RoslynSrc;

    public static int Main(string[] args)
    {
        string path;

        string? builtInFileSet = null;
        if (args.Length == 1)
        {
            builtInFileSet = args[0].ToLower();
        }

        if (args.Length == 0 || builtInFileSet is RoslynSrc or Src or TestFileA or DefaultTestFile)
        {
            path = Environment.CurrentDirectory;
            string exeBasePath = AppDomain.CurrentDomain.BaseDirectory;

            // Use the current directory if it's different from the app's base directory and if no file set was specified.
            var currentPathWithoutTrailingSlash = Path.TrimEndingDirectorySeparator(path.AsSpan());
            var exeBasePathWithoutTrailingSlash = Path.TrimEndingDirectorySeparator(exeBasePath.AsSpan());
            if (currentPathWithoutTrailingSlash.SequenceEqual(exeBasePathWithoutTrailingSlash) || builtInFileSet != null)
            {
                var parentDir = new DirectoryInfo(exeBasePath);
                string srcPath;
                while (true)
                {
                    srcPath = Path.Join(parentDir.FullName, "src");
                    if (Directory.Exists(srcPath))
                        break;

                    parentDir = parentDir.Parent;
                    if (parentDir == null)
                        throw new DirectoryNotFoundException("Couldn't find the root of roslyn's source code.");
                }

                builtInFileSet ??= DefaultFileSet;
                if (builtInFileSet is TestFileA or DefaultTestFile)
                {
                    path = $"{srcPath}/Test/Perf/AnalysisTestFileA.cs";
                }
                else if (builtInFileSet is RoslynSrc or Src)
                {
                    path = srcPath;
                }
            }
        }
        else
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Tests.SyntaxRewriter expected no more than a single optional path argument representing a file or directory or a built-in file set.");
                return Result.Error;
            }
            path = args[0];
        }

        SetupEnvironment();

        AnalyzeFileOrDirectory(path);

        return Result.Success;
    }

    public static class BuiltInFileSets
    {
        public const string Src = "src";
        public const string Roslyn = "rosyln";
        public const string DefaultTestFile = "testfile";
        public const string TestFileA = "testfilea";
        public const string RoslynSrc = Roslyn;
    }

    private static class Result
    {
        public const int Success = 0;
        public const int Error = -1;
    }

    private static void SetupEnvironment()
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
    }

    private const string DefaultSearchPattern = "*.cs";

    private const SearchOption DefaultSearchOption = SearchOption.AllDirectories;

    public static void AnalyzeFileOrDirectory(string path, string directorySearchPattern = DefaultSearchPattern, SearchOption directorySearchOption = DefaultSearchOption)
    {
        path = Path.GetFullPath(path);
        var file = new FileInfo(path);
        if (file.Exists)
        {
            Analyze(file);
        }
        else
        {
            Analyze(new DirectoryInfo(path), directorySearchPattern, directorySearchOption);
        }
    }

    public static void Analyze(DirectoryInfo root, string searchPattern = DefaultSearchPattern, SearchOption searchOption = DefaultSearchOption)
    {
        foreach (FileInfo file in root.EnumerateFiles(searchPattern, searchOption))
        {
            Analyze(file);
        }
    }

    public static void Analyze(FileInfo file)
    {
        using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Analyze(stream);
    }

    public static void Analyze(Stream stream)
    {
        var src = SourceText.From(stream);
        var root = CSharpSyntaxTree.ParseText(src).GetRoot();
        new Rewriter().Visit(root);
    }

    private class Rewriter : CSharpSyntaxRewriter
    {
        private readonly Random _randomGen = new();

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            SyntaxToken a, b;
            if (_randomGen.Next(2) == 0)
            {
                a = token.GetNextToken();
                b = token.GetNextToken4();
            }
            else
            {
                b = token.GetNextToken4();
                a = token.GetNextToken();
            }
            if (a != b)
                throw new InvalidOperationException($"^a: {a.FullSpan.Start}, ^b: {b.FullSpan.Start}; a: {a.ValueText}, b: {b.ValueText}");
            return base.VisitToken(token);
        }
    }
}