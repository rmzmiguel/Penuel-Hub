namespace Penuel.Infrastructure.Persistence.Seed;

/// <summary>
/// Identificadores fijos de los datos sembrados por la rama de Servicios y Cultos.
/// </summary>
/// <remarks>
/// Mismo esquema legible que <see cref="CoreSeedData"/>, con el prefijo 60 reservado a los
/// tipos de servicio (10 = iglesia, 20 = roles, 30 = ministerios, 40 = sociedades,
/// 50 = cargos, 60 = tipos de servicio).
/// La marca de tiempo se reutiliza de <see cref="CoreSeedData.SeededAt"/>: <c>HasData</c> exige
/// un valor constante, y todos estos son datos de referencia sembrados por el sistema, no
/// capturados por nadie.
/// </remarks>
public static class ServicesSeedData
{
    public static class ServiceTypeIds
    {
        public static readonly Guid EscuelaDominical = new("60000000-0000-4000-8000-000000000001");
        public static readonly Guid CultoGeneral = new("60000000-0000-4000-8000-000000000002");
        public static readonly Guid CultoDeOracion = new("60000000-0000-4000-8000-000000000003");
        public static readonly Guid CultoDeJovenes = new("60000000-0000-4000-8000-000000000004");
    }
}
