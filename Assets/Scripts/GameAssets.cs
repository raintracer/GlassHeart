using System.Collections.Generic;
using UnityEngine;

static public class GameAssets
{

    private static readonly GameObject GO;

    private static Dictionary<string, Sound> Sounds;
    private static Dictionary<string, Mesh> Meshes;
    private static Dictionary<string, UnityEngine.Material> Materials;
    private static Dictionary<string, UnityEngine.Sprite> Sprites;

    static GameAssets()
    {
        GO = new GameObject("GameAssetObject");
        Object.DontDestroyOnLoad(GO);
        LoadSounds();
        LoadMaterials();
        LoadSprites();
    }

    #region Materials
    private static void LoadMaterials()
    {

        // 

        Materials = new Dictionary<string, UnityEngine.Material>
        {
            //["SpriteDefault"] = Resources.Load<UnityEngine.Material>("Arena Grid"),
            ["Default"] = Resources.Load<UnityEngine.Material>("Default"),
            ["Swap"] = Resources.Load<UnityEngine.Material>("Swap"),
            ["ClearingFlash"] = Resources.Load<UnityEngine.Material>("ClearingFlash"),
            ["WaitingToBust"] = Resources.Load<UnityEngine.Material>("WaitingToBust"),
            ["TileLand"] = Resources.Load<UnityEngine.Material>("TileLand"),
            ["TileBounce"] = Resources.Load<UnityEngine.Material>("TileBounce"),
            ["TechCounterRise"] = Resources.Load<UnityEngine.Material>("TechCounterRise")
        };
    }

    private static UnityEngine.Material GetMaterial(string _MaterialName)
    {
        if (!Materials.TryGetValue(_MaterialName, out UnityEngine.Material MaterialTemp)) Debug.LogError("Material was not found: " + _MaterialName);
        return MaterialTemp;
    }

    public static class Material
    {
        // Example - public static UnityEngine.Material ArenaGrid { get => GetMaterial("Arena Grid"); }
        public static UnityEngine.Material Swap { get => GetMaterial("Swap"); }
        public static UnityEngine.Material Default { get => GetMaterial("Default"); }
        public static UnityEngine.Material ClearingFlash { get => GetMaterial("ClearingFlash"); }
        public static UnityEngine.Material WaitingToBust { get => GetMaterial("WaitingToBust"); }
        public static UnityEngine.Material TileLand { get => GetMaterial("TileLand"); }
        public static UnityEngine.Material TileBounce { get => GetMaterial("TileBounce"); }
        public static UnityEngine.Material TechCounterRise { get => GetMaterial("TechCounterRise"); }
    }

    #endregion

    #region Sprites

    private static void LoadSprites()
    {
        Sprites = new Dictionary<string, UnityEngine.Sprite>
        {
            ["TileBlueBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Blue-Background"),
            ["TileIndigoBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Indigo-Background"),
            ["TileRedBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Red-Background"),
            ["TileYellowBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Yellow-Background"),
            ["TilePurpleBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Purple-Background"),
            ["TileGreenBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Green-Background"),
            ["TileBlueBackground"] = Resources.Load<UnityEngine.Sprite>("Tile-Blue-Background"),
            ["TileBlueIcon"] = Resources.Load<UnityEngine.Sprite>("Tile-Blue-Icon"),
            ["TileIndigoIcon"] = Resources.Load<UnityEngine.Sprite>("Tile-Indigo-Icon"),
            ["TileRedIcon"] = Resources.Load<UnityEngine.Sprite>("Tile-Red-Icon"),
            ["TileYellowIcon"] = Resources.Load<UnityEngine.Sprite>("Tile-Yellow-Icon"),
            ["TilePurpleIcon"] = Resources.Load<UnityEngine.Sprite>("Tile-Purple-Icon"),
            ["TileGreenIcon"] = Resources.Load<UnityEngine.Sprite>("Tile-Green-Icon"),
            ["BlockTileSingleLeft"] = Resources.Load<UnityEngine.Sprite>("BlockTileSingleLeft"),
            ["BlockTileSingleCenter"] = Resources.Load<UnityEngine.Sprite>("BlockTileSingleCenter"),
            ["BlockTileSingleRight"] = Resources.Load<UnityEngine.Sprite>("BlockTileSingleRight"),
            ["BlockTileSingle"] = Resources.Load<UnityEngine.Sprite>("BlockTileSingle")
        };
    }

    private static UnityEngine.Sprite GetSprite(string _SpriteName)
    {
        if (!Sprites.TryGetValue(_SpriteName, out UnityEngine.Sprite SpriteTemp)) Debug.LogError("Sprite was not found: " + _SpriteName);
        return SpriteTemp;
    }

    public static class Sprite
    {
        // Example - public static UnityEngine.Material ArenaGrid { get => GetMaterial("Arena Grid"); }
        public static UnityEngine.Sprite TileBlueBackground { get => GetSprite("TileBlueBackground"); }
        public static UnityEngine.Sprite TileRedBackground { get => GetSprite("TileRedBackground"); }
        public static UnityEngine.Sprite TileGreenBackground { get => GetSprite("TileGreenBackground"); }
        public static UnityEngine.Sprite TileYellowBackground { get => GetSprite("TileYellowBackground"); }
        public static UnityEngine.Sprite TileIndigoBackground { get => GetSprite("TileIndigoBackground"); }
        public static UnityEngine.Sprite TilePurpleBackground { get => GetSprite("TilePurpleBackground"); }
        public static UnityEngine.Sprite TileBlueIcon { get => GetSprite("TileBlueIcon"); }
        public static UnityEngine.Sprite TileRedIcon { get => GetSprite("TileRedIcon"); }
        public static UnityEngine.Sprite TileGreenIcon { get => GetSprite("TileGreenIcon"); }
        public static UnityEngine.Sprite TileYellowIcon { get => GetSprite("TileYellowIcon"); }
        public static UnityEngine.Sprite TileIndigoIcon { get => GetSprite("TileIndigoIcon"); }
        public static UnityEngine.Sprite TilePurpleIcon { get => GetSprite("TilePurpleIcon"); }
        public static UnityEngine.Sprite BlockTileSingleLeft { get => GetSprite("BlockTileSingleLeft"); }
        public static UnityEngine.Sprite BlockTileSingleCenter { get => GetSprite("BlockTileSingleCenter"); }
        public static UnityEngine.Sprite BlockTileSingleRight { get => GetSprite("BlockTileSingleRight"); }
        public static UnityEngine.Sprite BlockTileSingle { get => GetSprite("BlockTileSingle"); }
    }

    #endregion

    #region Sounds

    private static void LoadSounds()
    {
        Sounds = new Dictionary<string, Sound>
        {
            ["CursorClick"] = new Sound(GO.AddComponent<AudioSource>(), "CursorClick", 0.05f),
            ["DefaultBust"] = new Sound(GO.AddComponent<AudioSource>(), "DefaultBust", 0.5f),
            ["Swap"] = new Sound(GO.AddComponent<AudioSource>(), "Swap", 0.3f),
            ["TileLand"] = new Sound(GO.AddComponent<AudioSource>(), "TileLand", 0.5f),
            ["StoneRock"] = new Sound(GO.AddComponent<AudioSource>(), "StoneRock", 0.5f, true),
            ["Combo1"] = new Sound(GO.AddComponent<AudioSource>(), "Combo1", 0.5f),
            ["TileExplode"] = new Sound(GO.AddComponent<AudioSource>(), "TileExplode", 0.5f),
            ["Glass Break 1"] = new Sound(GO.AddComponent<AudioSource>(), "Glass/Glass Break 1", 0.5f),
            ["Glass Break 2"] = new Sound(GO.AddComponent<AudioSource>(), "Glass/Glass Break 2", 0.5f),
            ["Glass Break Final"] = new Sound(GO.AddComponent<AudioSource>(), "Glass/Glass Break Final", 0.5f)
        };
    }
    public static Sound GetSound(string _SoundName)
    {
        if (!Sounds.TryGetValue(_SoundName, out Sound SoundTemp)) Debug.LogError("Sound was not found: " + _SoundName);
        return SoundTemp;
    }

    public class Sound
    {

        // EXPOSE SOUNDS FOR STRONG TYPING
        public static Sound CursorClick { get => GetSound("CursorClick"); }
        public static Sound DefaultBust { get => GetSound("DefaultBust"); }
        public static Sound Swap { get => GetSound("Swap"); }
        public static Sound TileLand { get => GetSound("TileLand"); }
        public static Sound StoneRock { get => GetSound("StoneRock"); }
        public static Sound Combo1 { get => GetSound("Combo1"); }

        public static Sound TileExplode { get => GetSound("TileExplode"); }

        public static Sound GlassBreak1 { get => GetSound("Glass Break 1"); }

        public static Sound GlassBreak2 { get => GetSound("Glass Break 2"); }

        public static Sound GlassBreakFinal { get => GetSound("Glass Break Final"); }

        private AudioSource Source;
        public string ClipName { get; private set; }
        public float Volume
        {
            get { return Source.volume; }
            set { Source.volume = value; }
        }
        public float Pitch
        {
            get { return Source.pitch; }
            set { Source.pitch = value; }
        }

        public bool Loop
        {
            get { return Source.loop; }
            set { Source.loop = value; }
        }

        public Sound(AudioSource Source, string ClipName, float Volume, bool Loop = false, float Pitch = 1.00f)
        {
            this.Source = Source;
            this.ClipName = ClipName;
            this.Volume = Volume;
            this.Pitch = Pitch;
            this.Loop = Loop;
            this.Source.clip = Resources.Load<AudioClip>(ClipName);
        }

        public void Play()
        {
            Source.Play();
        }

        public void Stop()
        {
            Source.Stop();
        }

    }

    #endregion

    #region Glassheart Methods

    public static UnityEngine.Sprite GetBackgroundSpriteByTileColor(BasicTile.TileColor _Color)
    {
        return _Color switch
        {
            BasicTile.TileColor.Red    => Sprite.TileRedBackground,
            BasicTile.TileColor.Yellow => Sprite.TileYellowBackground,
            BasicTile.TileColor.Green  => Sprite.TileGreenBackground,
            BasicTile.TileColor.Blue   => Sprite.TileBlueBackground,
            BasicTile.TileColor.Indigo => Sprite.TileIndigoBackground,
            BasicTile.TileColor.Purple => Sprite.TilePurpleBackground,
            _ => Sprite.TileRedBackground,
        };
    }

    public static UnityEngine.Sprite GetIconSpriteByTileColor(BasicTile.TileColor _Color)
    {
        return _Color switch
        {
            BasicTile.TileColor.Red => Sprite.TileRedIcon,
            BasicTile.TileColor.Yellow => Sprite.TileYellowIcon,
            BasicTile.TileColor.Green => Sprite.TileGreenIcon,
            BasicTile.TileColor.Blue => Sprite.TileBlueIcon,
            BasicTile.TileColor.Indigo => Sprite.TileIndigoIcon,
            BasicTile.TileColor.Purple => Sprite.TilePurpleIcon,
            _ => Sprite.TileRedIcon,
        };
    }

    public static BasicTile.TileColor GetRandomTileColor()
    {
        return (BasicTile.TileColor) Random.Range((int)0, (int)6);
    }

    #endregion

}