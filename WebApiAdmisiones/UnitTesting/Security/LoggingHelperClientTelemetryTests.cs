using Microsoft.AspNetCore.Http;
using WebApiAdmisiones.Security.Observability;
using WebApiAdmisiones.Security.Observability;

namespace UnitTesting.Security;

public class LoggingHelperClientTelemetryTests
{
    [Fact]
    public void FormatEntrada_WithClientTelemetryHeaders_IncludesFrontendFields()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/auth/login";
        context.Request.Headers[ClientTelemetryHeaders.ClientService] = "admisiones-frontend";
        context.Request.Headers[ClientTelemetryHeaders.ClientEnvironment] = "development";
        context.Request.Headers[ClientTelemetryHeaders.ClientVersion] = "1.2.3";
        context.Request.Headers[ClientTelemetryHeaders.ClientRoute] = "/login?token=secret";
        context.Request.Headers[ClientTelemetryHeaders.ClientRouteHistory] = "/inicio > /login?token=secret";
        context.Request.Headers[ClientTelemetryHeaders.ClientDevice] = "desktop; Win32; Firefox";
        context.Request.Headers[ClientTelemetryHeaders.TestRunId] = "manual-telemetry-check";

        var result = LoggingHelper.FormatEntrada(context, "TestFilter");

        Assert.Contains("ClientService: admisiones-frontend", result);
        Assert.Contains("ClientEnv: development", result);
        Assert.Contains("ClientVersion: 1.2.3", result);
        Assert.Contains("ClientRoute: /login", result);
        Assert.Contains("ClientRouteHistory: /inicio > /login", result);
        Assert.Contains("ClientDevice: desktop; Win32; Firefox", result);
        Assert.Contains("TestRunId: manual-telemetry-check", result);
        Assert.DoesNotContain("token=secret", result);
    }

    [Fact]
    public void FormatEntrada_WithoutClientTelemetryHeaders_DoesNotIncludeFrontendFields()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/auth/login";

        var result = LoggingHelper.FormatEntrada(context, "TestFilter");

        Assert.DoesNotContain("ClientService:", result);
        Assert.DoesNotContain("ClientRoute:", result);
        Assert.DoesNotContain("TestRunId:", result);
    }
}
