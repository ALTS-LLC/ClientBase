using UnityEngine;

public class GameViewGrid : MonoBehaviour
{
    [Header("グリッド設定")]
    [Tooltip("グリッドの範囲 (X軸とZ軸の両方に適用)")]
    public int gridSize = 100;

    [Tooltip("グリッドのセルのサイズ (単位: Unityユニット)")]
    public float cellSize = 1.0f;

    [Tooltip("グリッドの線の色")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 半透明の灰色

    [Tooltip("中央線の色 (X軸とZ軸)")]
    public Color centerLineColor = new Color(0.7f, 0.7f, 0.7f, 0.7f); // 少し明るい灰色

    [Tooltip("グリッドを描画する高さ (Y座標)")]
    public float gridHeight = 0f;

    // グリッド描画用のマテリアル
    private Material lineMaterial;

    void Awake()
    {
        CreateLineMaterial(); // マテリアルを生成
    }

    void OnDisable()
    {
        // スクリプトが無効になったときにマテリアルを破棄
        if (lineMaterial != null)
        {
            DestroyImmediate(lineMaterial);
        }
    }

    // グリッド描画用のシンプルなマテリアルを作成する
    // Hidden/Internal-Colored はGL.LINES描画によく使われる内部シェーダー
    void CreateLineMaterial()
    {
        if (lineMaterial == null)
        {
            // Unityの内部シェーダーを探してマテリアルを作成
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Debug.LogError("グリッド描画用のシェーダーが見つかりません。");
                return;
            }
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave; // シーンに保存しない
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 両面描画
            lineMaterial.SetInt("_ZWrite", 0); // Zバッファ書き込み無効 (半透明グリッド用)
        }
    }

    // すべてのカメラのレンダリングが完了した後に呼び出される
    void OnRenderObject()
    {
        if (lineMaterial == null)
        {
            CreateLineMaterial(); // マテリアルが破棄されていた場合に再作成
            if (lineMaterial == null) return;
        }

        // マテリアルをパス0でセット (通常はデフォルトパス)
        lineMaterial.SetPass(0);

        GL.PushMatrix(); // 現在の行列を保存

        // カメラの位置に基づいてグリッドを中央に配置するためのオフセット計算
        // グリッドが常にカメラの下に追従し、特定の範囲を描画するようにする
        Vector3 camPos = gameObject.transform.position;
        float startX = Mathf.Floor(camPos.x / cellSize) * cellSize - gridSize * cellSize / 2f;
        float startZ = Mathf.Floor(camPos.z / cellSize) * cellSize - gridSize * cellSize / 2f;

        // グリッドの描画
        GL.Begin(GL.LINES); // 線を描画することを宣言

        // Z軸に平行な線を描画 (Xを変化させる)
        for (int z = 0; z <= gridSize; z++)
        {
            float currentZ = startZ + z * cellSize;
            GL.Color((z == gridSize / 2) ? centerLineColor : gridColor); // 中央線は色を変える
            GL.Vertex3(startX, gridHeight, currentZ);
            GL.Vertex3(startX + gridSize * cellSize, gridHeight, currentZ);
        }

        // X軸に平行な線を描画 (Zを変化させる)
        for (int x = 0; x <= gridSize; x++)
        {
            float currentX = startX + x * cellSize;
            GL.Color((x == gridSize / 2) ? centerLineColor : gridColor); // 中央線は色を変える
            GL.Vertex3(currentX, gridHeight, startZ);
            GL.Vertex3(currentX, gridHeight, startZ + gridSize * cellSize);
        }

        GL.End(); // 線描画の終了
        GL.PopMatrix(); // 保存した行列を復元
    }
}
