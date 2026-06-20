using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DevContextMcp.UnitTests")]
[assembly: InternalsVisibleTo("DevContextMcp.IntegrationTests")]

// Allows Moq (Castle DynamicProxy) to mock internal interfaces such as IAnalyticsRecorder.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
