using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedFlocking : MonoBehaviour
{
    private Vector2 cursorPos;

    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = dir.x;
            direction.y = dir.y;
            direction.z = dir.z;
            noise_offset = offset;
        }
    }

    public ComputeShader shader;
    
    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    [Range(0, 1)]public float boidSpeedVariation = 1f;
    public Mesh boidMesh;
    public Material boidMaterial;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;
    public float cohesionWeight;
    public float alignmentWeight;
    public float separationWeight;
    public float cursorWeight;

    int kernelHandle;
    ComputeBuffer boidsBuffer;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    Boid[] boidsArray;
    int groupSizeX;
    int numOfBoids;
    Bounds bounds;
    
    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        InitBoids();
        InitShader();
    }

    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            float offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, rot.eulerAngles, offset);
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        shader.SetBuffer(this.kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetVector("flockForward", transform.forward);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", numOfBoids);
        shader.SetFloat("cohesionWeight", cohesionWeight);
        shader.SetFloat("alignmentWeight", alignmentWeight);
        shader.SetFloat("separationWeight", separationWeight);
        shader.SetFloat("cursorWeight", cursorWeight);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    void Update()
    {
        Vector2 mousePos = new Vector2(cursorPos.x, cursorPos.y);
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("cursor", new Vector3(mousePos.x, mousePos.y, 0));

        shader.Dispatch(this.kernelHandle, groupSizeX, 1, 1);

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer);
        
    }

    void OnDestroy()
    {
        if (boidsBuffer != null)
        {
            boidsBuffer.Dispose();
        }

        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
        }
    }

    void OnGUI(){
        Vector3 p = new Vector3();
        Camera mainCamera = Camera.main;
        Event currentEvent = Event.current;
        Vector2 mousePos = new Vector2();

        mousePos.x = currentEvent.mousePosition.x;
        mousePos.y = mainCamera.pixelHeight - currentEvent.mousePosition.y;

        p = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));
        
        cursorPos.x = p.x;
        cursorPos.y = p.y;
    }
    
}

