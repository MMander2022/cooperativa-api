using System;
namespace CooperativaApp.Utils
{
    public static class DateTimeUtils
    {
        public static DateTime ObtenerHoraPeru()
        {
            try
            {
                TimeZoneInfo timeZonePeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZonePeru);
            }
            catch
            {
                // Fallback si corre en Linux/Docker con ID IANA
                TimeZoneInfo timeZonePeruLinux = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZonePeruLinux);
            }
        }
    }
}
