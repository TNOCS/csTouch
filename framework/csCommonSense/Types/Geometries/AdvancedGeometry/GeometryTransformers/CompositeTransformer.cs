using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Markup;
using System.Windows.Media;

namespace csCommon.Types.Geometries.AdvancedGeometry.GeometryTransformers
{
	/// <summary>
	/// Composite Transformer allowing to transform a geometry by using a list of geometry transformers.
	/// </summary>
	[ContentProperty("GeometryTransformers")]
	public class CompositeTransformer : IGeometryTransformer, ICollection<IGeometryTransformer>
	{
		
		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeTransformer"/> class.
		/// </summary>
		public CompositeTransformer()
		{
			_geometryTransformers = new Collection<IGeometryTransformer>();
		}

		/// <summary>
		/// Transforms the specified geometry.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		public void Transform(Geometry geometry)
		{
			foreach (var geometryTransformer in _geometryTransformers)
				geometryTransformer.Transform(geometry);
		}

		private Collection<IGeometryTransformer> _geometryTransformers;

		/// <summary>
		/// Gets or sets the geometry transformers (needed to be able to add transformers in XAML).
		/// </summary>
		/// <value>The geometry transformers.</value>
		public Collection<IGeometryTransformer> GeometryTransformers
		{
			get { return _geometryTransformers; }
			set { _geometryTransformers = value; }
		}

		#region ICollection interface
		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<IGeometryTransformer> GetEnumerator()
		{
			return _geometryTransformers.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _geometryTransformers.GetEnumerator();
		}

		/// <summary>
		/// Adds the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		public void Add(IGeometryTransformer item)
		{
			_geometryTransformers.Add(item);
		}

		/// <summary>
		/// Clears this instance.
		/// </summary>
		public void Clear()
		{
			_geometryTransformers.Clear();
		}

		/// <summary>
		/// Determines whether [contains] [the specified item].
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>
		/// 	<c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
		/// </returns>
		public bool Contains(IGeometryTransformer item)
		{
			return _geometryTransformers.Contains(item);
		}

		/// <summary>
		/// Copies to.
		/// </summary>
		/// <param name="array">The array.</param>
		/// <param name="arrayIndex">Index of the array.</param>
		public void CopyTo(IGeometryTransformer[] array, int arrayIndex)
		{
			_geometryTransformers.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		/// <value>The count.</value>
		public int Count
		{
			get { return _geometryTransformers.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is read only.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is read only; otherwise, <c>false</c>.
		/// </value>
		public bool IsReadOnly
		{
			get { return (_geometryTransformers as IList).IsReadOnly; }
		}

		/// <summary>
		/// Removes the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		public bool Remove(IGeometryTransformer item)
		{
			return _geometryTransformers.Remove(item);
		} 
		#endregion

	}
}
