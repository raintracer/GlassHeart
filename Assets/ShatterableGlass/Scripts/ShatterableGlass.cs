using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;

public class ShatterableGlass : MonoBehaviour
{

    // !!!
    // Before trying to read or modify code it is strongly recommended to read Readme.pdf.
    // It contains detailed explanations with images and formulas.
    // !!!

    // Sector per side.
    public int Sectors = 3;
    // Figures per sector.
    public int DetailsPerSector = 3;

    // Sectors with area smaller than Area * SimplifyThreshold will be trimmed to simple triangle.
    public float SimplifyThreshold = 0.05f;

    // Generate Glass sides?
    public bool GlassSides = true;
    // Material of that sides.
    public Material GlassSidesMaterial;

    public float GlassThickness = 0.01f;

    // Draw net, but not break?
    public bool ShatterButNotBreak = false;
    // A bit realistic effect, if glass not breakble?
    public bool SlightlyRotateGibs = true;

    // Destroy gibs?
    public bool DestroyGibs = true;
    // After time, seconds.
    public float AfterSeconds = 5f;

    // Move Gibs on a separate layer?
    public bool GibsOnSeparateLayer = false;
    // Index of that layer.
    public int GibsLayer = 0;

    // Maximum force applied to glass gibs.
    public float Force = 100f;

    // Should glass fragments have same parent?
    public bool AdoptFragments = false;

    // Abstract bounds of the glass.
    Vector2[] Bounds = new Vector2[4];
    // Area of the glass. Calculated at Start().
    float Area = 1f;
    // Original glass material. Same will be applied to glass gibs.
    Material GlassMaterial;
    // AudioSource of break sound.
    AudioSource SoundEmitter;

    // Using this for initialization
    void Start()
    {
        // Dimmentions of the glass. Please refer to Figure 2.1. in Readme.pdf
        float u = Mathf.Abs(transform.lossyScale.x / 2f);
        float v = Mathf.Abs(transform.lossyScale.y / 2f);

        // Calculate area.
        Area = u * v;

        // Corners of the glass.
        Bounds[0] = new Vector2(u, v);
        Bounds[1] = new Vector2(-u, v);
        Bounds[2] = new Vector2(-u, -v);
        Bounds[3] = new Vector2(u, -v);

        SoundEmitter = GetComponent<AudioSource>();

        // Check if Renderer and MeshFilter present.
        if (GetComponent<Renderer>() == null || GetComponent<MeshFilter>() == null)
        {
            Debug.LogError(gameObject.name + ": No Renderer and/or MeshFilter components!");
            Destroy(gameObject);
            return;
        }

        // Original glass's material will be applied to glass gibs.
        GlassMaterial = GetComponent<Renderer>().material;

        // Throw an error, if second material required, but not set.
        if (GlassSides && GlassSidesMaterial == null)
        {
            Debug.LogError(gameObject.name + ": GlassSide material must be assigned! Glass will be destroyed.");
            Destroy(gameObject);
        }
    }

    // Function for SendMessage usage.
    // HitPoint is point in local glass coordinates. Typically {0, 0} (center). Force applied towards local z axis.
    public void Shatter2D(Vector2 HitPoint)
    {
        Shatter(HitPoint, transform.forward);
    }

    // Converts Global 3D point to local 2D point and calls Shatter(). Please refer to Figure 2.2.
    public void Shatter3D(ShatterableGlassInfo Inf)
    {

        // Check if any of parents have non-{1, 1, 1} scale.
        // If any of parents have wrong scale conversion and shattering will be incorrect.
        Transform Parent = gameObject.transform.parent;

        bool Sucsess = true;

        while (Parent != null)
        {
            if (Parent.localScale.x != 1f || Parent.localScale.y != 1f || Parent.localScale.y != 1f)
                Sucsess = false;
            Parent = Parent.parent;
        }

        // Using lossyScale may cause problems, throw warning.
        if (!Sucsess)
            Debug.LogWarning(gameObject.name + ": scale of all parents in hierarchy recommended to be {1, 1, 1}. Glass may shatter weirdly.");

        // There we use triangle to determine 2D point. Please refer to figure 2.2.
        // Local bottom left and bottom right points.
        Vector3 A = transform.TransformPoint(new Vector3(-0.5f, -0.5f));
        Vector3 B = transform.TransformPoint(new Vector3(0.5f, -0.5f));

        // Sides of triangle.
        float b = Vector3.Distance(Inf.HitPoint, A);
        float c = Vector3.Distance(B, A);
        float a = Vector3.Distance(Inf.HitPoint, B);

        // Half perimeter.
        float p = (a + b + c) / 2f;

        // Area.
        float S = Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));

        // Height of the triangle.
        float h = 2 / c * S;

        // Calculate u(x) using Pythagorean theorem.
        float u = Mathf.Sqrt(b * b - h * h);

        h -= Mathf.Abs(transform.lossyScale.y / 2f);
        u -= Mathf.Abs(transform.lossyScale.x / 2f);

        // Finaly, call Shatter().
        Shatter(new Vector2(u * Mathf.Sign(transform.lossyScale.x), h * Mathf.Sign(transform.lossyScale.y)), Inf.HitDirrection);
    }

    // Main function. HitPoint is local glass's coordinates.
    public void Shatter(Vector2 HitPoint, Vector3 ForceDirrection)
    {
        // 4 BaseLines for 4 courners. Plus Sectors per side (4 sides). 
        int BaseLinesCount = 4 + (Sectors - 1) * 4;
        BaseLine[] BaseLines = new BaseLine[BaseLinesCount];

        // For each side:
        for (int j = 0; j < 4; j++)
        {
            // BaseLine from HitPoint to corner.
            BaseLines[j * Sectors] = new BaseLine(HitPoint, Bounds[j], DetailsPerSector);

            // Calculate Ratio. For example for FiguresPerSector == 4 Ratio must increase by 0.25;
            float Margin = 1f / Sectors;
            float Ratio = Margin;

            for (int i = 1; i < Sectors; i++)
            {
                // Rest BaseLines per side.
                BaseLines[j * Sectors + i] = new BaseLine(HitPoint, Vector2.Lerp(Bounds[j], Bounds[(j + 1) % 4], Ratio), DetailsPerSector);
                Ratio += Margin;
            }
        }

        // Figures.
        List<Figure> Figures = new List<Figure>();

        // 2 BaseLines constructs sector. usually FiguresPerSector Figures generated per sector.
        // But if area of sector is relatively small it is replaced by single triangle.
        // Please refer to figure 2.3.

        // For Each BaseLine:
        for (int i = 0; i < BaseLinesCount; i++)
        {
            //Index of next BaseLine. In last itteration used first BaseLine.
            int k = (i + 1) % BaseLinesCount;

            // Calculate sector area.
            // Sides of triangle.
            float a = Vector2.Distance(HitPoint, BaseLines[i].Points[DetailsPerSector]);
            float b = Vector2.Distance(HitPoint, BaseLines[k].Points[DetailsPerSector]);
            float c = Vector2.Distance(BaseLines[i].Points[DetailsPerSector], BaseLines[k].Points[DetailsPerSector]);

            // Half perimeter.
            float p = (a + b + c) * 0.5f;

            // Area by Heron's formula.
            float S = Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));

            // If Area is smaller than Sqare * Threshold.
            if (S < Area * SimplifyThreshold)
                Figures.Add(new Figure(new Vector2[] { BaseLines[i].Points[DetailsPerSector], BaseLines[k].Points[DetailsPerSector], HitPoint }, DetailsPerSector / 2));
            else
            {
                //First triangle Generation.
                Figures.Add(new Figure(new Vector2[] { BaseLines[i].Points[1], BaseLines[k].Points[1], HitPoint }, 1));

                // Rest trapeze generation.
                for (int j = 1; j < DetailsPerSector; j++)
                {
                    // 4 points of trapeze.
                    Vector2[] Points = new Vector2[4];

                    Points[0] = BaseLines[i].Points[j];
                    Points[1] = BaseLines[(i + 1) % BaseLinesCount].Points[j];
                    Points[2] = BaseLines[i].Points[j + 1];
                    Points[3] = BaseLines[(i + 1) % BaseLinesCount].Points[j + 1];

                    Figures.Add(new Figure(Points, i + 1));
                }
            }
        }

        // Generate Mesh for each Figure.        
        foreach (Figure Fig in Figures)
        {
            GameObject Obj = new GameObject("GlassGib");
            // Apply original glass's transform.
            Obj.transform.rotation = transform.rotation;
            Obj.transform.position = transform.position;
            if (AdoptFragments)
                Obj.transform.parent = transform.parent;

            MeshFilter Filter = Obj.AddComponent<MeshFilter>();

            // Create gib's renderer and assign material(s).
            MeshRenderer Rnd = Obj.AddComponent<MeshRenderer>();

            if (GlassSides)
                // First Material is original glass's material, secnd is GlassSideMaterial.
                Rnd.materials = new Material[2] { GlassMaterial, GlassSidesMaterial };
            else
                Rnd.material = GlassMaterial;

            Mesh Model = Fig.GenerateMesh(GlassSides, GlassThickness / 2f, new Vector2(transform.lossyScale.x, transform.lossyScale.y));
            Filter.sharedMesh = Model;

            // if gib must be Rigidbody:
            if (!ShatterButNotBreak)
            {
                Fig.GenerateCollider(GlassThickness, Obj);

                Rigidbody Rig = Obj.AddComponent<Rigidbody>();

                // Apply Force. Closer to HitPoint - greater force.
                Rig.AddForce(ForceDirrection * Random.Range(Force, Force * 1.5f) / Fig.ForceScale);

                if (GibsOnSeparateLayer)
                    Obj.layer = GibsLayer;

                if (DestroyGibs)
                {
                    float AfterSecondsMargin = AfterSeconds * 0.1f;
                    Destroy(Obj, Random.Range(AfterSeconds - AfterSecondsMargin, AfterSeconds + AfterSecondsMargin));
                }
            }
            else if (SlightlyRotateGibs)
                // Slightly rotate gib. 
                // Rotation around glass's center.
                Obj.transform.Rotate(new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)));

        }

        // Play sound, if AudioSource component is attached.
        if (SoundEmitter)
            SoundEmitter.Play();

        // Remove useless components
        Destroy(GetComponent<Renderer>());
        Destroy(GetComponent<MeshFilter>());
        Destroy(GetComponent<ShatterableGlass>());

        if (ShatterButNotBreak)
            // Stop access by Gun.
            gameObject.tag = "Untagged";
        else
        {
            //Stop colliding
            Destroy(GetComponent<MeshCollider>());
            // Destroy original glass after sound stops or now, if AudioSource not set.
            if (SoundEmitter)
            {
                if (SoundEmitter.clip)
                    Destroy(gameObject, SoundEmitter.clip.length);
                else
                    Debug.Log(gameObject.name + ": AudioSource component is present, but SoundClip is not set.");
            }
            else
                Destroy(gameObject);
        }
    }

    // Figure class. Figure consists of 3(triangle) or 4(trapeze) points.
    // Multiple Figures created betwin 2 BaseLines.
    class Figure
    {
        public Vector2[] Points;
        // Closer to HitPoint lower should be this value. Applied force will be divided by this value.
        public int ForceScale;

        // Constructor.
        public Figure(Vector2[] Points, int ForceScale)
        {
            this.Points = Points;
            this.ForceScale = ForceScale;
        }

        // Generates BoxCollider for GameObject Obj.
        // Method made for triangles but can be applied to trapeze since it consists of 2 triangles.
        // Please refer to Figure 2.7.
        public void GenerateCollider(float GlassThickness, GameObject Obj)
        {
            BoxCollider Col = Obj.AddComponent<BoxCollider>();

            // Sides of triangle. Please refer to figure XX.
            float a = Vector2.Distance(Points[2], Points[0]);
            float b = Vector2.Distance(Points[2], Points[1]);
            float c = Vector2.Distance(Points[1], Points[0]);

            // Perimeter
            float p = a + b + c;

            // Incircle coordinates.
            float ox = (a * Points[0].x + b * Points[1].x + c * Points[2].x) / p;
            float oy = (a * Points[0].y + b * Points[1].y + c * Points[2].y) / p;

            // Radiuse of that incircle.
            p /= 2f;

            float r = Mathf.Sqrt(((p - a) * (p - b) * (p - c)) / p);

            // Insqare of that circle.
            r *= Mathf.Sqrt(2);

            // Apply calculations.
            Col.center = new Vector3(ox, oy, 0f);
            Col.size = new Vector3(r, r, GlassThickness);
        }

        // Generates Mesh from Poins[].
        // Mesh UV mapped to original glass.
        // Please refer to Figure 2.6.
        public Mesh GenerateMesh(bool GenerateGlassSides, float GlassHalfThickness, Vector2 UVScale)
        {
            Mesh Model = new Mesh();

            Model.name = "GlassGib";

            // If sides needs to be generated, must will consist of 2 submesh. Each submesh rendered with corresponding material.
            if (GenerateGlassSides)
                Model.subMeshCount = 2;

            bool IsTriangle = Points.Length == 3;

            // Size of Vertices[] depends on figure: 3 vertices for triangle or 4 for trapeze.
            // Plus 6 or 8 for sides.
            // Please refer to Figure 2.5.
            Vector3[] Vertices = new Vector3[IsTriangle ? GenerateGlassSides ? 9 : 3 : GenerateGlassSides ? 12 : 4];
            // Size of Map[] MUST be the same although we assign only 3 or 4 vertices.
            Vector2[] Map = new Vector2[Vertices.Length];

            for (int i = 0; i < Points.Length; i++)
            {
                Vertices[i] = Points[i];
                // As UV lies within {0, 1}, Point must be downscaled.
                Map[i] = new Vector2(Points[i].x / UVScale.x, Points[i].y / UVScale.y) + new Vector2(0.5f, 0.5f);
            }

            // Triangles represented as triplet of vertex indices.
            int[] MainTriangles;

            if (IsTriangle)
                MainTriangles = new int[3] { 2, 1, 0 }; // Indexes are always the same for each specific case.           
            else
                MainTriangles = new int[6] { 0, 1, 2, 3, 2, 1 };

            if (GenerateGlassSides)
            {
                // Triangles for sides. 2 triangle per edge.
                int[] TrianglesSide;

                if (IsTriangle)
                {
                    for (int i = 0; i < 3; i++)
                        GlassSideVertex(Points[i], ref Vertices[i * 2 + 3], ref Vertices[i * 2 + 4], GlassHalfThickness);

                    TrianglesSide = new int[18] { 3, 4, 5, 4, 6, 5, 3, 4, 7, 7, 8, 4, 5, 6, 8, 8, 7, 5 };
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                        GlassSideVertex(Points[i], ref Vertices[i * 2 + 4], ref Vertices[i * 2 + 5], GlassHalfThickness);

                    TrianglesSide = new int[24] { 7, 5, 4, 6, 7, 4, 11, 7, 6, 10, 11, 6, 10, 11, 9, 9, 8, 10, 8, 9, 5, 8, 4, 5 };

                }
                // Apply Vertices and triangles.
                Model.vertices = Vertices;
                Model.SetTriangles(MainTriangles, 0);
                Model.SetTriangles(TrianglesSide, 1);
            }
            else
            {
                // Apply Vertices and triangles.
                Model.vertices = Vertices;
                Model.triangles = MainTriangles;
            }

            // Apply UV map.
            Model.uv = Map;

            return Model;
        }

        // Offsets point Ref by +-GlassHalfThickness at z axis.
        void GlassSideVertex(Vector2 Ref, ref Vector3 A, ref Vector3 B, float GlassHalfThickness)
        {
            A = new Vector3(Ref.x, Ref.y, GlassHalfThickness);
            B = new Vector3(Ref.x, Ref.y, -GlassHalfThickness);
        }
    }

    // Baseline class. Basically divides line between HotPoint and End by Count.
    // Please refer to Figure 2.3.
    class BaseLine
    {
        public Vector2[] Points;

        public BaseLine(Vector2 HitPoint, Vector2 End, int Count)
        {
            Points = new Vector2[Count + 1];
            Points[0] = HitPoint;
            Points[Count] = End;

            float Margin = 1f / Count;
            float Ratio = Margin;

            // Roll calculation. All calculations in radians. Please refer to figure 2.4.

            // Angle of line at first quadrant.
            float Angle = Mathf.Atan2(Mathf.Max(HitPoint.y, End.y) - Mathf.Min(HitPoint.y, End.y), Mathf.Max(End.x, HitPoint.x) - Mathf.Min(HitPoint.x, End.x));

            // 45deg. Maximum angle.
            float Pi4 = Mathf.PI / 4f;
            // 90 deg.
            float Pi2 = Mathf.PI / 2f;

            // Invert angle relatively to 45 deg.
            if (Angle > Pi4)
            {
                Angle = Pi2 - Angle;
            }
            // Inverse interpolation. 0 deg - 0, 45 deg - 1;
            float Roll = Angle / Pi4;

            for (int i = 0; i < Count - 1; i++)
            {
                // Interpolate between HitPoint and End by Ratio. Ratio depends on Roll.
                Points[i + 1] = Vector2.Lerp(HitPoint, End, Ratio * Mathf.Lerp(1f, Mathf.Sqrt(2) / 2f, Roll));
                Ratio += Margin;
            }
        }
    }
}

// Class to be use as SendMessage argument.
public class ShatterableGlassInfo
{
    public Vector3 HitPoint;
    // Force will be multiplied by HitDirrection vector.
    public Vector3 HitDirrection;

    public ShatterableGlassInfo(Vector3 HitPoint, Vector3 HitDirrection)
    {
        this.HitPoint = HitPoint;
        this.HitDirrection = HitDirrection;
    }
}