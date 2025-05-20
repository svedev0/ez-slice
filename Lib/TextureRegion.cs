using UnityEngine;

namespace EzSlice.Lib
{
	/// <summary>
	/// Defines a region of a specific texture which can be used for custom UV
	/// mapping routines. TextureRegion is always stored in normalized UV
	/// coordinate space between 0.0f and 1.0f.
	/// </summary>
	public struct TextureRegion
	{
		private readonly float _posStartX;
		private readonly float _posStartY;
		private readonly float _posEndX;
		private readonly float _posEndY;

		public TextureRegion(float startX, float startY, float endX, float endY)
		{
			_posStartX = startX;
			_posStartY = startY;
			_posEndX = endX;
			_posEndY = endY;
		}

		public float StartX => _posStartX;
		public float StartY => _posStartY;
		public float EndX => _posEndX;
		public float EndY => _posEndY;

		public Vector2 Start => new(StartX, StartY);

		public Vector2 End => new(EndX, EndY);

		/// <summary>
		/// Perform a mapping of a UV coordinate (computed in 0,1 space) into
		/// the new coordinates defined by the provided TextureRegion.
		/// </summary>
		public Vector2 Map(Vector2 uv)
		{
			return Map(uv.x, uv.y);
		}

		/// <summary>
		/// Perform a mapping of a UV coordinate (computed in 0,1 space) into
		/// the new coordinates defined by the provided TextureRegion.
		/// </summary>
		public Vector2 Map(float x, float y)
		{
			float mappedX = Map(x, 0.0f, 1.0f, _posStartX, _posEndX);
			float mappedY = Map(y, 0.0f, 1.0f, _posStartY, _posEndY);
			return new Vector2(mappedX, mappedY);
		}

		/// <summary>
		/// Map arbitrary values into required texture region.
		/// </summary>
		private static float Map(float x, float inMin, float inMax, float outMin, float outMax)
		{
			return (x - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
		}
	}

	/// <summary>
	/// TextureRegion extensions to easily calculate from a Texture2D object.
	/// </summary>
	public static class TextureRegionExtension
	{
		/// <summary>
		/// Calculates the TextureRegion from a material using the mainTexture
		/// component to perform the calculation. Throws a null exception if
		/// the texture does not exist.
		/// </summary>
		public static TextureRegion GetTextureRegion(
			this Material mat, int pixX, int pixY, int pixWidth, int pixHeight)
		{
			return mat.mainTexture.GetTextureRegion(pixX, pixY, pixWidth, pixHeight);
		}

		/// <summary>
		/// Using a Texture2D, calculate and return a new TextureRegion for the
		/// provided pixel coordinates where 0,0 is the bottom left corner of
		/// the texture. The TextureRegion is automatically calculated to
		/// ensure that it will fit inside the texture.
		/// </summary>
		public static TextureRegion GetTextureRegion(
			this Texture tex, int pixX, int pixY, int pixWidth, int pixHeight)
		{
			int textureWidth = tex.width;
			int textureHeight = tex.height;

			// Ensure referenced coordinates are within bounds of texture.
			int calcWidth = Mathf.Min(textureWidth, pixWidth);
			int calcHeight = Mathf.Min(textureHeight, pixHeight);
			int calcX = Mathf.Min(Mathf.Abs(pixX), textureWidth);
			int calcY = Mathf.Min(Mathf.Abs(pixY), textureHeight);

			float startX = calcX / (float)textureWidth;
			float startY = calcY / (float)textureHeight;
			float endX = (calcX + calcWidth) / (float)textureWidth;
			float endY = (calcY + calcHeight) / (float)textureHeight;

			return new TextureRegion(startX, startY, endX, endY);
		}
	}
}
