using System;
using System.Collections.Generic;
using System.Reflection;

namespace BenchmarkDotNet.Loggers
{
    public sealed class LinqPadLogger : ILogger
    {
        private const string DefaultColor = "";

        // The Util.WithStyle method from LINQPad
        private readonly MethodInfo withStyle;

        private readonly IReadOnlyDictionary<LogKind, string> colorScheme;

        public static readonly Lazy<LinqPadLogger> lazyInstance = new Lazy<LinqPadLogger>(() =>
        {
            // Detect if being run from LINQPad; see https://github.com/dotnet/BenchmarkDotNet/issues/445#issuecomment-300723741
            MethodInfo withStyle = null;
            if (AppDomain.CurrentDomain.FriendlyName.StartsWith("LINQPad Query Server", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Use reflection to avoid taking a dependency on the LINQPad assembly
                    var util = Type.GetType("LINQPad.Util, LINQPad, Version=1.0.0.0, Culture=neutral, PublicKeyToken=21353812cd2a2db5", throwOnError: true);
                    withStyle = util.GetMethod("WithStyle", BindingFlags.Static | BindingFlags.Public);
                }
                catch (Exception)
                {
                }
            }

            if (withStyle != null)
            {
                var isDarkTheme = (bool) (withStyle.DeclaringType.GetProperty("IsDarkThemeEnabled", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) ?? false);
                var colorScheme = isDarkTheme ? CreateDarkScheme() : CreateLightScheme();
                return new LinqPadLogger(withStyle, colorScheme);
            }

            return null;
        });

        public static bool IsAvailable => Instance != null;

        public static ILogger Instance => lazyInstance.Value;

        private LinqPadLogger(MethodInfo withStyle, IReadOnlyDictionary<LogKind, string> colorScheme)
        {
            this.withStyle = withStyle;
            this.colorScheme = colorScheme;
        }

        public void Write(LogKind logKind, string text) => Write(logKind, Console.Write, text);

        public void WriteLine() => Console.WriteLine();

        public void WriteLine(LogKind logKind, string text) => Write(logKind, Console.WriteLine, text);

        private void Write(LogKind logKind, Action<object> write, string text) =>
            write(WithStyle(text, "color:" + GetColor(logKind)));

        private object WithStyle(object data, string htmlStyle) =>
            withStyle.Invoke(null, new[] { data, htmlStyle });

        private string GetColor(LogKind logKind) =>
            colorScheme.TryGetValue(logKind, out var color) ? color : DefaultColor;

        // Converted from ConsoleLogger.CreateColorfulScheme using https://stackoverflow.com/a/28211539
        private static IReadOnlyDictionary<LogKind, string> CreateDarkScheme() =>
            new Dictionary<LogKind, string>
            {
                { LogKind.Default, "#C0C0C0" },
                { LogKind.Help, "#008000" },
                { LogKind.Header, "#FF00FF" },
                { LogKind.Result, "#008080" },
                { LogKind.Statistic, "#00FFFF" },
                { LogKind.Info, "#808000" },
                { LogKind.Error, "#FF0000" },
                { LogKind.Hint, "#008080" }
            };

        private static IReadOnlyDictionary<LogKind, string> CreateLightScheme() =>
            new Dictionary<LogKind, string>
            {
                { LogKind.Default, "#404040" },
                { LogKind.Help, "#008000" },
                { LogKind.Header, "#800080" },
                { LogKind.Result, "#004040" },
                { LogKind.Statistic, "#008080" },
                { LogKind.Info, "#808000" },
                { LogKind.Error, "#FF0000" },
                { LogKind.Hint, "#008080" }
            };
    }
}