// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress common warnings for WPF projects
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "WPF event handlers need to be instance methods")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Not needed in UI context")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "XAML bindings provide valid arguments")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This is a German application with hardcoded German text")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "UI applications need robust error handling")]

// Suppress async void warnings for event handlers
[assembly: SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Event handlers may use async void pattern")]
[assembly: SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage", Justification = "WPF async event handlers")]

// Suppress naming convention warnings for WPF controls
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "WPF XAML naming conventions")]
[assembly: SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "WPF collection naming")]

// Suppress file/encoding warnings
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "DateTime formatting for German locale")]
[assembly: SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "String operations in German context")]

// Suppress design warnings for auto-generated code
[assembly: SuppressMessage("Design", "CA1060:Move pinvokes to native methods class", Justification = "WPF interop requirements")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "XAML may call private methods")]

// Additional .NET 8 specific warnings
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows-only WPF application")]
[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Logging with dynamic parameters")]
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Simple logging scenarios")]
[assembly: SuppressMessage("Performance", "CA1851:Possible multiple enumerations of IEnumerable collection", Justification = "Small collections in UI context")]

// XAML-related warnings
[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "WPF controls are managed by framework")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "WPF framework manages object lifetime")]

// String and culture warnings
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "UI display formatting")]
[assembly: SuppressMessage("Globalization", "CA1309:Use ordinal string comparison", Justification = "User-facing string operations")]

// Reflection warnings
[assembly: SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access", Justification = "WPF binding and reflection")]
[assembly: SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality", Justification = "WPF runtime requirements")]

// Nullable reference warnings
[assembly: SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Readability preference")]
[assembly: SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Traditional constructor pattern")]
[assembly: SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "Explicit initialization for clarity")]
