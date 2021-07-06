﻿using System.Collections.Generic;
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

    private static void LoadSounds()
    {
        Sounds = new Dictionary<string, Sound>
        {
            ["CursorClick"] = new Sound(GO.AddComponent<AudioSource>(), "CursorClick", 0.5f),
        };
    }


    public static Sound GetSound(string _SoundName)
    {
        if (!Sounds.TryGetValue(_SoundName, out Sound SoundTemp)) Debug.LogError("Sound was not found: " + _SoundName);
        return SoundTemp;
    }

    private static void LoadMaterials()
    {
        Materials = new Dictionary<string, UnityEngine.Material>
        {
            // Example - ["Arena Grid"] = Resources.Load<UnityEngine.Material>("Arena Grid"),
        };
    }

    private static UnityEngine.Material GetMaterial(string _MaterialName)
    {
        if (!Materials.TryGetValue(_MaterialName, out UnityEngine.Material MaterialTemp)) Debug.LogError("Material was not found: " + _MaterialName);
        return MaterialTemp;
    }

    private static void LoadSprites()
    {
        Sprites = new Dictionary<string, UnityEngine.Sprite>
        {
            ["TileBlue"] = Resources.Load<UnityEngine.Sprite>("Tile-Blue"),
            ["TileIndigo"] = Resources.Load<UnityEngine.Sprite>("Tile-Indigo"),
            ["TileRed"] = Resources.Load<UnityEngine.Sprite>("Tile-Red"),
            ["TileYellow"] = Resources.Load<UnityEngine.Sprite>("Tile-Yellow"),
            ["TilePurple"] = Resources.Load<UnityEngine.Sprite>("Tile-Purple"),
            ["TileGreen"] = Resources.Load<UnityEngine.Sprite>("Tile-Green"),
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
        public static UnityEngine.Sprite TileBlue { get => GetSprite("TileBlue"); }
        public static UnityEngine.Sprite TileRed { get => GetSprite("TileRed"); }
        public static UnityEngine.Sprite TileGreen { get => GetSprite("TileGreen"); }
        public static UnityEngine.Sprite TileYellow { get => GetSprite("TileYellow"); }
        public static UnityEngine.Sprite TileIndigo { get => GetSprite("TileIndigo"); }
        public static UnityEngine.Sprite TilePurple { get => GetSprite("TilePurple"); }
    }

    public class Sound
    {

        // EXPOSE SOUNDS FOR STRONG TYPING
        public static Sound CursorClick { get => GetSound("CursorClick"); }


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

    // GAME SPECIFIC METHODS

    public static UnityEngine.Sprite GetSpriteByTileColor(PuzzleTile.TileColor _Color)
    {
        return _Color switch
        {
            PuzzleTile.TileColor.Red    => Sprite.TileRed,
            PuzzleTile.TileColor.Yellow => Sprite.TileYellow,
            PuzzleTile.TileColor.Green  => Sprite.TileGreen,
            PuzzleTile.TileColor.Blue   => Sprite.TileBlue,
            PuzzleTile.TileColor.Indigo => Sprite.TileIndigo,
            PuzzleTile.TileColor.Purple => Sprite.TilePurple,
            _ => Sprite.TileRed,
        };
    }

    public static PuzzleTile.TileColor GetRandomTileColor()
    {
        return (PuzzleTile.TileColor) Random.Range((int)0, (int)6);
    }



}