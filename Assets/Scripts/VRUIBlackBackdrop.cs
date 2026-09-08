using UnityEngine;

/// <summary>
/// Membuat panel background hitam pekat di belakang UI yang pas dengan ukuran poster,
/// memblokir 100% cahaya api dan partikel di belakangnya, serta bisa diatur posisinya dan ukurannya secara manual di Inspector.
/// </summary>
public class VRUIBlackBackdrop : MonoBehaviour
{
    [Header("1. Ukuran & Posisi Manual")]
    [Tooltip("Posisi offset background (X: geser kanan/kiri, Y: atas/bawah, Z: maju/mundur)")]
    public Vector3 backdropPosition = new Vector3(0f, 0f, 0.01f);

    [Tooltip("Lebar background (meter)")]
    public float width = 7.7f;

    [Tooltip("Tinggi background (meter)")]
    public float height = 10.48f;

    [Header("2. Tampilan & Warna")]
    [Tooltip("Warna background (Default: Hitam Pekat)")]
    public Color color = new Color(0.01f, 0.01f, 0.02f, 1.0f);

    [Tooltip("Radius kelengkungan sudut rounded corner (0 = kotak siku-siku)")]
    [Range(0f, 40f)]
    public float cornerRadius = 18f;

    [Tooltip("Sorting Order relatif terhadap UI (misal -2 agar tepat di belakang UI)")]
    public int orderOffset = -2;

    [Header("3. Blokir Cahaya & Partikel Api (Z-Buffer Opaque)")]
    [Tooltip("Gunakan shader Opaque dengan ZWrite ON agar partikel api di belakang 100% terblokir")]
    public bool blockFireParticles = true;

    private GameObject _backdropGO;
    private SpriteRenderer _backdropSR;
    private Material _opaqueMaterial;

    private void Awake()
    {
        AutoDetectDimensionsIfZero();
        UpdateBackdrop();
    }

    private void Start()
    {
        UpdateBackdrop();
    }

    private void OnEnable()
    {
        UpdateBackdrop();
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += SafeEditorUpdate;
        #endif
    }

    private void SafeEditorUpdate()
    {
        #if UNITY_EDITOR
        if (this != null)
        {
            UpdateBackdrop();
        }
        #endif
    }

    private void AutoDetectDimensionsIfZero()
    {
        SpriteRenderer parentSR = GetComponent<SpriteRenderer>();
        if (parentSR != null && parentSR.sprite != null)
        {
            if (width <= 0.1f || height <= 0.1f)
            {
                if (parentSR.drawMode != SpriteDrawMode.Simple)
                {
                    width = parentSR.size.x;
                    height = parentSR.size.y;
                }
                else
                {
                    width = parentSR.sprite.rect.width / parentSR.sprite.pixelsPerUnit;
                    height = parentSR.sprite.rect.height / parentSR.sprite.pixelsPerUnit;
                }
            }
        }
    }

    /// <summary>
    /// Memperbarui posisi, ukuran, material, dan tampilan background hitam
    /// </summary>
    public void UpdateBackdrop()
    {
        Transform existing = transform.Find("UI_Black_Backdrop");
        if (existing != null)
        {
            _backdropGO = existing.gameObject;
        }
        else
        {
            _backdropGO = new GameObject("UI_Black_Backdrop");
            _backdropGO.transform.SetParent(transform, false);
        }

        _backdropGO.transform.localPosition = backdropPosition;
        _backdropGO.transform.localRotation = Quaternion.identity;
        _backdropGO.transform.localScale = Vector3.one;

        _backdropSR = _backdropGO.GetComponent<SpriteRenderer>();
        if (_backdropSR == null)
            _backdropSR = _backdropGO.AddComponent<SpriteRenderer>();

        SpriteRenderer parentSR = GetComponent<SpriteRenderer>();
        if (parentSR != null)
        {
            _backdropSR.sortingLayerID = parentSR.sortingLayerID;
            _backdropSR.sortingOrder = parentSR.sortingOrder + orderOffset;
        }

        _backdropSR.sprite = CreateCardSprite(256, 256, cornerRadius);
        _backdropSR.drawMode = SpriteDrawMode.Sliced;
        _backdropSR.size = new Vector2(Mathf.Max(0.1f, width), Mathf.Max(0.1f, height));
        _backdropSR.color = color;

        if (blockFireParticles)
        {
            if (_opaqueMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    _opaqueMaterial = new Material(shader);
                    _opaqueMaterial.name = "M_UI_Black_OpaqueBackdrop";
                    if (_opaqueMaterial.HasProperty("_BaseColor"))
                        _opaqueMaterial.SetColor("_BaseColor", color);
                    else if (_opaqueMaterial.HasProperty("_Color"))
                        _opaqueMaterial.SetColor("_Color", color);

                    _opaqueMaterial.renderQueue = 2000;
                }
            }

            if (_opaqueMaterial != null)
            {
                _backdropSR.sharedMaterial = _opaqueMaterial;
            }
        }
    }

    private Sprite CreateCardSprite(int w, int h, float r)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = 0f;
                if (x < r) dx = r - x;
                else if (x > w - r) dx = x - (w - r);

                float dy = 0f;
                if (y < r) dy = r - y;
                else if (y > h - r) dy = y - (h - r);

                if (dx > 0f && dy > 0f)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(r - dist + 1.0f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }
        tex.Apply();
        Vector4 border = new Vector4(r, r, r, r);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
