/// <summary>
///     Interface for entities that support soft deletion functionality.
///     Soft-deleted entities are logically deleted (hidden from queries/automatically filtered) but remain in the database, 
///     until they are permanently removed by a cleanup process, every so often.
/// </summary>
/// <remarks>
///     Brug ikke på owned entities.
/// </remarks>
/// 
/// <para>
///     Entities inheriting from this interface will have a <see cref="SoftDeletedOnUtc"/> property
///     that tracks when the record was soft-deleted. A <see langword="null"/> value indicates an active record,
///     while a non-null timestamp indicates the record is logically deleted.
/// </para>
/// <para>
///     A global EF Core query filter is automatically applied in
/// <see cref="Data.AppDbContext.OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder)"/>
///     to every entity type that inherits from this interface. The filter excludes all records where
/// <see cref="SoftDeletedOnUtc"/> is not <see langword="null"/>, so soft-deleted rows are
///     invisible to normal queries by default. Use <c>IgnoreQueryFilters()</c> when you need to include
///     soft-deleted records in a query.
/// </para>
public abstract class SoftDeletableBase
{
    /// <summary>
    /// Computed property that indicates whether <see cref="SoftDeletedOnUtc"/> has a value, i.e., whether the record is currently marked as deleted (Soft or hard).
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public bool IsDeleted { get { return SoftDeletedOnUtc.HasValue || HardDelete; } }

    /// <summary>
    ///   Computed property that indicates whether the record is currently marked as soft-deleted (i.e., <see cref="SoftDeletedOnUtc"/> has a value and <see cref="HardDelete"/> is false).
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public bool IsSoftDeleted { get { return SoftDeletedOnUtc.HasValue && !HardDelete; } }

    /// <summary>
    /// Gets or sets a value indicating whether this record should be hard-deleted (permanently removed from the database) instead of soft-deleted.
    /// Set this to true before calling <see cref="Microsoft.EntityFrameworkCore.DbContext.Remove{TEntity}(TEntity)"/> to indicate that the record should be permanently deleted instead of just soft-deleted.
    /// </summary>
    [JsonIgnore]
    [NotMapped]
    public bool HardDelete { get; set; } = false;


    /// <summary>
    ///     Gets or sets the local server time at which this record was soft-deleted.
    ///     A <see langword="null"/> value indicates the record is active (not deleted).
    ///     When set, the record is treated as logically deleted and excluded from queries.
    ///     If the user is later subscribed to FMK, this property should be set back to <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Do not use a setter. Use the provided methods <see cref="MarkAsNotDeleted"/> instead or <see cref="MarkAsSoftDeleted(DateTime)"/> to set this property.
    /// To delete a record, call <see cref="Microsoft.EntityFrameworkCore.DbContext.Remove{TEntity}(TEntity)"/> instead, which sets this property to the current UTC time and filters it out of any get queries. 
    /// To restore a record, call <see cref="MarkAsNotDeleted"/>, which sets this property back to <see langword="null"/>.
    /// </remarks>
    [JsonIgnore]
    public DateTime? SoftDeletedOnUtc { get; private set; }

    /// <summary>
    ///   Marks the record as not deleted by setting <see cref="SoftDeletedOnUtc"/> to <see langword="null"/> and <see cref="HardDelete"/> to false.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void MarkAsNotDeleted()
    {
        if (!SoftDeletedOnUtc.HasValue) throw new InvalidOperationException("The record is not marked as soft deleted.");

        SoftDeletedOnUtc = null;
        HardDelete = false;
    }

    /// <summary>
    ///  Marks the record as soft-deleted by setting <see cref="SoftDeletedOnUtc"/> to the provided UTC time and <see cref="HardDelete"/> to false.
    /// </summary>
    /// <param name="utcNow"></param>
    public void MarkAsSoftDeleted(DateTime utcNow)
    {
        SoftDeletedOnUtc = utcNow;
        HardDelete = false;
    }
}
