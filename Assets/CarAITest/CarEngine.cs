using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour
{
    public Transform path;
    public float maxSteerAngle = 45f;
    public float turnSpeed = 5f;
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelBL;
    public WheelCollider wheelBR;
    public float maxMotorTorque = 80f;
    public float maxBrakeTorque = 150f;
    public float currentSpeed;
    public float maxSpeed = 100f;
    public Vector3 centerOfMass;
    public bool isBraking = false;
    public Texture2D textureNormal;
    public Texture2D textureBraking;
    public Renderer carRenderer;

    [Header("Sensors")]
    public float sensorLength = 3f;
    public Vector3 frontSensorPosition = new Vector3(0f, 0.5f, 0.5f);
    public float frontSideSensorPosition = 0.75f;
    public float frontSensorAngle = 30f;

    // 노드 관련 선언
    private List<Transform> nodes;
    private int currentNode = 0;
    private bool avoiding = false;
    private float targetSteerAngle = 0;

    private void Start()
    {
        GetComponent<Rigidbody>().centerOfMass = centerOfMass;

        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        // 점 생성
        for(int i = 0; i < pathTransforms.Length; i++){
            if(pathTransforms[i] != path.transform) {
                nodes.Add(pathTransforms[i]);
            }
        }
    }

    private void FixedUpdate()
    {
        //CarStopSensors();
        Sensors();
        Braking();
        ApplySteer();
        Drive();
        CheckWaypointDistance();
        LerpToSteerAngle();
    }

    private void Sensors() // 물체 감지 센서
    {
        RaycastHit hit;
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.z;
        sensorStartPos += transform.up * frontSensorPosition.y;
        float avoidMultiplier = 0;
        avoiding = false;

        // front right sensor
        sensorStartPos += transform.right * frontSideSensorPosition;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isBraking = true;
                    Debug.Log("차감지");
                }
                else
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    avoidMultiplier -= 1f;
                }
            }
        }

        // front right angle sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isBraking = true;
                    Debug.Log("차감지");
                }
                else
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    avoidMultiplier -= 0.5f;
                }
            }
        }

        // front left sensor 
        sensorStartPos -= transform.right * frontSideSensorPosition * 2;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isBraking = true;
                    Debug.Log("차감지");
                }
                else
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    avoidMultiplier += 1f;
                }
            }
        }

        // front left angle sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isBraking = true;
                    Debug.Log("차감지");
                }
                else
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    avoidMultiplier += 0.5f;
                }
            }
        }

        // front center sensor - avoid
        if (avoidMultiplier == 0)
        {
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (!hit.collider.CompareTag("Terrain"))
                {
                    if (hit.collider.CompareTag("Car"))
                    {
                        isBraking = true;
                        Debug.Log("차감지");
                    }
                    else
                    {
                        Debug.DrawLine(sensorStartPos, hit.point);
                        avoiding = true;
                        // 정면 장애물 회피
                        if (hit.normal.x < 0)
                        {
                            avoidMultiplier = -1;
                        }
                        else
                        {
                            avoidMultiplier = 1;
                        }
                    }
                }

            }
        }

        if (avoiding)
        {
            targetSteerAngle = maxSteerAngle * avoidMultiplier;
        }

    }

    private void ApplySteer() {
        if (avoiding) return;
        // 회전벡터
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position); 
        // 바퀴 각도
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;

        targetSteerAngle = newSteer;

    }

    private void Drive(){
        isBraking = false;
        Debug.Log("차없음");

        currentSpeed = 2 * Mathf.PI * wheelFL.radius * wheelFL.rpm * 60 / 1000;

        if(currentSpeed < maxSpeed && !isBraking){
            // 모터속도 max
            wheelFL.motorTorque = maxMotorTorque;
            wheelFR.motorTorque = maxMotorTorque;
        }else{
            // 모터속도 0
            wheelFL.motorTorque = 0;
            wheelFR.motorTorque = 0;
        }

    }

    private void CheckWaypointDistance(){
        if(Vector3.Distance(transform.position, nodes[currentNode].position) < 1.5f){
            if(currentNode == nodes.Count - 1){
                currentNode = 0;
            }else {
                currentNode++;
            }
        }
    }

    private void Braking()
    {
        if (isBraking)
        {
            // 브레이크 Texture
            carRenderer.material.mainTexture = textureBraking;
            // 속도 감속 브레이크
            wheelBL.brakeTorque = maxBrakeTorque;
            wheelBR.brakeTorque = maxBrakeTorque;
        }
        else
        {
            // 브레이크 Texture
            carRenderer.material.mainTexture = textureNormal;
            // 브레이크 0
            wheelBL.brakeTorque = 0;
            wheelBR.brakeTorque = 0;
        }
    }

    private void LerpToSteerAngle()
    {
        wheelFL.steerAngle = Mathf.Lerp(wheelFL.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
        wheelFR.steerAngle = Mathf.Lerp(wheelFR.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
    }
}
