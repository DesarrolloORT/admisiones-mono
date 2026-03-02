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
        #endregion CONSULTAS GENERALES
    }
}
