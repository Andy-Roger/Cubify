using UnityEngine;

//Note place this on object that you want to voxelize, then open the "Cubify" window on the GameObject to use
public class cubifyObject : MonoBehaviour {

    public void checkIfMeshesOverlap(int sqrResolution) {
        Collider[] neighbours;
        var thisCollider = GetComponent<Collider>();
        neighbours = new Collider[sqrResolution];

        if (!thisCollider)
            return; // nothing to do without a Collider attached

        float radius = 3f;
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, neighbours);

        GameObject saveVoxelsGameObject = new GameObject("SavedVoxelParent");
        Transform saveVoxelsParent = saveVoxelsGameObject.transform;
        saveVoxelsParent.transform.position = transform.position;

        Collider totalVolumeCollider = GameObject.Find("Total Volume").GetComponent<BoxCollider>();

        for (int i = 0; i < count; ++i) {
            var collider = neighbours[i];

            if (collider == thisCollider || collider == totalVolumeCollider)
                continue; // skip ourself and total volume collider

            Vector3 otherPosition = collider.gameObject.transform.position;
            Quaternion otherRotation = collider.gameObject.transform.rotation;

            Vector3 direction;
            float distance;

            bool overlapped = Physics.ComputePenetration(
                thisCollider, transform.position, transform.rotation,
                collider, otherPosition, otherRotation,
                out direction, out distance
            );

            if (overlapped) {
                collider.transform.parent = saveVoxelsParent;
            }
        }
    }
}
