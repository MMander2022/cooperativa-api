using System.Text.Json.Serialization;

namespace CooperativaApp.DTOS
{

    public class OperacionResponse
    {
        [JsonPropertyName("idOperacion")]
        public int? IdOperacion { get; set; }

        [JsonPropertyName("idCredito")]
        public int? IdCredito { get; set; }
        [JsonPropertyName("idPago")] // Añadido para consistencia en JSON
        public int? IdPago { get; set; } // 💎 Cambiado a nulable

        [JsonPropertyName("idMovimientoCaja")]
        public int? IdMovimientoCaja { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; }

        [JsonPropertyName("exito")]
        public bool Exito { get; set; }

        [JsonPropertyName("saldoPendienteDesembolso")]
        public decimal? SaldoPendienteDesembolso { get; set; }

        // Constructores
        public OperacionResponse() { }

        public OperacionResponse(bool exito, string mensaje)
        {
            Exito = exito;
            Mensaje = mensaje;
        }

        // Constructor para mapeo desde SP
        public OperacionResponse(int idOperacion, int idCredito, int? idMovimientoCaja, string mensaje, bool exito, decimal saldoPendiente, int idPago)
        {
            IdOperacion = idOperacion;
            IdCredito = idCredito;
            IdMovimientoCaja = idMovimientoCaja;
            Mensaje = mensaje;
            Exito = exito;
            SaldoPendienteDesembolso = saldoPendiente;
            IdPago = idPago;
        }
    }
}

