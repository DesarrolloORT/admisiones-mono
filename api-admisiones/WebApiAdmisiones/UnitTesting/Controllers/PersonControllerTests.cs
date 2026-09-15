using AppLogic.Contracts.Dtos;
using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using AppLogic.Scholarships.Dtos;
using AppLogic.People.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;
using AppLogic.People.Contracts;
using WebApiAdmisiones.Models;

namespace UnitTesting.Controllers
{
    public class PersonaControllerTests
    {
        private readonly Mock<IGetPersonDetails> _detailsMock;
        private readonly Mock<IUpdatePersonDetails> _updateMock;
        private readonly Mock<IValidatePhoneNumber> _phoneMock;
        private readonly Mock<IGetMyEnrollments> _enrollmentsMock;
        private readonly Mock<IChangePassword> _passwordMock;
        private readonly Mock<IGetPersonPhoto> _photoMock;
        private readonly Mock<IUploadPersonPhoto> _uploadPhotoMock;
        private readonly Mock<IGetPersonIdentityDocument> _documentMock;
        private readonly Mock<IUploadPersonIdentityDocument> _uploadDocumentMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly PersonController _controller;

        public PersonaControllerTests()
        {
            _detailsMock = new Mock<IGetPersonDetails>();
            _updateMock = new Mock<IUpdatePersonDetails>();
            _phoneMock = new Mock<IValidatePhoneNumber>();
            _enrollmentsMock = new Mock<IGetMyEnrollments>();
            _passwordMock = new Mock<IChangePassword>();
            _photoMock = new Mock<IGetPersonPhoto>();
            _uploadPhotoMock = new Mock<IUploadPersonPhoto>();
            _documentMock = new Mock<IGetPersonIdentityDocument>();
            _uploadDocumentMock = new Mock<IUploadPersonIdentityDocument>();
            _currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonController>>();

            _controller = new PersonController(
                _detailsMock.Object,
                _updateMock.Object,
                _phoneMock.Object,
                _enrollmentsMock.Object,
                _passwordMock.Object,
                _photoMock.Object,
                _uploadPhotoMock.Object,
                _documentMock.Object,
                _uploadDocumentMock.Object,
                loggerMock.Object,
                _currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public void ValidatePhoneNumber_IsAnonymousAndRateLimited()
        {
            var method = typeof(PersonController).GetMethod(nameof(PersonController.ValidatePhoneNumber));

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
                attribute => attribute is AllowAnonymousAttribute);
            Assert.Contains(
                method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true),
                attribute => attribute is EnableRateLimitingAttribute rateLimit &&
                             rateLimit.PolicyName == "PhoneValidation");
        }

        [Fact]
        public void ObtenerDatosPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _detailsMock
                .Setup(s => s.Execute(123))
                .Returns(OperationResult<PersonDetailsResponse>.Ok(new PersonDetailsResponse(), "PersonUseCase"));

            var response = _controller.GetPersonDetails();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _detailsMock.Verify(s => s.Execute(123), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            var request = new UpdatePersonDetailsRequest
            {
                CountryId = 1,
                StateId = 2,
                CityId = 3,
                Address = "18 de julio 1234",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099333222", Iso2 = "UY" },
                Email = "ana@test.com",
                EmailConfirmation = "ana@test.com"
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _updateMock
                .Setup(s => s.Execute(123, request))
                .Returns(OperationResult<bool>.Ok(true, "PersonUseCase"));

            var response = _controller.UpdatePersonDetails(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _updateMock.Verify(s => s.Execute(123, request), Times.Once);
        }

        [Fact]
        public void ObtenerMisInscripciones_UsesAuthenticatedUserAndReturnsOk()
        {
            var enrollments = new List<MyEnrollmentsResponse>
            {
                new() { ProductId = 10 }
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _enrollmentsMock
                .Setup(s => s.Execute(123))
                .Returns(OperationResult<IEnumerable<MyEnrollmentsResponse>>.Ok(
                    enrollments,
                    "PersonUseCase"));

            var response = _controller.GetMyEnrollments();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _enrollmentsMock.Verify(s => s.Execute(123), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_UsesAuthenticatedUserAndReturnsOk()
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "NuevaPassword1!"
            };
            var result = OperationResult<object>.Ok(
                "Se actualizó tu contraseña",
                "PersonUseCase");

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _passwordMock
                .Setup(s => s.ExecuteAsync(123, request))
                .ReturnsAsync(result);

            var response = await _controller.ChangePassword(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _passwordMock.Verify(s => s.ExecuteAsync(123, request), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_WithoutAuthenticatedUser_ThrowsUnauthorizedAccessException()
        {
            // GetUserId() lanza cuando el token no trae el claim de usuario; el middleware
            // global (ExceptionHandlingMiddleware) es quien la convierte en 401 (CTL-02).
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "NuevaPassword1!"
            };
            _currentUserMock.Setup(c => c.GetUserId()).Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.ChangePassword(request));

            _passwordMock.Verify(
                s => s.ExecuteAsync(It.IsAny<long>(), It.IsAny<ChangePasswordRequest>()),
                Times.Never);
        }

        [Fact]
        public void ObtenerFotoPersona_WhenServiceSucceeds_ReturnsFileContentResult()
        {
            var bytes = new byte[] { 1, 2, 3 };
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _photoMock
                .Setup(s => s.Execute(123))
                .Returns(OperationResult<byte[]>.Ok(bytes, "PersonUseCase"));

            var response = _controller.GetPersonPhoto();

            var fileResult = Assert.IsType<FileContentResult>(response);
            Assert.Equal("image/jpeg", fileResult.ContentType);
            Assert.Equal(bytes, fileResult.FileContents);
        }

        [Fact]
        public void ObtenerFotoPersona_WhenServiceFails_ReturnsOperationResultBody()
        {
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _photoMock
                .Setup(s => s.Execute(123))
                .Returns(OperationResult<byte[]>.IsFailed(
                    "GEN_FA_01",
                    "PersonUseCase",
                    "Foto no encontrada.",
                    404));

            var response = _controller.GetPersonPhoto();

            var notFoundResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, notFoundResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<byte[]>>(notFoundResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("GEN_FA_01", operationResult.ErrorCode);
        }

        [Fact]
        public void ObtenerFotoPersona_WhenServiceSucceedsWithNullData_ReturnsOperationResultBody()
        {
            // ⚠️ CONTRATO: antes este caso devolvía 404 sin body (NotFoundResult); ahora cumple
            // el ProducesResponseType(typeof(OperationResult<byte[]>), 404) declarado en el endpoint.
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _photoMock
                .Setup(s => s.Execute(123))
                .Returns(OperationResult<byte[]>.Ok(null, "PersonUseCase"));

            var response = _controller.GetPersonPhoto();

            var notFoundResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, notFoundResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<byte[]>>(notFoundResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("GEN_FA_03", operationResult.ErrorCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            var dueDate = DateTime.Today.AddYears(1);
            var document = new PersonIdentityDocumentResponse
            {
                Front = new IdentityDocumentFile
                {
                    FileName = "123_1.pdf",
                    Content = new byte[] { 1, 2, 3 }
                },
                ExpirationDate = dueDate
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _documentMock
                .Setup(s => s.Execute(123))
                .Returns(OperationResult<PersonIdentityDocumentResponse>.Ok(
                    document,
                    "PersonUseCase"));

            var response = _controller.GetPersonIdentityDocument();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<PersonIdentityDocumentResponse>>(okResult.Value);
            Assert.Equal(dueDate, operationResult.Data!.ExpirationDate);
            _documentMock.Verify(s => s.Execute(123), Times.Once);
        }

        [Fact]
        public void SubirDocumentoPersona_MapsFrenteDorsoAndFecha()
        {
            var fecha = DateTime.Today.AddYears(1);
            var request = new UploadPersonIdentityDocumentRequest
            {
                ExpirationDate = fecha,
                Front = new FilePayload
                {
                    FileName = "frente.pdf",
                    Content = new byte[] { 1, 2, 3 }
                },
                Back = new FilePayload
                {
                    FileName = "dorso.pdf",
                    Content = new byte[] { 4, 5, 6 }
                }
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _uploadDocumentMock
                .Setup(s => s.Execute(
                    123,
                    fecha,
                    It.Is<IdentityDocumentFile>(d =>
                        d.FileName == "frente.pdf" &&
                        d.Content!.SequenceEqual(new byte[] { 1, 2, 3 })),
                    It.Is<IdentityDocumentFile>(d =>
                        d.FileName == "dorso.pdf" &&
                        d.Content!.SequenceEqual(new byte[] { 4, 5, 6 }))))
                .Returns(OperationResult<bool>.Ok(true, "PersonUseCase"));

            var response = _controller.UploadPersonIdentityDocument(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _detailsMock.VerifyAll();
            _updateMock.VerifyAll();
            _photoMock.VerifyAll();
            _uploadPhotoMock.VerifyAll();
            _documentMock.VerifyAll();
            _uploadDocumentMock.VerifyAll();
        }
    }
}
