using UnityEngine;
using UnityEditor;

public class ShadowboundMVPSetup : EditorWindow
{
    [MenuItem("Shadowbound/Setup MVP Demo Scene")]
    public static void SetupScene()
    {
        // 1. Create Ground Layer if it doesn't exist
        CreateLayer("Ground");

        // 2. Setup Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);
        ground.layer = LayerMask.NameToLayer("Ground");
        
        // Give ground a material if possible, or leave default
        
        // 3. Setup Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 15, -12);
            mainCam.transform.rotation = Quaternion.Euler(55, 0, 0);
        }

        // 4. Setup GameManager
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();

        // 5. Setup Player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Penguin";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, 0);
        
        Rigidbody playerRb = player.AddComponent<Rigidbody>();
        playerRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        PlayerController pc = player.AddComponent<PlayerController>();
        pc.groundLayer = LayerMask.GetMask("Ground");

        // 6. Setup a Shadow Zone (Cloud)
        GameObject shadowZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadowZone.name = "CloudShadowZone";
        shadowZone.transform.position = new Vector3(5, 0.5f, 5);
        shadowZone.transform.localScale = new Vector3(6, 2, 6);
        Collider shadowCollider = shadowZone.GetComponent<Collider>();
        shadowCollider.isTrigger = true;
        shadowZone.AddComponent<ShadowZone>();
        
        MeshRenderer shadowRenderer = shadowZone.GetComponent<MeshRenderer>();
        if (shadowRenderer != null && shadowRenderer.sharedMaterial != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0, 0, 0, 0.4f);
            shadowRenderer.sharedMaterial = mat;
        }

        // 7. Setup Enemy Prefab reference (Create a hidden one)
        GameObject enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyPrefab.name = "EnemyPlantPrefab";
        CreateTag("Enemy");
        enemyPrefab.tag = "Enemy"; 
        enemyPrefab.transform.position = new Vector3(0, -50, 0); // Hide it away
        
        Rigidbody enemyRb = enemyPrefab.AddComponent<Rigidbody>();
        enemyRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        enemyPrefab.AddComponent<EnemyPlant>();
        
        gm.enemyPrefab = enemyPrefab;

        // 8. Setup UI Canvas
        SetupUI();

        Debug.Log("Shadowbound Expanded MVP Scene setup complete! Press Play to test.");
    }

    private static void SetupUI()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        UIManager ui = canvasObj.AddComponent<UIManager>();

        // Status Text (Center)
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text statusTxt = statusObj.AddComponent<UnityEngine.UI.Text>();
        statusTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusTxt.alignment = TextAnchor.MiddleCenter;
        statusTxt.fontSize = 50;
        statusTxt.color = Color.white;
        RectTransform statusRt = statusObj.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0, 0);
        statusRt.anchorMax = new Vector2(1, 1);
        statusRt.sizeDelta = Vector2.zero;
        ui.statusText = statusTxt;

        // Time Text (Top Center)
        GameObject timeObj = new GameObject("TimeText");
        timeObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text timeTxt = timeObj.AddComponent<UnityEngine.UI.Text>();
        timeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timeTxt.alignment = TextAnchor.UpperCenter;
        timeTxt.fontSize = 30;
        timeTxt.color = Color.white;
        RectTransform timeRt = timeObj.GetComponent<RectTransform>();
        timeRt.anchorMin = new Vector2(0.5f, 1);
        timeRt.anchorMax = new Vector2(0.5f, 1);
        timeRt.anchoredPosition = new Vector2(0, -30);
        timeRt.sizeDelta = new Vector2(300, 50);
        ui.timeText = timeTxt;

        // Lives Text (Top Left)
        GameObject livesObj = new GameObject("LivesText");
        livesObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text livesTxt = livesObj.AddComponent<UnityEngine.UI.Text>();
        livesTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        livesTxt.alignment = TextAnchor.UpperLeft;
        livesTxt.fontSize = 30;
        livesTxt.color = Color.green;
        RectTransform livesRt = livesObj.GetComponent<RectTransform>();
        livesRt.anchorMin = new Vector2(0, 1);
        livesRt.anchorMax = new Vector2(0, 1);
        livesRt.anchoredPosition = new Vector2(150, -30);
        livesRt.sizeDelta = new Vector2(300, 50);
        ui.livesText = livesTxt;

        // Score Text (Top Right)
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text scoreTxt = scoreObj.AddComponent<UnityEngine.UI.Text>();
        scoreTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreTxt.alignment = TextAnchor.UpperRight;
        scoreTxt.fontSize = 30;
        scoreTxt.color = Color.yellow;
        RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(1, 1);
        scoreRt.anchorMax = new Vector2(1, 1);
        scoreRt.anchoredPosition = new Vector2(-150, -30);
        scoreRt.sizeDelta = new Vector2(300, 50);
        ui.scoreText = scoreTxt;
    }

    private static void CreateEnemy(Vector3 pos)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "EnemyPlant";
        CreateTag("Enemy");
        enemy.tag = "Enemy"; 
        enemy.transform.position = pos;
        
        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        enemy.AddComponent<EnemyPlant>();
    }

    private static void CreateTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        
        bool found = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
        }
    }

    private static void CreateLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        bool layerExists = false;
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName)
            {
                layerExists = true;
                break;
            }
        }

        if (!layerExists)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if (sp.stringValue == "")
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }
    }
}
