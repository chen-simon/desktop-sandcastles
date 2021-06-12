﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

// Modified from AR Core Foundation Samples
[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARRaycastManager))]
public class AnchorCreator : MonoBehaviour {
    public GameObject wall;
    public GameObject tower;

    bool isTowerNext = false;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    List<ARAnchor> m_Anchors = new List<ARAnchor>();
    ARRaycastManager m_RaycastManager;
    ARAnchorManager m_AnchorManager;
    Camera arCamera;

    public void RemoveAllAnchors() {
        foreach (var anchor in m_Anchors) {
            Destroy(anchor.gameObject);
        }
        m_Anchors.Clear();
    }

    void Awake() {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>();
        arCamera = GetComponentInChildren<Camera>(); // Gets the attached AR Camera from the AR Session Origin
    }

    ARAnchor CreateAnchor(in ARRaycastHit hit) {
        ARAnchor anchor = null;

        GameObject prefab = isTowerNext ? tower : wall;
        isTowerNext = !isTowerNext;

        // If we hit a plane, try to "attach" the anchor to the plane
        if (hit.trackable is ARPlane plane) {
            var planeManager = GetComponent<ARPlaneManager>();
            if (planeManager) {
                m_AnchorManager.anchorPrefab = prefab;
                anchor = m_AnchorManager.AttachAnchor(plane, hit.pose);
                return anchor;
            }
        }

        // Note: the anchor can be anywhere in the scene hierarchy
        var gameObject = Instantiate(prefab, hit.pose.position, hit.pose.rotation);

        // Make sure the new GameObject has an ARAnchor component
        anchor = gameObject.GetComponent<ARAnchor>();
        if (anchor == null) {
            anchor = gameObject.AddComponent<ARAnchor>();
        }

        return anchor;
    }

    void Update() {
        if (Input.touchCount == 0)
            return;

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
            return;

        //// Raycast against planes and feature points
        //const TrackableType trackableTypes = TrackableType.All;
        ////TrackableType.FeaturePoint |
        ////TrackableType.PlaneWithinPolygon;

        // Perform the raycast
        if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.All)) {
            // Raycast hits are sorted by distance, so the first one will be the closest hit.
            var hit = s_Hits[0];

            // Create a new anchor
            var anchor = CreateAnchor(hit);
            if (anchor) {
                // Remember the anchor so we can remove it later.
                m_Anchors.Add(anchor);
            }
        }
    }
}
