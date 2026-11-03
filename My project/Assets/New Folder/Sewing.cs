using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.GraphToolkit.Editor;
using System;
using Unity.VisualScripting;
using UnityEngine.InputSystem;


public class Sewing : MonoBehaviour
{

    [Header("Sewing Settings")]
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform sewingPoint;
    [SerializeField] LayerMask clothPoints;
    [SerializeField] int tailNodes = 40;
    [SerializeField] float tailSegmentLength = .2f;
    [SerializeField] float tailSegmentWidth = .01f;
    [SerializeField] float spriteRotaionOffset = 0f;
    [SerializeField] float rotationSpeed = 15f;
    [SerializeField] Transform needleSprite;


    //the sewing mechanic is jsut a lijked set of nodes, slowly influincing the one behind it
    private struct Node
    {
        public Vector2 position;
    }

    private Node[] nodesArray;
    private bool isSewing = false;
    private Vector2 lastMouseWorldPos;
    private Vector2 lastNeedleDir;
    private float targetAngel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nodesArray = new Node[tailNodes];
        Vector2 initialPosition = Vector2.zero;
        if (Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            float zDistance = Mathf.Abs(Camera.main.transform.position.z);
            mouseScreen.z = zDistance;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
            initialPosition = new Vector2(mouseWorld.x, mouseWorld.y);
            lastNeedleDir = Vector2.right;
        }

        lastMouseWorldPos = initialPosition;
        for (int i = 0; i < tailNodes; i++)
        {
            nodesArray[i].position = initialPosition;
        }
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.GetComponent<LineRenderer>();
        
            if (lineRenderer == null)
            {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        lineRenderer.positionCount = tailNodes;
        lineRenderer.startWidth = tailSegmentWidth;
        lineRenderer.endWidth = tailSegmentWidth;
        targetAngel = Mathf.Atan2(lastNeedleDir.y, lastNeedleDir.x) * Mathf.Rad2Deg;


    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isSewing = true;
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            float zDist = -Camera.main.transform.position.z;
            lastMouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDist));
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isSewing = false;
        }

        if (isSewing && Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            float zDistance = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
            Vector2 worldMousePos = new Vector2(mouseWorld.x, mouseWorld.y);

            Vector2 movementDelta = worldMousePos - lastMouseWorldPos;
            if (movementDelta.magnitude >.00005f)
            {
                lastNeedleDir = movementDelta.normalized;
                targetAngel = Mathf.Atan2(lastNeedleDir.y, lastNeedleDir.x) * Mathf.Rad2Deg;
            }
            

            
            //record last frame mous position, then track new one and make needle rotate to face the direction of movement
            lastNeedleDir = needleSprite.position;
            lastMouseWorldPos = mouseWorld;
            needleSprite.position = worldMousePos;
            //rotate the needle sprite to face the direction of movement


            sewingPoint.position = worldMousePos;

            nodesArray[0] = new Node
            {
                position = worldMousePos
            };

            Vector3[] linePositions = new Vector3[tailNodes];
            linePositions[0] = new Vector3(worldMousePos.x, worldMousePos.y, 0);

            for (int i = 1; i < tailNodes; i++)
            {
                float nodeAngle = Mathf.Atan2(nodesArray[i - 1].position.y - nodesArray[i].position.y, nodesArray[i - 1].position.x - nodesArray[i].position.x);
                linePositions[i] = new Vector3(nodesArray[i].position.x, nodesArray[i].position.y, 0);
                nodesArray[i] = new Node
                {
                    position = new Vector2(
                        nodesArray[i - 1].position.x - Mathf.Cos(nodeAngle) * tailSegmentLength, 
                        nodesArray[i - 1].position.y - Mathf.Sin(nodeAngle) * tailSegmentLength
                    )
                };


                Collider2D hitCollider = Physics2D.OverlapPoint(nodesArray[i].position, clothPoints);

                if (hitCollider != null)
                {
                    if (i == 1)
                    {
                        //anchor the needle, allow for more string
                    }
                    else if (i == tailNodes - 1)
                    {
                        //create a knot
                    }
                    Debug.Log("Hit cloth point: " + hitCollider.name);
                }

                linePositions[i] = new Vector3(nodesArray[i].position.x, nodesArray[i].position.y, 0);
            }
            lineRenderer.SetPositions(linePositions);
        }
    }
}
