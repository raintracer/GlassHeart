using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RowShatter : MonoBehaviour
{

    private GameObject CameraObject;
    private Camera RenderCamera;
    private GameObject QuadObject;
    private MeshRenderer QuadRenderer;
    private MeshFilter QuadFilter;

    public RenderTexture BaseRenderTexture;
    public Material BaseRenderMaterial;

    private bool TextureIsRendered = false;

    
    void Start()
    {

        // Get key components
        CameraObject = transform.Find("Camera").gameObject;
        RenderCamera = CameraObject.GetComponent<Camera>();
        QuadObject = transform.Find("Quad").gameObject;
        QuadRenderer = QuadObject.GetComponent<MeshRenderer>();
        QuadFilter = QuadObject.GetComponent<MeshFilter>();

        // Assign the mesh
        SetQuadMesh();

        // Set custom camera callbacks
        Camera.onPreCull += CustomOnPreCull;
        Camera.onPostRender += CustomOnPostRender;


    }

    private void SetQuadMesh()
    {

        // Update the mesh for the quad
        Vector3[] verts = new Vector3[4] {
            new Vector3(0, 0),
            new Vector3(0, 0.167f),
            new Vector3(1f, 0.167f),
            new Vector3(1f, 0)
        };
        int[] tris = new int[6] { 1, 2, 3, 1, 3, 0 };
        Vector2[] uvs = new Vector2[] {
            verts[0], verts[1], verts[2], verts[3]
        };

        // Update MeshFilter mesh
        QuadFilter.sharedMesh.Clear();
        QuadFilter.sharedMesh.vertices = verts;
        QuadFilter.sharedMesh.triangles = tris;
        QuadFilter.sharedMesh.uv = uvs;
        QuadFilter.sharedMesh.RecalculateNormals();

        // Update MeshCollider mesh
        MeshCollider MC = QuadObject.GetComponent<MeshCollider>();
        MC.sharedMesh.Clear();
        MC.sharedMesh.vertices = verts;
        MC.sharedMesh.triangles = tris;
        MC.sharedMesh.uv = uvs;
        MC.sharedMesh.RecalculateNormals();

    }

    IEnumerator DestroyTimer()
    {
        yield return new WaitForSeconds(4f);
        Instantiate(Resources.Load<GameObject>("Row Shatter"), new Vector3(8f, 8f, 0f), Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Camera.onPreCull -= CustomOnPreCull;
        Camera.onPostRender -= CustomOnPostRender;
    }

    private void CustomOnPreCull(Camera cam)
    {
        Debug.Log("Precull called.");
        if (cam != RenderCamera) return;
        if (TextureIsRendered) return;
        Debug.Log("Initial PreRender");
        QuadObject.SetActive(false);
    }

    private void CustomOnPostRender(Camera cam)
    {

        Debug.Log("Postrender called.");
        
        if (cam != RenderCamera) return;
        if (TextureIsRendered) return;
        TextureIsRendered = true;

        Debug.Log("Initial PostRender");

        // Read pixels to texture
        RenderTexture rt = RenderCamera.targetTexture; 
        Texture2D renderResult = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        Rect rect = new Rect(0, 0, rt.width, rt.height);
        renderResult.ReadPixels(rect, 0, 0);

        // Save the texture to the quad object material
        QuadRenderer.material.SetTexture("_MainTex", renderResult);

        // Re-enable the quad object
        QuadObject.SetActive(true);

        // Destroy the camera
        Destroy(RenderCamera);

        // Break the glass
        QuadObject.GetComponent<ShatterableGlass>().Shatter2D(Vector2.zero);

        // Time out to destruction
        StartCoroutine(DestroyTimer());

    }

}
