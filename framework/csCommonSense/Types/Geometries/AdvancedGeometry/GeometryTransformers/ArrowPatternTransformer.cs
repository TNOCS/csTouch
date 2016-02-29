namespace csCommon.Types.Geometries.AdvancedGeometry.GeometryTransformers
{
	/// <summary>
	/// Arrow Pattern Transformer
	/// </summary>
	public class ArrowPatternTransformer : PatternTransformer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ArrowPatternTransformer"/> class.
		/// </summary>
		public ArrowPatternTransformer()
		{
			AtStart = false;
			AtMiddle = false;
			AtEnd = true;
			BySegment = false;

			PatternName = "arrow";
		}
	}

}
