﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class cubify : EditorWindow {

    private int resolution = 10;
    private Object cubifyObject;

    [MenuItem("GameObject/Cubify", false, 10)]
    public static void runCubify() {
        openCubifyWindow();
    }

    public static void openCubifyWindow() {
        GetWindow<cubify>("Cubify");
        GetWindow<cubify>("Cubify").cubifyObject = Selection.activeObject;
    }

    void OnGUI() {
        EditorGUILayout.BeginHorizontal();
        cubifyObject = EditorGUILayout.ObjectField(cubifyObject, typeof(Object), true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        resolution = EditorGUILayout.IntField(resolution);
        if (GUILayout.Button("Generate")) {
            generate();
        }
        if (GUILayout.Button("Delete")) {
            delete();
        }
        EditorGUILayout.EndHorizontal();
    }

    void delete() {
        DestroyImmediate(GameObject.Find("Total Volume"));
        DestroyImmediate(GameObject.Find("SavedVoxelParent"));
    }

    void generate() {
        //get center & size of mesh group
        GameObject cubifyObjectToGameObject = cubifyObject as GameObject;
        Vector3 size = getBounds(cubifyObjectToGameObject.transform).size;
        Vector3 center = getBounds(cubifyObjectToGameObject.transform).center;

        //get longest side and divide it into equal parts of that length
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
        GameObject voxelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        createVoxelGrid(startLocation, shiftVoxelOffset, voxelObj, totalVolume, voxelSize, maxDimension);

        cubifyObjectToGameObject.GetComponent<cubifyObject>().checkIfMeshesOverlap(Mathf.CeilToInt(Mathf.Pow(resolution, 3)));
        //destroy the original voxel
        DestroyImmediate(voxelObj);
        DestroyImmediate(totalVolume);
    }

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

    //gets bounding volume of model
    public static Bounds getBounds(Transform loadedTransform) {
        Bounds bounds = new Bounds(getGroupedMeshCenter(loadedTransform), Vector3.zero); //center the bounds object on the model
        foreach (Renderer renderer in loadedTransform.GetComponentsInChildren<Renderer>()) //iterates over all child renderers and adjusts the bounds to fit over all of them
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    // gets average center point for bounds, used as a point to grow bounds outwards from
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
}