using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;

namespace VeilView;

internal static class WebView2NativeBootstrap
{
    private const string ResourceName = "WebView2Loader.dll";
    private static bool _registered;
    private static string? _extractedLoaderPath;

    public static void RegisterResolver()
    {
        if (_registered) return;
        _registered = true;

        NativeLibrary.SetDllImportResolver(typeof(CoreWebView2Environment).Assembly, ResolveWebView2NativeLibrary);
    }

    private static IntPtr ResolveWebView2NativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.Contains("WebView2Loader", StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        var path = EnsureLoaderExtracted();
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? EnsureLoaderExtracted()
    {
        if (_extractedLoaderPath is not null && File.Exists(_extractedLoaderPath))
        {
            return _extractedLoaderPath;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            return null;
        }

        var targetDir = Path.Combine(AppSettings.AppDataDirectory, "native", RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());
        Directory.CreateDirectory(targetDir);

        var targetPath = Path.Combine(targetDir, ResourceName);
        using var source = assembly.GetManifestResourceStream(resource);
        if (source is null) return null;

        var shouldWrite = true;
        if (File.Exists(targetPath))
        {
            try { shouldWrite = new FileInfo(targetPath).Length != source.Length; }
            catch { shouldWrite = true; }
        }

        if (shouldWrite)
        {
            using var target = File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            source.CopyTo(target);
        }

        _extractedLoaderPath = targetPath;
        return targetPath;
    }
}
