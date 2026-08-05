using AppLogic.Registration.Dtos;
using AzureService.DTOs;

namespace AppLogic.Registration.Mapping;

/// <summary>
/// Límite anticorrupción con <c>Core/AzureService</c>: sus DTOs están en español y no se pueden
/// renombrar (submódulo compartido), así que se traducen acá antes de salir al front.
/// </summary>
public static class DocumentRecognitionMapper
{
    public static DocumentRecognitionResponse ToResponse(this ReconocimientoDocumentoResponse source) => new()
    {
        RequiresReview = source.RequiereRevision,
        Fields = ToFields(source.Campos),
        PersonFace = ToFace(source.CaraPersona),
        Warnings = source.Advertencias
    };

    private static RecognizedDocumentFields ToFields(CamposDocumentoReconocidoDto campos) => new()
    {
        DocumentType = campos.TipoDocumento,
        DocumentNumber = campos.NumeroDocumento,
        FirstName = campos.PrimerNombre,
        MiddleName = campos.SegundoNombre,
        FirstSurname = campos.PrimerApellido,
        SecondSurname = campos.SegundoApellido,
        BirthDate = campos.FechaNacimiento,
        BirthPlace = campos.LugarNacimiento,
        State = campos.Departamento,
        Sex = campos.Sexo,
        ExpirationDate = campos.FechaVencimiento,
        Nationality = campos.Nacionalidad
    };

    private static RecognizedFace? ToFace(ArchivoDescargaDto? cara) => cara is null
        ? null
        : new RecognizedFace
        {
            Content = cara.Archivo,
            FileName = cara.NombreArchivo,
            ContentType = cara.ContentType
        };
}
