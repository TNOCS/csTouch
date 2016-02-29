namespace SharpMap.Geometries
{
    public interface IRaster : IGeometry
    {
        byte[] Data { get; }
        SharpMap.Geometries.BoundingBox GetBoundingBox(); // FIXME TODO "new" keyword missing?
    }
}
