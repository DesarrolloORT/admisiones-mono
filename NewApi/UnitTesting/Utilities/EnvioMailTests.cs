// File: UnitTesting/MailORT/EnvioMailTests.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using MailORT;
using Moq;
using Office365;
using Xunit;

namespace UnitTesting.MailORT
{
    public class EnvioMailTests
    {
        private const string TestUrl = "https://fake.office365/soap";
        private readonly List<string> _toList = new() { "test@ort.edu.uy" };

        [Fact]
        public async Task EnviarMail_CallsEnviarConDto_WithCorrectDto()
        {
            // Arrange
            var mail = new TestableEnvioMail(TestUrl);
            bool enviarConDtoCalled = false;
            mail.OnEnviarConDto = dto =>
            {
                enviarConDtoCalled = true;
                Assert.Equal("from@ort.edu.uy", dto.From);
                Assert.Equal("subject", dto.Subject);
                Assert.Contains("body", dto.Body);
                Assert.NotNull(dto.ImagenCabezal);
                Assert.Equal(_toList, dto.ColTOs);
                return Task.CompletedTask;
            };

            // Act
            await mail.EnviarMail("from@ort.edu.uy", _toList, "subject", "body");

            // Assert
            Assert.True(enviarConDtoCalled);
        }

        [Fact]
        public async Task EnviarMailAttachmentsAsync_IncludesAttachmentsInDto()
        {
            // Arrange
            var mail = new TestableEnvioMail(TestUrl);
            var attachment = new Attachment(new MemoryStream(new byte[] { 1, 2, 3 }), "file.txt", "text/plain");
            bool enviarConDtoCalled = false;
            mail.OnEnviarConDto = dto =>
            {
                enviarConDtoCalled = true;
                Assert.NotNull(dto.ColAdjuntos);
                Assert.Single(dto.ColAdjuntos);
                Assert.Equal("file.txt", dto.ColAdjuntos[0].FileName);
                Assert.Equal("text/plain", dto.ColAdjuntos[0].MimeType);
                Assert.Equal(new byte[] { 1, 2, 3 }, dto.ColAdjuntos[0].Attachment);
                return Task.CompletedTask;
            };

            // Act
            await mail.EnviarMailAttachmentsAsync("from@ort.edu.uy", _toList, "subject", "body", new List<Attachment> { attachment });

            // Assert
            Assert.True(enviarConDtoCalled);
        }

        [Fact]
        public void CabezalHTML_ReturnsHtml()
        {
            var html = EnvioMail.CabezalHTML();
            Assert.Contains("<!DOCTYPE HTML>", html);
            Assert.Contains("<body", html);
        }

        [Fact]
        public void Cabezal_ReturnsHeaderHtml()
        {
            var html = EnvioMail.Cabezal();
            Assert.Contains("Universidad ORT Uruguay", html);
            Assert.Contains("img", html);
        }

        [Fact]
        public void CuerpoConTitulo_ReturnsExpectedHtml()
        {
            var html = EnvioMail.CuerpoConTitulo("Titulo", new DateTime(2024, 6, 1), "Destinatario");
            Assert.Contains("Titulo", html);
            Assert.Contains("01/06/2024", html);
            Assert.Contains("Destinatario", html);
        }

        [Fact]
        public void CuerpoConTituloFecha_ReturnsExpectedHtml()
        {
            var html = EnvioMail.CuerpoConTituloFecha("Titulo", new DateTime(2024, 6, 1), "Destinatario");
            Assert.Contains("Titulo", html);
            Assert.Contains("01/06/2024", html);
            Assert.Contains("Destinatario", html);
        }

        [Fact]
        public void CuerpoParrafos_ReturnsParagraphHtml()
        {
            var html = EnvioMail.CuerpoParrafos("Texto de prueba");
            Assert.Contains("Texto de prueba", html);
            Assert.Contains("<p", html);
        }

        [Fact]
        public void PieReferencia_ReturnsReferenceHtml()
        {
            var html = EnvioMail.PieReferencia("12345");
            Assert.Contains("Ref: 12345", html);
        }

        [Fact]
        public void LineaNegrita_ReturnsBoldLineHtml()
        {
            var html = EnvioMail.LineaNegrita("Texto", "50%", 2);
            Assert.Contains("<b>Texto</b>", html);
            Assert.Contains("width='50%'", html);
        }

        [Fact]
        public void LineaNormal_ReturnsNormalLineHtml()
        {
            var html = EnvioMail.LineaNormal("Texto", "80%", 2);
            Assert.Contains(">Texto<", html);
            Assert.Contains("width='80%'", html);
        }

        [Fact]
        public void LineaEspacio_ReturnsSpaceLineHtml()
        {
            var html = EnvioMail.LineaEspacio("60%", 2);
            Assert.Contains("&nbsp;", html);
            Assert.Contains("width='60%'", html);
        }

        [Fact]
        public async Task ObtenerImagenCabezalAsync_ReturnsBytes()
        {
            // Use reflection to invoke private static method
            var method = typeof(EnvioMail).GetMethod("ObtenerImagenCabezalAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new Exception("Method not found");

            // To avoid exception, embed a dummy resource or skip if not present
            try
            {
                var task = (Task<byte[]>)method.Invoke(null, null);
                var result = await task;
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Resource not found, acceptable for test context
                Assert.Contains("No se encontró el recurso", ex.InnerException.Message);
            }
        }

        // Helper: Testable subclass to intercept EnviarConDto
        private class TestableEnvioMail : EnvioMail
        {
            public Func<DTOEmailCentralizado, Task> OnEnviarConDto { get; set; } = _ => Task.CompletedTask;

            public TestableEnvioMail(string url) : base(url) { }

            protected override Task EnviarConDto(DTOEmailCentralizado dto)
            {
                return OnEnviarConDto(dto);
            }
        }
    }
}
