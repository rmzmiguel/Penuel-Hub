namespace Penuel.Infrastructure.Persistence.Seed;

/// <summary>
/// Datos sembrados con la migración inicial (Paso 7).
/// </summary>
/// <remarks>
/// Los identificadores son FIJOS y están escritos a mano, no generados: <c>HasData</c> exige
/// valores constantes, y además el bootstrap del Paso 9 necesita referirse al rol Pastor y al
/// cargo Pastor por su Id. El prefijo agrupa por tipo para que la base sea legible de un vistazo
/// al administrarla desde Supabase (10 = iglesia, 20 = roles, 30 = ministerios, 40 = sociedades,
/// 50 = cargos).
/// </remarks>
public static class CoreSeedData
{
    /// <summary>Marca de tiempo fija de la siembra: <c>HasData</c> no admite un valor variable.</summary>
    public static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid ChurchId = new("10000000-0000-4000-8000-000000000001");

    public static class RoleIds
    {
        public static readonly Guid Pastor = new("20000000-0000-4000-8000-000000000001");

        /// <summary>
        /// Agregado por la rama de Servicios. Vive aquí, con los demás roles, porque el
        /// catálogo de roles es del Core: la rama no crea uno propio (Sección 5 de esa rama).
        /// </summary>
        public static readonly Guid SundaySchoolRecorder = new("20000000-0000-4000-8000-000000000002");

        /// <summary>
        /// Rol de servicio, no de congregación: quien mantiene el sistema. Se siembra igual
        /// que los demás porque <c>AssignRoleCommand</c> busca el rol por nombre en la tabla —
        /// sin la fila, la constante de <c>RoleNames</c> no serviría para nada.
        /// </summary>
        public static readonly Guid Developer = new("20000000-0000-4000-8000-000000000003");
    }

    public static class MinistryIds
    {
        public static readonly Guid Evangelismo = new("30000000-0000-4000-8000-000000000001");
        public static readonly Guid Comunion = new("30000000-0000-4000-8000-000000000002");
        public static readonly Guid Discipulado = new("30000000-0000-4000-8000-000000000003");
        public static readonly Guid Adoracion = new("30000000-0000-4000-8000-000000000004");
        public static readonly Guid Servicio = new("30000000-0000-4000-8000-000000000005");
        public static readonly Guid Infantil = new("30000000-0000-4000-8000-000000000006");
    }

    public static class SocietyIds
    {
        public static readonly Guid Damas = new("40000000-0000-4000-8000-000000000001");
        public static readonly Guid Varones = new("40000000-0000-4000-8000-000000000002");
        public static readonly Guid Jovenes = new("40000000-0000-4000-8000-000000000003");
        public static readonly Guid Infantil = new("40000000-0000-4000-8000-000000000004");
    }

    public static class PositionIds
    {
        public static readonly Guid Pastor = new("50000000-0000-4000-8000-000000000001");
        public static readonly Guid Diacono = new("50000000-0000-4000-8000-000000000002");
        public static readonly Guid SecretarioGeneral = new("50000000-0000-4000-8000-000000000003");
        public static readonly Guid TesoreroGeneral = new("50000000-0000-4000-8000-000000000004");
    }
}
