using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using BusinessLogic.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace AppLogic.Interfaces
{
    public interface IGeneralServices
    {
        #region CONSULTAS GENERALES
        OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaises();
        OperationResult<DtoPaisDevart> ObtenerPais(long idPais);
        OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos();
        #endregion CONSULTAS GENERALES

        #region INTERES, PRODUCTOS, PROCESOS HABILITADOS
        OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto);
        OperationResult<DTOUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona);
        #endregion INTERES, PRODUCTOS, PROCESOS HABILITADOS

        #region PERSONA
        OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona);
        #endregion PERSONA

        #region ENCUESTA
        OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona);
        OperationResult<IEnumerable<DtoTurnoDevart>> ObtenerTurnos(long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>> ObtenerMotivosEleccion();
        OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>> ObtenerPublicidadesEleccion();
        #endregion ENCUESTA

        #region BACHILLERATOS Y UNIVERSIDADES
        OperationResult<IEnumerable<DtoTituloDevart>> ObtenerBachilleratos(long idAnioBachillerato);
        OperationResult<DtoAnioBachillerDevart> ObtenerAnioBachiller(long idAnioBachillerato);
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado);
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerUniversidades();
        #endregion BACHILLERATOS Y UNIVERSIDADES

        #region POSTULACION A BECAS
        OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto);
        #endregion POSTULACION A BECAS

        #region INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS
        OperationResult<DtoAceptacionReglamentoEstDevart> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona);
        OperationResult<DtoAceptacionReglamentoEstDevart> RegistrarAceptacionReglamentoEstudiantil(long codigoPersona);
        OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona);
        OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona);
        OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesPendientes(long codigoPersona);
        OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesCanceladas(long codigoPersona);
        OperationResult<IEnumerable<DtoOfertaDevart>> ObtenerOfertasParaInscripcionConProceso(long idProducto, long idProceso, long idTurno);
        OperationResult<IEnumerable<DTOInscripcionRealizada>> ObtenerInscripcionesRealizadas(long codigoPersona);
        OperationResult<IEnumerable<DTOProductoBeca>> ObtenerProductosBeca(long codigoPersona);
        OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso);
        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        #region IMAGEN / DOCUMENTOS
        OperationResult<byte[]> ObtenerDocumentoAlumno(long codigoPersona, int tipo);
        OperationResult<byte[]> ObtenerFotoAlumno(long codigoPersona);
        OperationResult<bool> SubirFotoAlumno(long codigoPersona, byte[] fileContent, string fileName);
        OperationResult<bool> SubirDocumentoAlumno(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName);
        #endregion IMAGEN / DOCUMENTOS

        #region ADMISIONES
        OperationResult<DateTime> ObtenerFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
        OperationResult<IEnumerable<DtoPruebaDevart>> ObtenerFondosDeBecaVigentes(long idProducto, long idProceso, long codigoPersona);
        #endregion ADMISIONES
    }
}
