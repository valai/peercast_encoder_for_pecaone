using Microsoft.Win32;

namespace PecaOneRelay;

internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PecaOneConnect";

    public static bool IsEnabled
    {
        get
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return runKey?.GetValue(ValueName) is string;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows のスタートアップ設定を開けません。");

        if (!enabled)
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException("アプリの実行ファイルの場所を取得できません。");

        runKey.SetValue(ValueName, $"\"{executablePath}\"", RegistryValueKind.String);
    }
}
