using AppLogic.DevartDTOs;
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
        OperationResult<IEnumerable<DtoProductoDevart>> ObtenerProductosConInteres(long codigoPersona);
        OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto);
        OperationResult<DtoInscriptoDevart> ObtenerUltimaInscripcion(long codigoPersona);
        OperationResult<DtoInscriptoDevart> ObtenerInscripcionPorProductoProceso(long codigoPersona, long idProducto, long idProceso);
        #endregion INTERES, PRODUCTOS, PROCESOS HABILITADOS

        #region PERSONA
        OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona);
        #endregion PERSONA

        #region ENCUESTA
        OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerDatosPreInscripcion(long codigoPersona);
        OperationResult<DateTime?> ObtenerFechaVtoAdmisiones(long codigoPersona, long idProceso);
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
        OperationResult<IEnumerable<DtoPruebaDevart>> ObtenerFondosDeBecaVigentes(long codigoPersona, long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorNivel(long idProducto);
        #endregion POSTULACION A BECAS

        #region INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS
        OperationResult<DtoAceptacionReglamentoEstDevart> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona);
        OperationResult<IEnumerable<DtoInscriptoDevart>> ObtenerInscripcionesRealizadas(long codigoPersona);
        OperationResult<IEnumerable<DtoInscriptoDevart>> ObtenerInscripcionesPendientes(long codigoPersona);
        OperationResult<IEnumerable<DtoInscriptoDevart>> ObtenerInscripcionesCanceladas(long codigoPersona);
        OperationResult<IEnumerable<DtoProductoDevart>> ObtenerProductoInteresPersona(long codigoPersona);
        OperationResult<IEnumerable<DtoInscriptoDevart>> ObtenerProductosBeca(long codigoPersona);
        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS
    }
}
