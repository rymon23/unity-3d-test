
using System.Collections.Generic;
using UnityEngine;

public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector3[] vertices)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            m_points.Add(new Vector2(vertices[i].x, vertices[i].z));
        }
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
        {
            return indices.ToArray();
        }

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++)
            {
                V[v] = v;
            }
        }
        else
        {
            for (int v = 0; v < n; v++)
            {
                V[v] = (n - 1) - v;
            }
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
            {
                return indices.ToArray();
            }

            int u = v;
            if (nv <= u)
            {
                u = 0;
            }
            v = u + 1;
            if (nv <= v)
            {
                v = 0;
            }
            int w = v + 1;
            if (nv <= w)
            {
                w = 0;
            }

            if (Snip(u, v, w, nv, V))
            {
                int a = V[u];
                int b = V[v];
                int c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                for (int s = v, t = v + 1; t < nv; s++, t++)
                {
                    V[s] = V[t];
                }
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float area = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            area += pval.x * qval.y - qval.x * pval.y;
        }
        return area * 0.5f;
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
        {
            return false;
        }

        for (int p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
            {
                continue;
            }

            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
            {
                return false;
            }
        }

        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax = C.x - B.x;
        float ay = C.y - B.y;
        float bx = A.x - C.x;
        float by = A.y - C.y;
        float cx = B.x - A.x;
        float cy = B.y - A.y;
        float apx = P.x - A.x;
        float apy = P.y - A.y;
        float bpx = P.x - B.x;
        float bpy = P.y - B.y;
        float cpx = P.x - C.x;
        float cpy = P.y - C.y;

        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;

        return (aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f);
    }

    private static bool IsEar(List<Vector2> vertices, int index1, int index2, int index3)
    {
        Vector2 vertex1 = vertices[index1];
        Vector2 vertex2 = vertices[index2];
        Vector2 vertex3 = vertices[index3];

        float crossProduct = (vertex2.x - vertex1.x) * (vertex3.y - vertex1.y) - (vertex2.y - vertex1.y) * (vertex3.x - vertex1.x);

        if (crossProduct <= 0)
        {
            return false;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            if (i == index1 || i == index2 || i == index3)
            {
                continue;
            }

            Vector2 vertex = vertices[i];

            if (IsPointInsideTriangle(vertex, vertex1, vertex2, vertex3))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPointInsideTriangle(Vector2 point, Vector2 vertex1, Vector2 vertex2, Vector2 vertex3)
    {
        float area1 = (point.x - vertex1.x) * (vertex2.y - vertex1.y) - (vertex2.x - vertex1.x) * (point.y - vertex1.y);
        float area2 = (point.x - vertex2.x) * (vertex3.y - vertex2.y) - (vertex3.x - vertex2.x) * (point.y - vertex2.y);
        float area3 = (point.x - vertex3.x) * (vertex1.y - vertex3.y) - (vertex1.x - vertex3.x) * (point.y - vertex3.y);

        bool hasNegativeArea = (area1 < 0) || (area2 < 0) || (area3 < 0);
        bool hasPositiveArea = (area1 > 0) || (area2 > 0) || (area3 > 0);

        return !(hasNegativeArea && hasPositiveArea);
    }

    public static List<int> TriangulatePolygonWithHoles(List<Vector2> polygon, List<List<Vector2>> holes)
    {
        List<int> triangles = new List<int>();

        // Triangulate the outer polygon
        TriangulatePolygon(polygon, ref triangles);

        // Triangulate the holes
        foreach (List<Vector2> hole in holes)
        {
            TriangulatePolygon(hole, ref triangles);
        }

        return triangles;
    }

    private static void TriangulatePolygon(List<Vector2> vertices, ref List<int> triangles)
    {
        if (vertices.Count < 3)
        {
            return;
        }

        int[] indices = new int[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            indices[i] = i;
        }

        int index = 0;
        int count = vertices.Count;
        while (count > 2)
        {
            int previousIndex = (index + count - 1) % count;
            int nextIndex = (index + 1) % count;

            if (IsEar(vertices, indices[previousIndex], indices[index], indices[nextIndex]))
            {
                triangles.Add(indices[previousIndex]);
                triangles.Add(indices[index]);
                triangles.Add(indices[nextIndex]);

                // Remove the current index from the list
                for (int i = index; i < count - 1; i++)
                {
                    indices[i] = indices[i + 1];
                }
                count--;

                // Check the two adjacent vertices
                if (index == count)
                {
                    index = 0;
                }

                continue;
            }

            index = (index + 1) % count;
        }
    }

}
