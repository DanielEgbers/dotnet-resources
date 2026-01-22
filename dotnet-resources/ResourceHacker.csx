#!/usr/bin/env dotnet

#:property ManagePackageVersionsCentrally=false

#:package Flurl.Http@4.0.2

using Flurl.Http;
using System.IO.Compression;
using System.Runtime.CompilerServices;

const string DownloadUrl = "http://www.angusj.com/resourcehacker/resource_hacker.zip";

var targetFileInfo = new FileInfo(Path.ChangeExtension(GetScriptPath(), "exe"));
using var zipStream = await DownloadUrl.GetStreamAsync();
using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
var fileEntry = zipArchive.GetEntry(targetFileInfo.Name) ?? throw new FileNotFoundException($"{targetFileInfo.Name} not found in the archive");
using var fileEntryStream = fileEntry.Open();
using var fileStream = File.Create(targetFileInfo.FullName);
await fileEntryStream.CopyToAsync(fileStream);

string GetScriptPath([CallerFilePath] string path = null!) => path;