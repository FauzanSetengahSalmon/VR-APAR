using UnityEngine;

/// <summary>
/// Script Optimasi VR Otomatis (VR Auto-Optimizer).
/// 
/// FUNGSI:
///   1. Mengunci Target Frame Rate ke 90 FPS (smooth tanpa patah-patah).
///   2. Mematikan modul Particle Collision pada asap APAR (menghilangkan CPU lag spikes).
///   3. Memperbesar ukuran fisik partikel asap (Visual TETAP TEBAL & MEMUKAU meski partikel sedikit).
///   4. Mengoptimasi semua lampu api real-time agar tidak membebani GPU Meta Quest.
/// </summary>
public class VROptimizer : MonoBehaviour
{
    [Header("Target Framerate VR")]
    [Tooltip("Frame rate target Quest VR (72Hz / 90Hz)")]
    public int targetVRFrameRate = 90;

    [Header("Optimasi Visual Asap APAR")]
    [Tooltip("Particle system asap APAR (Jika kosong, akan dicari otomatis)")]
    public ParticleSystem aparSmokeParticle;

    [Tooltip("Jumlah maksimum partikel asap (200 sangat lancar di Quest)")]
    public int maxSmokeParticles = 200;

    [Tooltip("Pengali ukuran partikel asap agar tetap terlihat tebal & bervolume")]
    public float smokeSizeMultiplier = 1.8f;

    [Header("Optimasi Pencahayaan Realistis")]
    [Tooltip("Otomatis sesuaikan mode lampu api agar tidak membebani GPU Quest")]
    public bool autoOptimizeLights = true;

    private void Awake()
    {
        // 1. Kunci Refresh Rate VR ke 90 FPS
        Application.targetFrameRate = targetVRFrameRate;
        QualitySettings.vSyncCount = 0; // VR SDK yang mengatur VSync

        // 2. Optimasi Asap APAR: Visual Tebal + Bebas Lag
        OptimizeAPARSmoke();

        // 3. Optimasi Lampu Api
        if (autoOptimizeLights)
        {
            OptimizeSceneLights();
        }

        Debug.Log("[VROptimizer] 🚀 Optimasi VR Otomatis Selesai! Visual Memukau + 90 FPS Mulus.");
    }

    private void OptimizeAPARSmoke()
    {
        if (aparSmokeParticle == null)
        {
            AutoFireExtinguisher mainExt = FindFirstObjectByType<AutoFireExtinguisher>();
            if (mainExt != null)
                aparSmokeParticle = mainExt.sprayEffect;
        }

        if (aparSmokeParticle != null)
        {
            // A. MATIKAN Collision Fisika Partikel (Eliminasi Penyebab Lag CPU #1)
            var collision = aparSmokeParticle.collision;
            if (collision.enabled)
            {
                collision.enabled = false;
                Debug.Log("[VROptimizer] ✔ Particle Collision dinonaktifkan (CPU aman).");
            }

            // B. Kurangi Max Particles tetapi Perbesar Ukuran Fisik Partikel
            //    Hasilnya: Visual asap TETAP TEBAL & REALISTIS di layar VR, tapi GPU sangat ringan!
            var main = aparSmokeParticle.main;
            main.maxParticles = maxSmokeParticles;

            // Perbesar ukuran awal partikel (Misal dari 0.4 - 0.8 -> 0.72 - 1.44)
            float minSize = main.startSize.constantMin > 0 ? main.startSize.constantMin : 0.4f;
            float maxSize = main.startSize.constantMax > 0 ? main.startSize.constantMax : 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize * smokeSizeMultiplier, maxSize * smokeSizeMultiplier);

            Debug.Log($"[VROptimizer] ✔ Asap APAR dioptimasi: Max Particles={maxSmokeParticles}, Size Multiplier={smokeSizeMultiplier}x.");
        }
    }

    private void OptimizeSceneLights()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        int lightCount = 0;

        foreach (Light l in allLights)
        {
            // Jangan ubah Directional Light utama (lampu matahari/ruangan)
            if (l.type == LightType.Directional) continue;

            // Atur lampu api & lampu kecil ke Vertex/Auto mode tanpa bayangan berat
            l.renderMode = LightRenderMode.Auto;
            l.shadows = LightShadows.None;
            lightCount++;
        }

        Debug.Log($"[VROptimizer] ✔ {lightCount} lampu dinamis dioptimasi untuk Quest GPU.");
    }
}
