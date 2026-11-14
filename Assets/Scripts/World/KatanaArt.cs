using UnityEngine;

namespace Ganryujima.World
{
    public static class KatanaArt
    {
        public static Transform Build(Transform camera, bool firstPerson = true, bool hands = true)
        {
            var sword = new GameObject("Forged katana").transform;
            sword.SetParent(camera, false);
            sword.localPosition = new Vector3(0.42f, -0.55f, 0.85f);
            sword.localRotation = Quaternion.Euler(18, -8, -12);
            var assets = sword.gameObject.AddComponent<GeneratedResources>();
            var steel = ArtMaterials.Surface(assets, "Folded steel", new Color(0.78f, 0.83f, 0.84f), null, 0.86f, 0.72f);
            var edge = ArtMaterials.Surface(assets, "Hamon polish", new Color(0.92f, 0.93f, 0.88f), null, 0.8f, 0.55f);
            var dark = ArtMaterials.Surface(assets, "Black silk wrap", new Color(0.055f, 0.07f, 0.065f), "cloth", 0.1f);
            var bronze = ArtMaterials.Surface(assets, "Aged brass fittings", new Color(0.46f, 0.34f, 0.15f), "stone", 0.55f, 0.7f);
            var ivory = ArtMaterials.Surface(assets, "Samegawa grip", new Color(0.69f, 0.66f, 0.53f), "stone", 0.15f);
            var glove = ArtMaterials.Surface(assets, "Worn leather hand wraps", new Color(0.23f, 0.19f, 0.15f), "cloth", 0.2f);
            var sleeve = ArtMaterials.Surface(assets, "Indigo sleeve", new Color(0.10f, 0.145f, 0.155f), "cloth", 0.05f);
            var blade = ArenaBuilder.MeshObject("Curved shinogi blade", sword, assets.Own(ArtGeometry.Blade(0.88f, 0.036f, 0.035f)), steel);
            blade.localPosition = Vector3.up * 0.16f;
            // Fine, irregular temper line along the blade's polished face.
            var hamon = new Vector3[41];
            for (int i = 0; i < hamon.Length; i++)
            {
                float t = i / 40f;
                hamon[i] = new Vector3(t * t * 0.035f - 0.008f + Mathf.Sin(t * 63) * 0.0012f, 0.17f + t * 0.84f, -0.0028f);
            }
            ArenaBuilder.Tube("Wavy hamon", sword, assets, hamon, 0.0011f, 0.0007f, edge, 4);
            ArenaBuilder.Primitive("Samegawa", PrimitiveType.Cylinder, sword, new Vector3(0, 0.005f, 0), new Vector3(0.048f, 0.13f, 0.037f), ivory);
            for (int i = 0; i < 13; i++)
            {
                float y = -0.115f + i * 0.02f;
                ArenaBuilder.Tube("Crossed silk binding", sword, assets, new[] {
                    new Vector3(-0.024f, y, -0.016f), new Vector3(0.024f, y + 0.024f, -0.016f),
                    new Vector3(0.024f, y + 0.024f, 0.016f), new Vector3(-0.024f, y + 0.045f, 0.016f) }, 0.0055f, 0.0055f, dark, 5);
            }
            ArenaBuilder.Primitive("Fuchi", PrimitiveType.Cylinder, sword, new Vector3(0, 0.135f, 0), new Vector3(0.059f, 0.009f, 0.045f), bronze);
            ArenaBuilder.Primitive("Kashira", PrimitiveType.Cylinder, sword, new Vector3(0, -0.13f, 0), new Vector3(0.053f, 0.008f, 0.041f), bronze);
            ArenaBuilder.Primitive("Habaki", PrimitiveType.Cube, sword, new Vector3(0, 0.18f, 0), new Vector3(0.038f, 0.04f, 0.012f), bronze);
            ArenaBuilder.Primitive("Iron tsuba", PrimitiveType.Cylinder, sword, new Vector3(0, 0.15f, 0), new Vector3(0.14f, 0.004f, 0.11f), dark);
            var rim = new Vector3[65];
            for (int i = 0; i < rim.Length; i++) { float a = i * Mathf.PI * 2 / 64; rim[i] = new Vector3(Mathf.Cos(a) * 0.068f, 0.15f, Mathf.Sin(a) * 0.053f); }
            ArenaBuilder.Tube("Tsuba rim", sword, assets, rim, 0.002f, 0.002f, bronze, 5);
            if (hands) Hand(sword, assets, new Vector3(0.035f, 0.04f, 0.008f), glove, sleeve, false, firstPerson);
            if (hands) Hand(sword, assets, new Vector3(-0.025f, -0.09f, 0.015f), glove, sleeve, true, firstPerson);
            return sword;
        }

        private static void Hand(Transform sword, GeneratedResources assets, Vector3 p, Material glove, Material sleeve, bool left, bool firstPerson)
        {
            float sign = left ? -1 : 1;
            var root = new GameObject(left ? "Left wrapped hand" : "Right wrapped hand").transform;
            root.SetParent(sword, false); root.localPosition = p;
            var palm = ArenaBuilder.MeshObject("Leather palm", root, assets.Own(ArtGeometry.Rock(left ? 80 : 70)), glove);
            palm.localScale = new Vector3(0.043f, 0.052f, 0.035f);
            for (int i = 0; i < 4; i++)
            {
                float y = -0.035f + i * 0.019f;
                ArenaBuilder.Tube("Wrapped finger", root, assets, new[] {
                    new Vector3(sign * 0.025f, y, -0.015f), new Vector3(-sign * 0.03f, y - 0.004f, -0.03f),
                    new Vector3(-sign * 0.045f, y, 0.004f), new Vector3(-sign * 0.02f, y + 0.003f, 0.026f) }, 0.009f, 0.008f, glove, 7);
            }
            ArenaBuilder.Tube("Thumb", root, assets, new[] { new Vector3(sign * 0.035f, 0.04f, 0), new Vector3(0, 0.045f, -0.035f), new Vector3(-sign * 0.025f, 0.02f, -0.03f) }, 0.013f, 0.01f, glove, 8);
            if (firstPerson) ArenaBuilder.Tube("Sleeve and wrist", root, assets, new[] { new Vector3(sign * 0.03f, -0.02f, 0.02f), new Vector3(sign * 0.12f, -0.18f, -0.04f), new Vector3(sign * 0.26f, -0.42f, -0.12f) }, 0.045f, 0.095f, sleeve, 12);
        }
    }
}
