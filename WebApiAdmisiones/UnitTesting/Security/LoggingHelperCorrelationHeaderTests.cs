using Microsoft.AspNetCore.Http;
using WebApiAdmisiones.Observability;
using WebApiAdmisiones.Security.Observability;

namespace UnitTesting.Security;

public class LoggingHelperCorrelationHeaderTests
{
    [Fact]
    public void EnsureCorrelationId_WithIncomingHeader_UsesHeaderValue()
    {
        var context = new DefaultHttpContext();
        var incomingCorrelationId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        context.Request.Headers[ClientTelemetryHeaders.CorrelationId] = incomingCorrelationId.ToString();

        var result = LoggingHelper.EnsureCorrelationId(context);

        Assert.Equal(incomingCorrelationId, result);
        Assert.Equal(incomingCorrelationId, context.Items[LoggingHelper.CorrelationIdKey]);
    }
}
