using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security.Authentication;
using Xunit;

namespace UnitTesting.Controllers
{
    public class InscripcionesControllerTests
    {
        [Fact]
        public async Task ConfirmarPreInscripcion_DelegatesToServiceWithAuthenticatedUser()
        {
            var interestMock = new Mock<IRegisterProductInterest>();
            var confirmMock = new Mock<IConfirmPreEnrollment>();
            var reactivateMock = new Mock<IReactivateEnrollment>();
            var detailsMock = new Mock<IGetEnrollmentDetails>();
            var regulationsMock = new Mock<IGetStudentRegulationsAcceptance>();
            var pagosMock = new Mock<IStartEnrollmentPayment>();
            var encuestaMock = new Mock<IInitialSurveyService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<EnrollmentsController>>();
            var request = new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            };
            var responseDto = new ConfirmPreEnrollmentResponse
            {
                Confirmed = true,
                Summary = new EnrollmentHeader { PaymentDueDate = new DateTime(2026, 6, 30) },
                Enrollments =
                [
                    new EnrollmentOffering
                    {
                        OfferingId = 10,
                        EnrollmentId = 100
                    }
                ],
                DepositAmount = 1500
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            confirmMock
                .Setup(s => s.ExecuteAsync(1, request))
                .ReturnsAsync(OperationResult<ConfirmPreEnrollmentResponse>.Ok(responseDto, nameof(IConfirmPreEnrollment.ExecuteAsync)));

            var controller = new EnrollmentsController(interestMock.Object, confirmMock.Object, reactivateMock.Object, detailsMock.Object, regulationsMock.Object, pagosMock.Object, encuestaMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.ConfirmPreEnrollment(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            confirmMock.Verify(s => s.ExecuteAsync(1, request), Times.Once);
        }

        [Fact]
        public async Task ReactivarInscripcion_DelegatesToServiceWithAuthenticatedUser()
        {
            var interestMock = new Mock<IRegisterProductInterest>();
            var confirmMock = new Mock<IConfirmPreEnrollment>();
            var reactivateMock = new Mock<IReactivateEnrollment>();
            var detailsMock = new Mock<IGetEnrollmentDetails>();
            var regulationsMock = new Mock<IGetStudentRegulationsAcceptance>();
            var pagosMock = new Mock<IStartEnrollmentPayment>();
            var encuestaMock = new Mock<IInitialSurveyService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<EnrollmentsController>>();
            var request = new ReactivateEnrollmentRequest { EnrollmentId = 555 };
            var responseDto = new ConfirmPreEnrollmentResponse
            {
                Confirmed = true,
                Enrollments =
                [
                    new EnrollmentOffering { OfferingId = 10, EnrollmentId = 100 }
                ],
                DepositAmount = 1500
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            reactivateMock
                .Setup(s => s.ExecuteAsync(1, request))
                .ReturnsAsync(OperationResult<ConfirmPreEnrollmentResponse>.Ok(responseDto, nameof(IReactivateEnrollment.ExecuteAsync)));

            var controller = new EnrollmentsController(interestMock.Object, confirmMock.Object, reactivateMock.Object, detailsMock.Object, regulationsMock.Object, pagosMock.Object, encuestaMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.ReactivateEnrollment(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            reactivateMock.Verify(s => s.ExecuteAsync(1, request), Times.Once);
        }

        [Fact]
        public async Task Pagar_DelegatesToServiceWithAuthenticatedUser()
        {
            var interestMock = new Mock<IRegisterProductInterest>();
            var confirmMock = new Mock<IConfirmPreEnrollment>();
            var reactivateMock = new Mock<IReactivateEnrollment>();
            var detailsMock = new Mock<IGetEnrollmentDetails>();
            var regulationsMock = new Mock<IGetStudentRegulationsAcceptance>();
            var pagosMock = new Mock<IStartEnrollmentPayment>();
            var encuestaMock = new Mock<IInitialSurveyService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<EnrollmentsController>>();
            var request = new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "BANRED" };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            pagosMock
                .Setup(s => s.ExecuteAsync(1, request))
                .ReturnsAsync(OperationResult<StartPaymentResponse>.Ok(
                    new StartPaymentResponse { Result = "URL_GENERADA", PaymentUrl = "https://pagos.test" },
                    nameof(IStartEnrollmentPayment.ExecuteAsync)));
            var controller = new EnrollmentsController(interestMock.Object, confirmMock.Object, reactivateMock.Object, detailsMock.Object, regulationsMock.Object, pagosMock.Object, encuestaMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.StartPayment(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            pagosMock.Verify(s => s.ExecuteAsync(1, request), Times.Once);
        }

        [Fact]
        public void ReglamentoEstudiantil_PostEndpoint_IsNotExposed()
        {
            var postRoutes = typeof(EnrollmentsController)
                .GetMethods()
                .SelectMany(method => method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Cast<HttpPostAttribute>())
                .Select(attribute => attribute.Template);

            Assert.DoesNotContain("ReglamentoEstudiantil", postRoutes);
        }

        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_DelegatesToServiceWithAuthenticatedUser()
        {
            var interestMock = new Mock<IRegisterProductInterest>();
            var confirmMock = new Mock<IConfirmPreEnrollment>();
            var reactivateMock = new Mock<IReactivateEnrollment>();
            var detailsMock = new Mock<IGetEnrollmentDetails>();
            var regulationsMock = new Mock<IGetStudentRegulationsAcceptance>();
            var pagosMock = new Mock<IStartEnrollmentPayment>();
            var encuestaMock = new Mock<IInitialSurveyService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<EnrollmentsController>>();
            var responseDto = new StudentRegulationsAcceptanceResponse
            {
                AcceptedStudentRegulations = true,
                AcceptanceDate = new DateTime(2026, 6, 1)
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            regulationsMock
                .Setup(s => s.Execute(1))
                .Returns(OperationResult<StudentRegulationsAcceptanceResponse>.Ok(
                    responseDto,
                    nameof(IGetStudentRegulationsAcceptance.Execute)));
            var controller = new EnrollmentsController(interestMock.Object, confirmMock.Object, reactivateMock.Object, detailsMock.Object, regulationsMock.Object, pagosMock.Object, encuestaMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetStudentRegulationsAcceptance();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            regulationsMock.Verify(s => s.Execute(1), Times.Once);
        }

    }
}
