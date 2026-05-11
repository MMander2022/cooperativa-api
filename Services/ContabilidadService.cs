using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
namespace CooperativaApp.Services
{
    public class ContabilidadService
    {
        private readonly CooperativaContext _context;

        public async Task ValidarCuentaParaMovimiento(string codigoCuenta)
        {
            var cuenta = await _context.CuentasContables.FindAsync(codigoCuenta);
            if (cuenta == null) throw new Exception($"La cuenta {codigoCuenta} no existe.");
            if (!cuenta.EsAnalitica) throw new Exception($"La cuenta {codigoCuenta} es de título, no permite registros.");
            if (!cuenta.Activa) throw new Exception($"La cuenta {codigoCuenta} está inactiva.");
        }
    }

}
