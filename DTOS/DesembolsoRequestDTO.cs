namespace CooperativaApp.DTOS
{
    public class DesembolsoRequest
    {
        public int IdCredito { get; set; }

        // 🔹 Aquí está el culpable: Asegúrese de que se llame 'Monto' 
        // y sea decimal para precisión financiera.
        public decimal Monto { get; set; }

        public int UsuarioId { get; set; }
        public int IdCaja { get; set; }
        public string Observacion { get; set; }
        public int? IdMedioPago { get; set; } // 🎯 Cambio de string a int
        public DateTime? FechaPrimerDesembolso { get; set; } // 🎯 NUEVO: Fecha seleccionada de salida
    }
}
