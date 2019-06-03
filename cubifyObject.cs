using UnityEngine;

public class cubifyObject : MonoBehaviour {
    public void checkIfMeshesOverlap(int sqrResolution, BoxCollider totalVolumeCollider) {
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

        //finds all voxels that overlap with the mesh
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