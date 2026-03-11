using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace AppLogic.Helpers
{
    [ExcludeFromCodeCoverage]
    public class DatosPersonalesRequest
    {
        public DtoDatoBasico? Basicos { get; set; }
        public DtoDocumento? Documento { get; set; }
        public DtoDatoDireccion? Direccion { get; set; }
        public DtoDatoContacto? Contacto { get; set; }
        public List<DtoImagen>? Imagens { get; set; }

        public DatosPersonalesRequest()
        {
        }

        public DatosPersonalesRequest(Persona persona)
        {
            ArgumentNullException.ThrowIfNull(persona);

            // DtoDatoBasico
            Basicos = new DtoDatoBasico
            {
                CodigoPersona = persona.CodigoPersona,
                PrimerApellido = persona.PrimerApellido?.Trim(),
                SegundoApellido = persona.SegundoApellido?.Trim(),
                PrimerNombre = persona.PrimerNombre?.Trim(),
                SegundoNombre = persona.SegundoNombre?.Trim(),
                Sexo = persona.Sexo?.Trim(),
                FechaNacimiento = persona.FechaNacimiento,
                NombrePaisNacimiento = persona.Pai?.Nombre?.Trim(),
                CodigoPaisNacimiento = persona.CodigoPaisNacimiento,
                NacionalidadPersona = persona.NacionalidadPersona,
                CredencialCivica = persona.CredencialCivica?.Trim(),
                EstadoCivil = persona.EstadoCivil
            };

            // DtoDocumento
            Documento = new DtoDocumento
            {
                TipoDocumento = persona.TipoDocumento,
                Documento = persona.Documento?.Trim(),
                FechaVtoDocumento = persona.FechaVtoDocumentoPersona
            };

            // DtoDatoDireccion
            Direccion = new DtoDatoDireccion
            {
                CodigoPais = persona.CodigoPais,
                NombrePais = persona.Ciudad.Estado.Pai?.Nombre?.Trim(),
                CodigoEstado = persona.CodigoEstado,
                NombreEstado = persona.Ciudad.Estado.Nombre?.Trim(),
                CodigoCiudad = persona.CodigoCiudad,
                NombreCiudad = persona.Ciudad.Nombre?.Trim(),
                DomicilioActual = persona.Direccion?.Trim(),
                CodigoPostal = persona.CodigoPostal?.Trim()
            };

            // DtoDatoContacto
            var tel1 = PhoneVerification.IdentificarTelefono(persona.Telefono1 ?? string.Empty, persona.CaracteristicaPai_IdCaracteristicaPaisTel1.Iso2, true);
            var tel2 = PhoneVerification.IdentificarTelefono(persona.Telefono2 ?? string.Empty, persona.CaracteristicaPai_IdCaracteristicaPaisTel2.Iso2, false);

            Contacto = new DtoDatoContacto
            {
                Telefono1 = new DtoTelefono
                {
                    TelefonoE164 = persona.Telefono1?.Trim(),
                    TelefonoValido = tel1.TelefonoValido,
                    CaracteristicaPais = persona.CaracteristicaPai_IdCaracteristicaPaisTel1.IdCaracteristicaPais,
                    TelefonoSimple = tel1.TelefonoSimple,
                    Iso2 = persona.CaracteristicaPai_IdCaracteristicaPaisTel1.Iso2,
                },
                Telefono2 = new DtoTelefono
                {
                    TelefonoE164 = persona.Telefono2?.Trim(),
                    TelefonoValido = tel2.TelefonoValido,
                    CaracteristicaPais = persona.CaracteristicaPai_IdCaracteristicaPaisTel2.IdCaracteristicaPais,
                    TelefonoSimple = tel2.TelefonoSimple,
                    Iso2 = persona.CaracteristicaPai_IdCaracteristicaPaisTel2.Iso2,
                },
                Email = persona.Email?.Trim(),
                Contacto1Persona = persona.Contacto1Persona?.Trim(),
                Contacto2Persona = persona.Contacto2Persona?.Trim(),
                LinkedinPersona = persona.LinkedinPersona?.Trim()
            };

        }
    }

    [ExcludeFromCodeCoverage]
    public class DtoDatoBasico
    {
        [ReadOnly(true)]
        public long CodigoPersona { get; set; }
        [ReadOnly(true)]
        public string? PrimerApellido { get; set; }
        [ReadOnly(true)]
        public string? SegundoApellido { get; set; }
        [ReadOnly(true)]
        public string? PrimerNombre { get; set; }
        [ReadOnly(true)]
        public string? SegundoNombre { get; set; }
        [ReadOnly(true)]
        [StringLength(1)]
        public string? Sexo { get; set; }
        [ReadOnly(true)]
        public DateTime? FechaNacimiento { get; set; }
        [ReadOnly(true)]
        [StringLength(50)]
        public string? NombrePaisNacimiento { get; set; }
        public long? CodigoPaisNacimiento { get; set; }
        [StringLength(30)]
        public string? NacionalidadPersona { get; set; }
        [StringLength(10)]
        public string? CredencialCivica { get; set; }
        [StringLength(1)]
        public string? EstadoCivil { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class DtoDocumento
    {
        [ReadOnly(true)]
        public string? TipoDocumento { get; set; }
        [ReadOnly(true)]
        [Redact(RedactionMode.PreserveLength)] // ensure attribute enum resolution
        public string? Documento { get; set; }
        public DateTime? FechaVtoDocumento { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class DtoDatoDireccion
    {
        [Required()]
        public long? CodigoPais { get; set; }
        [ReadOnly(true)]
        [StringLength(50)]
        public string? NombrePais { get; set; }
        [Required()]
        public long? CodigoEstado { get; set; }
        [ReadOnly(true)]
        [StringLength(30)]
        public string? NombreEstado { get; set; }
        [Required()]
        public long? CodigoCiudad { get; set; }
        [ReadOnly(true)]
        [StringLength(30)]
        public string? NombreCiudad { get; set; }
        [StringLength(60)]
        [Required()]
        public string? DomicilioActual { get; set; } = null!;
        [StringLength(20)]
        public string? CodigoPostal { get; set; }
    }
   
    [ExcludeFromCodeCoverage]
    public class DtoDatoContacto
    {

        [Required()]
        public DtoTelefono Telefono1 { get; set; } = new DtoTelefono();
        public DtoTelefono Telefono2 { get; set; } = new DtoTelefono();
        [Required()]
        [StringLength(60)]
        public string? Email { get; set; } = null!;
        [StringLength(60)]
        public string? Contacto1Persona { get; set; }

        [StringLength(60)]
        public string? Contacto2Persona { get; set; }

        [StringLength(500)]
        public string? LinkedinPersona { get; set; }
    }
    
    [ExcludeFromCodeCoverage]
    public class DtoTelefono()
    {
        public bool TelefonoValido { get; set; }
        [StringLength(20)]
        public string? TelefonoE164 { get; set; }
        [StringLength(2)]
        public string? Iso2 { get; set; }
        public long CaracteristicaPais { get; set; }
        [StringLength(20)]
        public string? TelefonoSimple { get; set; }
    }
    
    [ExcludeFromCodeCoverage]
    public class DtoImagen
    {
        public long? IdImagen { get; set; }

        [Required()]
        [Redact(RedactionMode.BinaryLength)]
        public byte[]? BlobImagen { get; set; }
    }
}
