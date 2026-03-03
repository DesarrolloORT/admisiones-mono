using BusinessLogic.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using AppLogic.Interfaces;
using AppLogic.Services;
using MailORT;
using AppLogic.DevartDTOs;
using ModBandejaAppLogic.DevartDTOs;
using AppLogic.Helpers;
using WebApiAdmisiones.Security;
using Utilities;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Controlador para la gestión de la Ficha de Persona (FDP).
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class GeneralController : ApiBaseController<GeneralController>
    {
        #region PROPS & CONSTRUCTOR

        private readonly IGeneralServices _GeneralService;

        /// <summary>
        /// Constructor del controlador FDP.
        /// En este punto validamos si existe un usuario y capturamos su UserId.
        /// </summary>
        /// <param name="FdpService">Servicio de FDP.</param>
        /// <param name="logger">Logger para FdpController.</param>
        /// <param name="envioMailORT">Servicio de envío de mails.</param>
        /// <param name="currentUser">Servicio que expone el usuario actual.</param>
        public GeneralController(
            IGeneralServices FdpService,
            ILogger<GeneralController> logger,
            ICurrentUserService currentUser) : base(logger, currentUser)
        {
            _GeneralService = FdpService;
        }

        #endregion PROPS & CONSTRUCTOR

        #region CONSULTAS(GET) GENERALES

        /// <summary>
        /// Obtiene el país y sus ciudades asociadas.
        /// </summary>
        /// <param name="id">ID del país.</param>
        /// <returns>País y ciudades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Pais")]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 400)]
        public IActionResult ObtenerPais(long id)
        {
            var result = _GeneralService.ObtenerPais(id);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la lista de países.
        /// </summary>
        /// <returns>Lista de países.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Paises")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 400)]
        public IActionResult ObtenerPaises()
        {
            var result = _GeneralService.ObtenerPaises();
            return ValidateResponse(result);
        }

        

        #endregion CONSULTAS(GET) GENERALES

    }
}
