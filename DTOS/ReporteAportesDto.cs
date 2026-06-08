using System;
using System.Collections.Generic;

namespace CooperativaApp.DTOs
{
    public class ReporteAportesConsolidadoDto
    {
        public List<AporteAnualDto> HistoricoCincoAnios { get; set; } = new();
        public List<AporteMensualDto> DetalleMensualAnio { get; set; } = new();
        public List<DetalleAporteSocioDto> ListaDetallada { get; set; } = new();
    }

    public class AporteAnualDto
    {
        public int Anio { get; set; }
        public decimal TotalPagado { get; set; }
    }

    public class AporteMensualDto
    {
        public int Mes { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
        public int CantidadAportes { get; set; }
    }

    public class DetalleAporteSocioDto
    {
        public int IdAporte { get; set; }
        public int IdSocio { get; set; }
        public string NombreSocio { get; set; } = string.Empty;
        public string DniSocio { get; set; } = string.Empty;
        public int Mes { get; set; }
        public int Anio { get; set; }
        public decimal Monto { get; set; }
        public string FechaPago { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}