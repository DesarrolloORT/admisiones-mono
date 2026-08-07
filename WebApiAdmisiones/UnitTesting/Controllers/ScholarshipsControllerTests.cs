using AppLogic.Scholarships.Dtos;
using AppLogic.Scholarships.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;

namespace UnitTesting.Controllers
{
    public class BecasControllerTests
    {
        [Fact]
        public void ObtenerMisInscripcionesConfirmadas_UsesAuthenticatedUserAndReturnsOk()
        {
            var serviceMock = new Mock<IScholarshipService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<ScholarshipsController>>();
            var controller = new ScholarshipsController(serviceMock.Object, loggerMock.Object, currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            serviceMock
                .Setup(s => s.GetMyConfirmedEnrollments(123))
                .Returns(OperationResult<IEnumerable<ConfirmedEnrollmentResponse>>.Ok(
                    [new ConfirmedEnrollmentResponse { EnrollmentStatus = "Confirmada" }],
                    nameof(IScholarshipService.GetMyConfirmedEnrollments)));

            var response = controller.GetMyConfirmedEnrollments();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.GetMyConfirmedEnrollments(123), Times.Once);
        }

    }
}
