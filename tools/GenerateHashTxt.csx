#!/usr/bin/env dotnet-script
// Generates loader/interop/assembly-hash.txt from GameAssembly.dll
// Run: dotnet script tools/GenerateHashTxt.csx -- /path/to/GameAssembly.dll
// Re-run after game updates that change GameAssembly.dll
// Algorithm must match InteropManager.ComputeSHA256() exactly

using System;
using System.IO;
using System.Security.Cryptography;

if (Args.Count == 0 || string.IsNullOrWhiteSpace(Args[0]))
{
    Console.Error.WriteLine("Usage: dotnet script tools/GenerateHashTxt.csx -- /path/to/GameAssembly.dll");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Generates loader/interop/assembly-hash.txt from the installed GameAssembly.dll.");
    Console.Error.WriteLine("Algorithm: SHA-256 of (8-byte LE file length) + (first 64KB of file).");
    Environment.Exit(1);
}

var gameAssemblyPath = Args[0];

if (!File.Exists(gameAssemblyPath))
{
    Console.Error.WriteLine($"Error: File not found: {gameAssemblyPath}");
    Environment.Exit(1);
}

// same algorithm as InteropManager.ComputeSHA256() — keep in sync
string ComputeSHA256(string filePath)
{
    using var sha = SHA256.Create();
    var info = new FileInfo(filePath);
    sha.TransformBlock(BitConverter.GetBytes(info.Length), 0, 8, null, 0);
    using var fs = File.OpenRead(filePath);
    var buf = new byte[65536];
    int read = fs.Read(buf, 0, buf.Length);
    sha.TransformFinalBlock(buf, 0, read);
    return Convert.ToHexString(sha.Hash!);
}

var hash = ComputeSHA256(gameAssemblyPath);

var outputPath = Path.GetFullPath(Path.Combine("loader", "interop", "assembly-hash.txt"));

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, hash);

Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Written to: {outputPath}");
