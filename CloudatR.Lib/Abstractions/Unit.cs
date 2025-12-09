namespace CloudatR.Lib.Abstractions;

/// <summary>
/// Represents a void-like return type for requests that don't return a value.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// The singleton instance of Unit.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Compares the current instance with another Unit instance.
    /// </summary>
    public int CompareTo(Unit other) => 0;

    /// <summary>
    /// Indicates whether the current Unit instance is equal to another Unit instance.
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Determines whether the specified object is equal to the current Unit instance.
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns a string representation of the Unit instance.
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Determines whether two Unit instances are equal.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Determines whether two Unit instances are not equal.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
