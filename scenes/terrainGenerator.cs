using Godot;
using System;
using System.Collections.Generic; // Added for List<>

[Tool]
public partial class terrainGenerator : MeshInstance3D
{
	[Export] public int Width = 50;
	[Export] public int Depth = 50;
	[Export] public float HeightMultiplier = 15f;
	[Export] public FastNoiseLite Noise;
	[Export] public MultiMeshInstance3D FloraMultiMesh;
	[Export] public float PlantDensityThreshold = 0.6f; 

	public override void _Ready()
	{
		if (Noise == null)
		{
			Noise = new FastNoiseLite();
			Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
			Noise.Frequency = 0.03f; 
		}

		GenerateOceanFloor();
	}

	private void GenerateOceanFloor()
	{
		SurfaceTool st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		
		List<Transform3D> plantTransforms = new List<Transform3D>();

		// Generate Vertices
		for (int z = 0; z <= Depth; z++)
		{
			for (int x = 0; x <= Width; x++)
			{
				float y = Noise.GetNoise2D(x, z) * HeightMultiplier;
				st.SetUV(new Vector2((float)x / Width, (float)z / Depth));
				
				// Define vertexPosition first so both the mesh and the plant can use it
				Vector3 vertexPosition = new Vector3(x - (Width / 2f), y, z - (Depth / 2f));
				st.AddVertex(vertexPosition);
				
				float plantNoise = Noise.GetNoise2D(x + 100, z + 100);
				
				if (plantNoise > PlantDensityThreshold)
				{
					Vector3 plantPos = vertexPosition + new Vector3(0, 1.0f, 0);
					Transform3D t = new Transform3D(Basis.Identity, vertexPosition);
					plantTransforms.Add(t);
				}
			}
		}

		// Generate Indices
		for (int z = 0; z < Depth; z++)
		{
			for (int x = 0; x < Width; x++)
			{
				int currentVertex = x + z * (Width + 1);

				st.AddIndex(currentVertex);
				st.AddIndex(currentVertex + 1);
				st.AddIndex(currentVertex + Width + 1);

				st.AddIndex(currentVertex + 1);
				st.AddIndex(currentVertex + Width + 2);
				st.AddIndex(currentVertex + Width + 1);
			}
		}
		
		st.GenerateNormals();
		this.Mesh = st.Commit();
		
		if (FloraMultiMesh != null && FloraMultiMesh.Multimesh != null)
		{
			FloraMultiMesh.Multimesh.InstanceCount = plantTransforms.Count;
			
			for (int i = 0; i < plantTransforms.Count; i++)
			{
				FloraMultiMesh.Multimesh.SetInstanceTransform(i, plantTransforms[i]);
			}
		}

		CreateCollision();
	}

	private void CreateCollision()
	{
		foreach (Node child in GetChildren())
		{
			if (child is StaticBody3D)
			{
				child.Free();
			}
		}

		this.CreateTrimeshCollision();
	}
}
