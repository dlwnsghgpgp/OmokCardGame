using UnityEngine;

public class BoardGizmo : MonoBehaviour
{
    public int boardSize = 15;
    public float spacing = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        float half = (boardSize - 1) / 2f; // 15면 7
        for (int col = 0; col < boardSize; col++)
        for (int row = 0; row < boardSize; row++)
        {
            Vector3 p = transform.position +
                new Vector3((col - half) * spacing, 0.25f, (row - half) * spacing);
            Gizmos.DrawSphere(p, 0.08f);
        }
    }
}