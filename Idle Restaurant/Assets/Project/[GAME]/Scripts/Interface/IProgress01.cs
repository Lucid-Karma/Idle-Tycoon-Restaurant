// Read-only 0..1 progress of a gameplay timer, so presentation can show it without owning it.
public interface IProgress01
{
    float Progress01 { get; }
}
