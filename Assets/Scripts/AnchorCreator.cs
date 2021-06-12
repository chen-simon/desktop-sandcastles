using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

// Modified from AR Core Foundation Samples
[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARRaycastManager))]
public class AnchorCreator : MonoBehaviour
{
    public GameObject wall;
    public GameObject tower;

    //bool isTowerNext = false;
    bool isSwiping = false;
    bool hasTappedItem = false;


    Pose startPose;
    Vector2 startTouchPos;
    Transform objectToGrow;

    readonly float GROW_HEIGHT = 0.1f;
    readonly float SWIPE_THRESH = 40;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    List<ARAnchor> m_Anchors = new List<ARAnchor>();
    ARRaycastManager m_RaycastManager;
    ARAnchorManager m_AnchorManager;
    Camera arCamera;

    public void RemoveAllAnchors()
    {
        foreach (var anchor in m_Anchors)
        {
            Destroy(anchor.gameObject);
        }
        m_Anchors.Clear();
    }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>();
        arCamera = GetComponentInChildren<Camera>(); // Gets the attached AR Camera from the AR Session Origin
    }

    ARAnchor CreateAnchor(in ARRaycastHit hit)
    {
        ARAnchor anchor = null;

        GameObject prefab = tower;
        Pose spawnPose = hit.pose;
        if (isSwiping) {
            prefab = wall;
            spawnPose = startPose;
            spawnPose.position = (startPose.position + hit.pose.position) / 2;
        }

        // If we hit a plane, try to "attach" the anchor to the plane
        if (hit.trackable is ARPlane plane)
        {
            var planeManager = GetComponent<ARPlaneManager>();
            if (planeManager)
            {
                m_AnchorManager.anchorPrefab = prefab;
                anchor = m_AnchorManager.AttachAnchor(plane, spawnPose);
                return anchor;
            }
        }

        // Note: the anchor can be anywhere in the scene hierarchy
        var gameObject = Instantiate(prefab, spawnPose.position, spawnPose.rotation);

        // Make sure the new GameObject has an ARAnchor component
        anchor = gameObject.GetComponent<ARAnchor>();
        if (anchor == null)
        {
            anchor = gameObject.AddComponent<ARAnchor>();
        }

        return anchor;
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        var touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            hasTappedItem = false;
            isSwiping = false;

            //// Raycast against planes and feature points
            //const TrackableType trackableTypes = TrackableType.All;
            ////TrackableType.FeaturePoint |
            ////TrackableType.PlaneWithinPolygon;

            RaycastHit physicsHit;
            Ray ray = arCamera.ScreenPointToRay(touch.position);

            // Perform the raycast
            if (Physics.Raycast(ray, out physicsHit) && !physicsHit.transform.GetComponent<ARPlane>())
            {
                objectToGrow = physicsHit.transform;
                hasTappedItem = true;
            }
            if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.All))
            {
                // Raycast hits are sorted by distance, so the first one will be the closest hit.
                var hit = s_Hits[0];

                // Remember location of tap in case this is a swipe
                startPose = hit.pose;
                startTouchPos = touch.position;
            }
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            if ((touch.position - startTouchPos).magnitude > SWIPE_THRESH)
            {
                isSwiping = true;
            }

            if (hasTappedItem && !isSwiping)
            {
                objectToGrow.localScale = new Vector3(objectToGrow.localScale.x, objectToGrow.localScale.y + GROW_HEIGHT, objectToGrow.localScale.z);
                objectToGrow.localPosition = new Vector3(objectToGrow.localPosition.x, objectToGrow.localPosition.y + GROW_HEIGHT / 2, objectToGrow.localPosition.z);
            }
            else
            {
                if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.All))
                {
                    // Raycast hits are sorted by distance, so the first one will be the closest hit.
                    var hit = s_Hits[0];

                    // Create a new anchor
                    var anchor = CreateAnchor(hit);
                    if (anchor)
                    {
                        // Remember the anchor so we can remove it later.
                        m_Anchors.Add(anchor);
                    }
                }
            }
        }
    }
}
