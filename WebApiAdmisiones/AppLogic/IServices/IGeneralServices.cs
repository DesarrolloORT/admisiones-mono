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
        OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto);
        OperationResult<DtoInscriptoDevart> ObtenerUltimaInscripcionActiva(long codigoPersona);
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
        OperationResult<IEnumerable<DtoProductoDevart>> ObtenerProductosConInteresActivo(long codigoPersona);
        OperationResult<IEnumerable<DtoProductoDevart>> ObtenerProductosVigentesConInteres(long codigoPersona);
        OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesPendientes(long codigoPersona);
        OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesCanceladas(long codigoPersona);
        OperationResult<IEnumerable<DtoOfertaDevart>> ObtenerOfertasParaInscripcionConProceso(long idProducto, long idProceso, long idTurno);
        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        #region IMAGEN / DOCUMENTOS
        OperationResult<byte[]> ObtenerDocumentoAlumno(long codigoPersona, int tipo);
        OperationResult<byte[]> ObtenerFotoAlumno(long codigoPersona);
        #endregion IMAGEN / DOCUMENTOS
    }
}
