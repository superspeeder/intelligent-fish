using Godot;
using System;

[Tool]
public partial class terrainGenerator : MeshInstance3D
{
    // These [Export] attributes let you tweak the values right in the Godot Inspector
    [Export] public int Width = 50;
    [Export] public int Depth = 50;
    [Export] public float HeightMultiplier = 15f;
    [Export] public FastNoiseLite Noise;

    public override void _Ready()
    {
        // Fallback just in case you forget to assign a noise profile in the editor
        if (Noise == null)
        {
            Noise = new FastNoiseLite();
            Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            Noise.Frequency = 0.03f; // Lower frequency means smoother, rolling underwater hills
        }

        GenerateOceanFloor();
    }

    private void GenerateOceanFloor()
    {
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Generate Vertices
        for (int z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                // Sample the noise to get our height (Y)
                float y = Noise.GetNoise2D(x, z) * HeightMultiplier;
                
                // Set the UV map so textures stretch correctly later
                st.SetUV(new Vector2((float)x / Width, (float)z / Depth));
                
                // Add the vertex, centering the mesh around the Node's origin
                st.AddVertex(new Vector3(x - (Width / 2f), y, z - (Depth / 2f)));
            }
        }

        // Generate Indices
        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                int currentVertex = x + z * (Width + 1);

                // First triangle of the square
                st.AddIndex(currentVertex);
                st.AddIndex(currentVertex + 1);
                st.AddIndex(currentVertex + Width + 1);

                // Second triangle of the square
                st.AddIndex(currentVertex + 1);
                st.AddIndex(currentVertex + Width + 2);
                st.AddIndex(currentVertex + Width + 1);
            }
        }

        // Calculate normals so lighting bounces off the terrain correctly
        st.GenerateNormals();
        
        // Finalize the mesh and assign it to this MeshInstance3D
        this.Mesh = st.Commit();

        CreateCollision();
    }

    private void CreateCollision()
    {
        // This built-in method takes the mesh we just generated and wraps a StaticBody3D 
        // and CollisionShape3D around it perfectly.
        this.CreateTrimeshCollision();
    }
}