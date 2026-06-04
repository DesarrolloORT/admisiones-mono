using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using WebApiAdmisiones.Observability;

namespace UnitTesting.Observability;

public class ClientTelemetryHeadersTests
{
    [Fact]
    public void ClientRouteMetricLabel_StripsQueryStringAndHash()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ClientTelemetryHeaders.ClientRoute] =
            "/crear-password?token=secret#step";

        var result = ClientTelemetryHeaders.ClientRouteMetricLabel(context);

        Assert.Equal("/crear-password", result);
    }

    [Fact]
    public void ClientDeviceMetricLabel_UsesDeviceTypeOnly()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ClientTelemetryHeaders.ClientDevice] = "desktop; Win32; Chrome";

        var result = ClientTelemetryHeaders.ClientDeviceMetricLabel(context);

        Assert.Equal("desktop", result);
    }

    [Fact]
    public void EnrichActivity_AddsFrontendTags()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ClientTelemetryHeaders.ClientRoute] =
            "/crear-password?token=secret";
        context.Request.Headers[ClientTelemetryHeaders.ClientRouteHistory] =
            "/login > /crear-password?token=secret";
        context.Request.Headers[ClientTelemetryHeaders.ClientService] = "admisiones-frontend";
        context.Request.Headers[ClientTelemetryHeaders.ClientEnvironment] = "development";

        using var activity = new Activity("test").Start();

        ClientTelemetryHeaders.EnrichActivity(activity, context.Request);

        Assert.Equal("/crear-password", activity.GetTagItem("client.route"));
        Assert.Equal("/login > /crear-password", activity.GetTagItem("client.route_history"));
        Assert.Equal("admisiones-frontend", activity.GetTagItem("client.service"));
        Assert.Equal("development", activity.GetTagItem("client.environment"));
    }
}
