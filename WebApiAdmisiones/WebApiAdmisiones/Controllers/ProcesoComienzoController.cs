using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcesoComienzoController : ApiBaseController<ProcesoComienzoController>
    {
        private readonly IProcesoComienzoServices _procesoComienzoService;

        public ProcesoComienzoController(
            IProcesoComienzoServices procesoComienzoService,
            ILogger<ProcesoComienzoController> logger,
            ICurrentUserService currentUser) : base(logger, currentUser)
        {
            _procesoComienzoService = procesoComienzoService;
        }

        [HttpGet("ObtenerProcesoComienzos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoComienzoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoComienzoDevart>>), 400)]
        public IActionResult ObtenerProcesoComienzos()
        {
            var result = _procesoComienzoService.ObtenerProcesoComienzos();
            return ValidateResponse(result);
        }

        [HttpGet("ObtenerProcesoComienzo")]
        [ProducesResponseType(typeof(OperationResult<DtoProcesoComienzoDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoProcesoComienzoDevart>), 400)]
        public IActionResult ObtenerProcesoComienzo(long idProceso, long idComienzo)
        {
            var result = _procesoComienzoService.ObtenerProcesoComienzo(idProceso, idComienzo);
            return ValidateResponse(result);
        }

        [HttpPost("GuardarProcesoComienzo")]
        [ProducesResponseType(typeof(OperationResult<DtoProcesoComienzoDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoProcesoComienzoDevart>), 400)]
        public IActionResult GuardarProcesoComienzo([FromBody] ProcesoComienzoRequest dto)
        {
            var result = _procesoComienzoService.GuardarProcesoComienzo(dto);
            return ValidateResponse(result);
        }

        [HttpPut("ModificarProcesoComienzo")]
        [ProducesResponseType(typeof(OperationResult<DtoProcesoComienzoDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoProcesoComienzoDevart>), 400)]
        public IActionResult ModificarProcesoComienzo([FromBody] ProcesoComienzoRequest dto)
        {
            var result = _procesoComienzoService.ModificarProcesoComienzo(dto);
            return ValidateResponse(result);
        }
    }
}