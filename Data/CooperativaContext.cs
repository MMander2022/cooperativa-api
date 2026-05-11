using CooperativaApp.DTOS;
using CooperativaApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using YourProject.Models;
namespace CooperativaApp.Data
{

    public class CooperativaContext : DbContext
    {
        public DbSet<OperacionResponse> OperacionResponses { get; set; } //
                                                                         // ional pero recomendado
        public CooperativaContext(DbContextOptions<CooperativaContext> options) : base(options)
        {

        }
        public DbSet<GlobalSettings> GlobalSettings { get; set; }
        public DbSet<Socio> Socios { get; set; }
        public DbSet<SolicitudCredito> Solicitudes { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<ProductoTasa> ProductoTasas { get; set; }
        public DbSet<Credito> Creditos { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<DetallePago> DetallePago { get; set; }
        public DbSet<Mora> Moras { get; set; }
        public DbSet<ConfiguracionMora> ConfiguracionMora { get; set; }
       // public DbSet<AsientossContable> AsientosContabless { get; set; }
        public DbSet<AsientosContables> AsientosContables { get; set; }
        public DbSet<DetalleAsiento> DetalleAsiento { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }
        public DbSet<CuentaContable> CuentasContables { get; set; }
        public DbSet<ConceptoOperacion> ConceptosOperacion { get; set; } 
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Perfil> Perfiles { get; set; }
        public DbSet<Opcion> Opciones { get; set; }
        public DbSet<LogActividad> LogsActividad { get; set; }
        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<PerfilModulo> PerfilModulo { get; set; }
        public DbSet<MotivoBaja> MotivoBaja { get; set; }
        public DbSet<Familiaridad> Familiaridad { get; set; }
        public DbSet<Parentesco> Parentescos { get; set; }
        public DbSet<HistorialEstadoSocio> HistorialEstadoSocio { get; set; }

        // 🛰️ REGISTRO DE NUEVAS ENTIDADES FINANCIERAS
        public DbSet<ConfigAporte> ConfigAportes { get; set; } = null!;
        public DbSet<AporteSocio> AportesSocios { get; set; } = null!;
        public DbSet<SolicitudPagoSocio> SolicitudPagoSocio { get; set; } = null!;
        public DbSet<SolicitudPagoDetalle> SolicitudPagoDetalle { get; set; } = null!;
        public DbSet<MedioPago> MedioPago { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<GlobalSettings>(entity =>
            {
                entity.HasKey(e => e.SettingId);
                entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(50);
            });
            // Configuración de la llave primaria compuesta para la tabla de permisos
            modelBuilder.Entity<PerfilModulo>()
                .HasKey(pm => new { pm.IdPerfil, pm.IdModulo });
            // Configuración adicional si fuera necesaria
            modelBuilder.Entity<ProductoTasa>()
                .Property(p => p.TasaInteres)
                .HasPrecision(5, 2);

            // CUOTA
            modelBuilder.Entity<Cuota>()
                .Property(p => p.Capital).HasPrecision(18, 2);
            modelBuilder.Entity<Cuota>()
                .Property(p => p.Interes).HasPrecision(18, 2);
            modelBuilder.Entity<Cuota>()
                .Property(p => p.Saldo).HasPrecision(18, 2);
            modelBuilder.Entity<Cuota>()
                .Property(p => p.MontoCuota).HasPrecision(18, 2);
            modelBuilder.Entity<Cuota>()
                .Property(p => p.SaldoCapital).HasPrecision(18, 2);
            modelBuilder.Entity<Cuota>()
                .Property(p => p.SaldoInteres).HasPrecision(18, 2);

            // CREDITO
            modelBuilder.Entity<Credito>()
                .Property(p => p.Monto).HasPrecision(18, 2);
            modelBuilder.Entity<Credito>()
                .Property(p => p.SaldoCapital).HasPrecision(18, 2);
            modelBuilder.Entity<Credito>()
                .Property(p => p.TasaInteres).HasPrecision(8, 4);

            // PAGO
            modelBuilder.Entity<Pago>()
                .Property(p => p.MontoTotal).HasPrecision(18, 2);

           

            // MORA
            modelBuilder.Entity<Mora>()
                .Property(p => p.MontoMora).HasPrecision(18, 2);
            modelBuilder.Entity<Mora>()
                .Property(p => p.MontoPagado).HasPrecision(18, 2);
            modelBuilder.Entity<Mora>()
                .Property(p => p.SaldoMora).HasPrecision(18, 2);

            // APROBACION
            modelBuilder.Entity<AprobacionResponse>().HasNoKey();
            // MIGRACIÓN A AZURE
            modelBuilder.Entity<ConceptoOperacion>(entity =>
            {
                entity.ToTable("ConceptosOperacion");
                entity.HasKey(c => c.IdConcepto);

                // Relación para el DEBE
                entity.HasOne(c => c.CuentaContableDebeNavigation) // Propiedad virtual de navegación (objeto)
                    .WithMany()
                    .HasForeignKey(c => c.CuentaContableDebe) // Propiedad de tipo string (FK física)
                    .OnDelete(DeleteBehavior.NoAction); // <--- Evita el ciclo de cascada en SQL Azure

                // Relación para el HABER
                entity.HasOne(c => c.CuentaContableHaberNavigation) // Propiedad virtual de navegación (objeto)
                    .WithMany()
                    .HasForeignKey(c => c.CuentaContableHaber) // Propiedad de tipo string (FK física)
                    .OnDelete(DeleteBehavior.NoAction); // <--- Evita el ciclo de cascada en SQL Azure
            });
            modelBuilder.Entity<Familiaridad>(entity =>
            {
                entity.ToTable("Familiaridad");
                entity.HasKey(f => f.IdFamiliaridad);

                // Relación con el Socio Titular (Desactiva borrado en cascada)
                entity.HasOne(f => f.SocioTitular) // Asegúrate de que el nombre coincida con tu propiedad virtual de navegación en Familiaridad
                    .WithMany()
                    .HasForeignKey(f => f.IdSocioTitular)
                    .OnDelete(DeleteBehavior.Restrict); // <--- LA CLAVE

                // Relación con el Socio Familiar (Desactiva borrado en cascada)
                entity.HasOne(f => f.SocioFamiliar) // Asegúrate de que el nombre coincida con tu propiedad virtual de navegación en Familiaridad
                    .WithMany()
                    .HasForeignKey(f => f.IdSocioFamiliar)
                    .OnDelete(DeleteBehavior.Restrict); // <--- LA CLAVE
            });
            modelBuilder.Entity<SolicitudPagoDetalle>(entity =>
            {
                entity.ToTable("SolicitudPagoDetalle");
                entity.HasKey(spd => spd.IdSolicitudDetalle);

                // Relación con Solicitud de Pago (Desactiva el borrado en cascada)
                entity.HasOne(spd => spd.Solicitud) // Asegúrate de que coincida con tu propiedad virtual de navegación
                    .WithMany()
                    .HasForeignKey(spd => spd.IdSolicitud)
                    .OnDelete(DeleteBehavior.NoAction); // <--- EVITA EL CICLO EN AZURE

                // Relación con Cuotas (Desactiva el borrado en cascada)
                entity.HasOne(spd => spd.Cuota) // Asegúrate de que coincida con tu propiedad virtual de navegación
                    .WithMany()
                    .HasForeignKey(spd => spd.IdCuota)
                    .OnDelete(DeleteBehavior.NoAction); // <--- EVITA EL CICLO EN AZURE
            });
            modelBuilder.Entity<Credito>(entity =>
            {
                // Forzamos que si se elimina un Socio, el sistema impida la acción si tiene créditos activos
                entity.HasOne(c => c.Socio) // Asegúrate de que coincida con tu propiedad virtual de navegación en la clase Credito
                    .WithMany() // O .WithMany(s => s.Creditos) si tienes la colección en la clase Socio
                    .HasForeignKey(c => c.IdSocio)
                    .OnDelete(DeleteBehavior.Restrict); // <--- PROTECCIÓN CONTRA CASCADA MÚLTIPLE
            });
            modelBuilder.Entity<Pago>(entity =>
            {
                entity.ToTable("Pagos");
                entity.HasKey(p => p.IdPago);

                // Evitamos que al eliminar un Crédito se eliminen en cascada sus pagos asociados
                entity.HasOne(p => p.Credito) // Asegúrate de que coincida con tu propiedad virtual de navegación en la clase Pago
                    .WithMany() // O .WithMany(c => c.Pagos) si tienes la colección en la clase Credito
                    .HasForeignKey(p => p.IdCredito)
                    .OnDelete(DeleteBehavior.NoAction); // <--- PROTECCIÓN DE INTEGRIDAD FINANCIERA
            });
            // 🛡️ Blindaje total de llaves primarias
            modelBuilder.Entity<Perfil>().HasKey(p => p.IdPerfil);
            modelBuilder.Entity<Opcion>().HasKey(o => o.IdOpcion);
            modelBuilder.Entity<LogActividad>().HasKey(l => l.IdLog);
            modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);
            // 🔹 ESTO ES VITAL: Le dice a EF que este objeto no tiene una Primary Key
            modelBuilder.Entity<OperacionResponse>().HasNoKey();
            modelBuilder.Entity<LogActividad>().ToTable("Logs_Actividad");
            modelBuilder.Entity<Perfil>().ToTable("Perfiles");
            modelBuilder.Entity<Opcion>().ToTable("Opciones");
            modelBuilder.Entity<Socio>().ToTable("Socio");
            //modelBuilder.Entity<CuotaDetalleDTO>().HasNoKey();
            modelBuilder.Entity<CuotaAnaliticaDTO>(eb =>
            {
                eb.HasNoKey();
                eb.ToView(null);

                // Configuración de precisión para todos los campos financieros
                foreach (var property in eb.Metadata.GetProperties().Where(p => p.ClrType == typeof(decimal)))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            });
            modelBuilder.Entity<CuotaDetalleDTO>(eb =>
            {
                eb.HasNoKey();
                eb.ToView(null); // Indica que no existe una tabla física

                // Forzamos la precisión para que el mapeador no descarte los valores
                eb.Property(x => x.MontoCuota).HasPrecision(18, 2);
                eb.Property(x => x.SaldoCuota).HasPrecision(18, 2);
                eb.Property(x => x.MontoPagadoReal).HasPrecision(18, 2);
                eb.Property(x => x.MontoEnRevision).HasPrecision(18, 2);
            });
            modelBuilder.Entity<MotivoBaja>(entity =>
            {
                entity.ToTable("MotivosBaja");
                entity.HasKey(e => e.IdMotivo); // 👈 Forzamos la PK
            });

            // Configuración para Historial
            modelBuilder.Entity<HistorialEstadoSocio>(entity =>
            {
                entity.ToTable("HistorialEstadoSocio");
                entity.HasKey(e => e.IdHistorial);
            });

            // Configuramos la relación Historial -> Motivo de forma robusta
            modelBuilder.Entity<HistorialEstadoSocio>()
            .HasOne(h => h.Motivo)
            .WithMany() // No necesitamos colección en MotivoBaja si no queremos
            .HasForeignKey(h => h.IdMotivo)
            .OnDelete(DeleteBehavior.Restrict);



            // Configuramos la relación Historial -> Motivo de forma robusta
            modelBuilder.Entity<HistorialEstadoSocio>()
                .HasOne(h => h.Motivo)
                .WithMany() // No necesitamos colección en MotivoBaja si no queremos
                .HasForeignKey(h => h.IdMotivo)
                .OnDelete(DeleteBehavior.Restrict);

            // ConfigAportes
            modelBuilder.Entity<ConfigAporte>(entity => {
                entity.ToTable("ConfigAportes");
                entity.HasKey(e => e.IdConfig);
                entity.Property(e => e.ValorAccion).HasPrecision(18, 2);
            });

            // AportesSocios
            modelBuilder.Entity<AporteSocio>(entity => {
                entity.ToTable("AportesSocios");
                entity.HasKey(e => e.IdAporte);
                entity.Property(e => e.MontoPagado).HasPrecision(18, 2);

                // 🏁 Restricción Única Titanium: Evita doble pago del mismo mes/año
                entity.HasIndex(e => new { e.IdSocio, e.MesAportado, e.AnioAportado }).IsUnique();

                // Relaciones
                entity.HasOne(d => d.Socio)
                    .WithMany() // O .WithMany(p => p.Aportes) si quieres navegación inversa
                    .HasForeignKey(d => d.IdSocio);
            });
            // ==========================================
            // BLINDAJE GLOBAL CONTRA CASCADAS MÚLTIPLES (PREVENTIVO)
            // ==========================================
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                // Si la relación está configurada para borrar en cascada de forma automática...
                if (relationship.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    // La cambiamos preventivamente a Restrict (NoAction en SQL Server)
                    relationship.DeleteBehavior = DeleteBehavior.ClientSetNull;
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information) // Imprime SQL en consola
                .EnableSensitiveDataLogging(); // Muestra los valores de los parámetros (solo para desarrollo)
        }

    }

}
