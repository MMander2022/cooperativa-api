using System;

namespace CooperativaApp.DTOs
{
    public class HistorialUtilidadDto
    {
        public string PeriodoNombre { get; set; }
        public string MesEvaluado { get; set; }
        public int AnioFiscal { get; set; }
        public decimal InteresMensualBruto { get; set; }
        public decimal GastoMensual { get; set; }
        public decimal TotalAportesConsolidado { get; set; }
        public decimal TotalUtilidadConsolidada { get; set; }
        public int IdSocio { get; set; }
        public string CodigoSocio { get; set; }
        public string NombreCompleto { get; set; }
        public decimal AporteAcumulado { get; set; }
        public decimal AporteDelMes { get; set; }
        public decimal UtilidadGenerada { get; set; }
        public decimal AporteAcumuladoFinal { get; set; }
    }
}