using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class cubify : EditorWindow {
    //cubic resolution
    private int resolution = 20;
    public enum VoxelTypes {
        Cube = 0,
        Sphere = 1,
        Cylinder = 2,
        Capsule = 3,
        Custom
    }
    public VoxelTypes voxelType;
    private GameObject cubifyObject;
    private GameObject customVoxel;

    //game object context menu to open Cubify window
    [MenuItem("GameObject/Cubify", false, 10)]
    public static void runCubify() {
        openCubifyWindow();
    }

    //opens Cubify window
    public static void openCubifyWindow() {
        GetWindow<cubify>("Cubify");
        GetWindow<cubify>("Cubify").cubifyObject = (GameObject)Selection.activeObject;
    }

    //Cubify tool window
    void OnGUI() {
        double? timeElapsed = null;

        //pass in the object with mesh that we want to cubify
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GameObject to Cubify");
        cubifyObject = (GameObject)EditorGUILayout.ObjectField(cubifyObject, typeof(Object), true);
        EditorGUILayout.EndHorizontal();

        //voxel type menu
        EditorGUILayout.BeginHorizontal();
        voxelType = (VoxelTypes)EditorGUILayout.EnumPopup("Voxel Type", voxelType);
        EditorGUILayout.EndHorizontal();

        //if custom voxel type, show custom voxel field
        if(voxelType == VoxelTypes.Custom) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Custom Voxel");
            customVoxel = (GameObject)EditorGUILayout.ObjectField(customVoxel, typeof(Object), true);
            EditorGUILayout.EndHorizontal();
            if(customVoxel != null)
                if (!customVoxel.GetComponent<Collider>()) {
                    EditorGUILayout.HelpBox("Add a mesh collider to this object before generating.", MessageType.Warning);
                }
        }

        //cubic resolution, generate, delete
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cubic Resolution");
        resolution = EditorGUILayout.IntField(resolution);
        if (GUILayout.Button("Generate")) {
            if (!cubifyObject.GetComponent<Collider>()) {
                Debug.LogError("Add a Collider to this GameObject before generating");
                return;
            }
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            generate(cubifyObject, getVoxelType(voxelType));

            timeElapsed = stopWatch.Elapsed.TotalSeconds;
        }
        if (GUILayout.Button("Delete")) {
            delete();
        }
        EditorGUILayout.EndHorizontal();

        if(timeElapsed != null)
            Debug.Log("Voxel generation took " + timeElapsed + " sec.");
    }

    //cleans up voxel parents one at a time
    void delete() {
        DestroyImmediate(GameObject.Find("SavedVoxelParent"));
    }

    //main method to start voxel generation
    void generate(GameObject cubifyObject, GameObject voxelObj) {
        //get center & size of mesh group
        var bounds = getBounds(cubifyObject.transform);
        Vector3 size = bounds.size;
        Vector3 center = bounds.center;

        //get longest side
        float maxDimension = Mathf.Max(Mathf.Max(size.x, size.y), size.z);
        
        //generate equally dimensioned box for creating a voxel grid inside
        var totalVolume = new GameObject("Total Volume");
        BoxCollider totalVolumeBoxCol = totalVolume.AddComponent<BoxCollider>();
        totalVolumeBoxCol.center = center;
        totalVolumeBoxCol.size = Vector3.one * maxDimension;

        //precompute constants
        Vector3 voxelSize = totalVolumeBoxCol.size / resolution;
        Vector3 startLocation = center - (size / 2);
        Vector3 shiftVoxelOffset = voxelSize / 2;

        //create voxel grid
        createVoxelGrid(startLocation, shiftVoxelOffset, voxelObj, totalVolume, voxelSize, maxDimension);

        //add "cubifyObject.cs" to mesh scene instance to detect overlapping voxels
        cubifyObject cubifyObjectComponent = cubifyObject.GetComponent<cubifyObject>();
        if (!cubifyObjectComponent)
            cubifyObjectComponent = cubifyObject.AddComponent<cubifyObject>();
        cubifyObjectComponent.checkIfMeshesOverlap(Mathf.CeilToInt(Mathf.Pow(resolution, 3)), totalVolumeBoxCol);

        //clean up the scene objects after generation
        if(voxelType != VoxelTypes.Custom) DestroyImmediate(voxelObj);
        DestroyImmediate(totalVolume);
        DestroyImmediate(cubifyObjectComponent);
    }

    //generate voxel grid
    void createVoxelGrid(Vector3 startLocation, Vector3 shiftVoxelOffset, GameObject voxelObj, GameObject totalVolume, Vector3 voxelSize, float maxDimension) {
        for (int x = 0; x < resolution; x++) {
            for (int y = 0; y < resolution; y++) {
                for (int z = 0; z < resolution; z++) {
                    Vector3 normalizeOffset = ((new Vector3(x, y, z) / resolution) * maxDimension);
                    Vector3 location = startLocation + normalizeOffset;
                    Vector3 offsetLocation = location + shiftVoxelOffset;
                    var voxel = Instantiate(voxelObj, offsetLocation, Quaternion.identity, totalVolume.transform);
                    voxel.transform.localScale = (voxelSize / maxDimension) * maxDimension;
                }
            }
        }
    }

    //grow a volume box over the total mesh
    public static Bounds getBounds(Transform loadedTransform) {
        Bounds bounds = new Bounds(getGroupedMeshCenter(loadedTransform), Vector3.zero); //center the bounds object on the model
        foreach (Renderer renderer in loadedTransform.GetComponentsInChildren<Renderer>()) //iterates over all child renderers and adjusts the bounds to fit over all of them
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    // gets average center point for bounds to center the voxel grid
    private static Vector3 getGroupedMeshCenter(Transform groupedMeshParent) {
        Vector3 vertSum = Vector3.zero;
        int count = 0;
        foreach (MeshFilter filter in groupedMeshParent.GetComponentsInChildren<MeshFilter>())
            foreach (Vector3 pos in filter.sharedMesh.vertices) {
                vertSum += pos;
                count++;
            }
        if (count == 0)
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in groupedMeshParent.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach (Vector3 pos in skinnedMeshRenderer.sharedMesh.vertices) {
                    vertSum += pos;
                    count++;
                }
        return groupedMeshParent.TransformPoint(vertSum /= count);
    }

    //this method acts as a filter of primitive types, dont want quads and planes
    GameObject getVoxelType(VoxelTypes option) {
        switch (option) {
            case VoxelTypes.Cube:
                return GameObject.CreatePrimitive(PrimitiveType.Cube);
            case VoxelTypes.Sphere:
                return GameObject.CreatePrimitive(PrimitiveType.Sphere);
            case VoxelTypes.Cylinder:
                return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            case VoxelTypes.Capsule:
                return GameObject.CreatePrimitive(PrimitiveType.Capsule);
            case VoxelTypes.Custom:
                return customVoxel;
            default:
                return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }
}
