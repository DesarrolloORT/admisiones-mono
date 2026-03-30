using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RegistroController(
        IRegistroService registroService,
        ILogger<RegistroController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<RegistroController>(logger, currentUser)
    {
        [AllowAnonymous]
        [HttpGet("Paises")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 400)]
        public IActionResult ObtenerPaises()
        {
            var result = registroService.ObtenerPaises();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("TiposDocumentos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 400)]
        public IActionResult ObtenerTipoDocumentos()
        {
            var result = registroService.ObtenerTipoDocumentos();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("ProcesosHabilitadosPorProducto")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoDevart>>), 400)]
        public IActionResult ObtenerProcesosHabilitadosPorProducto([FromQuery] long idProducto)
        {
            var result = registroService.ObtenerProcesosHabilitadosPorProducto(idProducto);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("PaisesEstadosCiudades")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 400)]
        public IActionResult ObtenerPaisesEstadosCiudades()
        {
            var result = registroService.ObtenerPaisesEstadosCiudades();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("ProductosVigentes")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoAdmisiones>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoAdmisiones>>), 400)]
        public IActionResult ObtenerProductosVigentes()
        {
            var result = registroService.ObtenerProductosVigentes();
            return ValidateResponse(result);
        }
    }
}
