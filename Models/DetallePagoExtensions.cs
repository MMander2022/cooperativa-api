namespace CooperativaApp.Models
{
    public static class DetallePagoExtensions
    {
        public static decimal GetCapital(this DetallePago detalle)
        {
            return detalle.IdConcepto == 1 ? detalle.Monto : 0;
        }

        public static decimal GetInteres(this DetallePago detalle)
        {
            return detalle.IdConcepto == 2 ? detalle.Monto : 0;
        }
    }
}