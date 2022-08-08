using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    public Color lineColor;

    private List<Transform> nodes = new List<Transform>();

    // 트랙 그리는 함수 (선택 되었을 때-이 기능 빼고 싶으면 함수명에서 Selected 빼면 됨)
    void OnDrawGizmosSelected() {
        Gizmos.color = lineColor;

        Transform[] pathTransforms = GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        // 점 생성
        for(int i = 0; i < pathTransforms.Length; i++){
            if(pathTransforms[i] != transform) {
                nodes.Add(pathTransforms[i]);
            }
        }

        // 점 사이 연결
        for(int i = 0; i < nodes.Count; i++){
            Vector3 currentNode = nodes[i].position;
            Vector3 previousNode = Vector3.zero;

            if(i > 0){
                previousNode = nodes[i - 1].position;
            }else if(i==0 && nodes.Count > 1){
                // 0번 노드와 마지막 노드 연결
                previousNode = nodes[nodes.Count - 1].position;
            }
            
            //선 그리기
            Gizmos.DrawLine(previousNode, currentNode);
            //꺾이는 지점 표시
            Gizmos.DrawWireSphere(currentNode, 0.3f);
        }
    }
}
