using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// 任务 12-15 关卡重建工具（编辑器专用）。
/// 数据驱动：9 个房间（L1/L2/L3 × 教学/应用/挑战）由矩形填充 + 斜坡线段 + 特殊物列表描述，
/// 一次 BuildAll 生成场景房间、RoomConfigSO 资产、灯光接线并保存场景。
/// 为什么用工具而非手工摆：Tilemap 手绘不可复现、不可审查；工具即关卡设计文档（面试可讲）。
/// 图例：'#'=公共地形 'O'=表世界地形 'N'=里世界地形 'T'=表陷阱 'U'=里陷阱 '='=单向板 'X'=挖空
/// </summary>
public static class LevelRebuildTool
{
    private class R
    {
        public char l; public int x0, y0, x1, y1;
        public R(char l, int x0, int y0, int x1, int y1) { this.l = l; this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; }
    }

    private class S
    {
        public char d; public int x0, y0, x1, y1; // 对角线两端（45°，|dx|=|dy|）
        public S(char d, int x0, int y0, int x1, int y1) { this.d = d; this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; }
    }

    private class Room
    {
        public string name, cfg;
        public Vector2 spawn = new Vector2(-15f, -1.95f), exit = new Vector2(15.5f, -1.6f);
        public bool endGame, hasBullets;
        public Vector2[] bPoints;
        public float[] plat; // x,y,axisX,axisY,dist,speed
        public List<R> rects = new List<R>();
        public List<S> slopes = new List<S>();
    }

    // ---------------- 资产路径 ----------------
    private const string TileDir = "Assets/ArtRes/Tiles/MainTerrain/";
    private const string TrapTile = "Assets/ArtRes/Tiles/Traps/On (16x32)_0.asset";
    private const string GenDir = "Assets/ArtRes/Tiles/Gen/";
    private const string TilemapMat = "Assets/Materials/URP2D_TilemapLit.mat";
    private const string RoomDir = "Assets/SO/Rooms/";
    private const string BulletPrefab = "Assets/Resources/Prefabs/Bullet.prefab";
    private const string BulletStats = "Assets/SO/Rooms/Bullet_Default.asset";

    private static TileBase tGroundFill, tGroundTop, tOldFill, tOldTop, tNewFill, tNewTop, tOneWay, tTrap, tSlopeUp, tSlopeDown;
    private static Sprite platSprite;

    // ---------------- 关卡规格（教学段→应用段→挑战段，房间沿 +X 每间 38 单位） ----------------
    private static List<Room> Spec()
    {
        var rooms = new List<Room>();

        // ===== L1「表里初现」 =====
        // 教学段：安全平地 + 跳跃台阶 + 第一座里世界断桥（表世界坠落无害，爬回重试）
        var l1t = new Room(); l1t.name = "L1_Teach"; l1t.cfg = "Room_L1_Teach";
        l1t.rects.Add(new R('#', -19, -5, 18, -4));            // 满铺地板
        l1t.rects.Add(new R('#', -19, -3, -18, 9));            // 左墙
        l1t.rects.Add(new R('#', 17, -3, 18, 9));              // 右墙
        l1t.rects.Add(new R('#', -6, -3, -4, -2));             // 跳跃台阶（2 高）
        l1t.rects.Add(new R('X', -2, -5, 2, -4));              // 断桥缺口
        l1t.rects.Add(new R('#', -2, -6, 2, -6));              // 缺口坑底（安全）
        l1t.rects.Add(new R('N', -2, -4, 2, -4));              // 里世界桥（切里才有路）
        rooms.Add(l1t);

        // 应用段：表墙里路交替——断桥（表坠=尖刺）→ 表墙（切里穿墙）→ 表尖刺带（切里通行）
        var l1a = new Room(); l1a.name = "L1_Apply"; l1a.cfg = "Room_L1_Apply";
        l1a.rects.Add(new R('#', -19, -5, 18, -4));
        l1a.rects.Add(new R('#', -19, -3, -18, 9));
        l1a.rects.Add(new R('#', 17, -3, 18, 9));
        l1a.rects.Add(new R('X', -4, -5, 1, -4));              // 断桥缺口（机制必过：只有里世界有桥）
        l1a.rects.Add(new R('#', -4, -7, 1, -6));              // 坑底
        l1a.rects.Add(new R('T', -4, -6, 1, -6));              // 表尖刺：表世界坠落致死
        l1a.rects.Add(new R('N', -4, -4, 1, -4));              // 里世界桥
        l1a.rects.Add(new R('O', 4, -3, 5, 2));                // 表世界墙（6 高，切里穿过）
        l1a.rects.Add(new R('T', 9, -3, 14, -3));              // 表尖刺带（里世界步行通过）
        rooms.Add(l1a);

        // 挑战段：尖刺场上三岛连续切换（表岛→里岛→表岛，两次空中切换）
        var l1c = new Room(); l1c.name = "L1_Challenge"; l1c.cfg = "Room_L1_Challenge";
        l1c.rects.Add(new R('#', -19, -5, 18, -4));
        l1c.rects.Add(new R('#', -19, -3, -18, 9));
        l1c.rects.Add(new R('#', 17, -3, 18, 9));
        l1c.rects.Add(new R('T', -8, -3, 7, -3));              // 尖刺场
        l1c.rects.Add(new R('O', -7, -2, -5, -2));             // 岛1（表）
        l1c.rects.Add(new R('N', -2, -1, 0, -1));              // 岛2（里，高一格）
        l1c.rects.Add(new R('O', 3, -2, 5, -2));               // 岛3（表）
        rooms.Add(l1c);

        // ===== L2「斜坡法则」 =====
        // 教学段：45° 爬坡/下坡 + 移动平台跨越（坡=唯一上山路，平台=节奏教学）
        var l2t = new Room(); l2t.name = "L2_Teach"; l2t.cfg = "Room_L2_Teach";
        l2t.rects.Add(new R('#', -19, -5, 18, -4));
        l2t.rects.Add(new R('#', -19, -3, -18, 9));
        l2t.rects.Add(new R('#', 17, -3, 18, 9));
        l2t.slopes.Add(new S('#', -11, -3, -7, 1));            // 上坡（坡顶=高台）
        l2t.rects.Add(new R('#', -7, -3, -4, 0));              // 高台（底接到地板面，防壁龛空洞）
        l2t.rects.Add(new R('X', 2, -5, 10, -4));              // 平台缺口（跳距 9 > 平跳 8.2）
        l2t.rects.Add(new R('#', 2, -6, 10, -6));              // 坑底
        l2t.plat = new float[] { 6.5f, -1.5f, 1f, 0f, 5f, 2f };// 移动平台
        rooms.Add(l2t);

        // 应用段：坡末起跳跨沟（必须沿下坡冲到坡末，台顶起跳够不着）+ 单向板顶穿出坑
        var l2a = new Room(); l2a.name = "L2_Apply"; l2a.cfg = "Room_L2_Apply";
        l2a.rects.Add(new R('#', -19, -5, 18, -4));
        l2a.rects.Add(new R('#', -19, -3, -18, 9));
        l2a.rects.Add(new R('#', 17, -3, 18, 9));
        l2a.slopes.Add(new S('#', -14, -3, -10, 1));           // 上坡
        l2a.rects.Add(new R('#', -10, -3, -6, 0));             // 高台（底接地面）
        l2a.slopes.Add(new S('#', -6, 1, -2, -3));             // 下坡（坡末悬于沟上）
        l2a.rects.Add(new R('X', -1, -5, 4, -4));              // 沟（台顶起跳 +4.2 < +5 必死，坡末起跳 +6.2 过）
        l2a.rects.Add(new R('#', -1, -6, 4, -6));              // 沟底
        l2a.rects.Add(new R('T', -1, -5, 4, -5));              // 沟内尖刺
        l2a.rects.Add(new R('=', 7, -1, 12, -1));              // 单向板顶棚（坑道顶）
        l2a.rects.Add(new R('#', 13, -3, 13, 1));              // 挡墙（顶 +2，只能从单向板上跳越）
        rooms.Add(l2a);

        // 挑战段：坡末跳里世界平台（落点只在里世界）+ 表墙 + 弹幕（理智 2 倍）
        var l2c = new Room(); l2c.name = "L2_Challenge"; l2c.cfg = "Room_L2_Challenge";
        l2c.hasBullets = true;
        l2c.bPoints = new Vector2[] { new Vector2(13, 3), new Vector2(13, 1), new Vector2(13, -1) };
        l2c.rects.Add(new R('#', -19, -5, 18, -4));
        l2c.rects.Add(new R('#', -19, -3, -18, 9));
        l2c.rects.Add(new R('#', 17, -3, 18, 9));
        l2c.slopes.Add(new S('#', -14, -3, -10, 1));           // 上坡
        l2c.rects.Add(new R('#', -10, -3, -8, 0));             // 高台（底接地面）
        l2c.slopes.Add(new S('#', -8, 1, -4, -3));             // 下坡
        l2c.rects.Add(new R('X', -3, -5, 7, -4));              // 大沟（11 宽，平跳 8.2 不及）
        l2c.rects.Add(new R('#', -3, -6, 7, -6));              // 沟底
        l2c.rects.Add(new R('T', -3, -5, 7, -5));              // 沟内尖刺
        l2c.rects.Add(new R('N', 3, -4, 7, -4));               // 里世界落点（切里+动量才落得上）
        l2c.rects.Add(new R('O', 9, -3, 9, 2));                // 表墙（里世界穿过）
        rooms.Add(l2c);

        // ===== L3「断章」 =====
        // 教学段：综合热身——坡 + 单向板坑道 + 短里桥（无尖刺）
        var l3t = new Room(); l3t.name = "L3_Teach"; l3t.cfg = "Room_L3_Teach";
        l3t.rects.Add(new R('#', -19, -5, 18, -4));
        l3t.rects.Add(new R('#', -19, -3, -18, 9));
        l3t.rects.Add(new R('#', 17, -3, 18, 9));
        l3t.slopes.Add(new S('#', -12, -3, -10, -1));          // 短坡
        l3t.rects.Add(new R('#', -10, -2, -8, -2));            // 小高台（单行，顶 -1 与坡面齐平）
        l3t.rects.Add(new R('=', -6, -1, -3, -1));             // 单向板顶棚
        l3t.rects.Add(new R('#', -2, -3, -2, 0));              // 挡墙（顶 +1，从棚顶跳越）
        l3t.rects.Add(new R('X', 3, -5, 7, -4));               // 短缺口
        l3t.rects.Add(new R('#', 3, -6, 7, -6));               // 坑底
        l3t.rects.Add(new R('N', 3, -4, 7, -4));               // 里桥
        rooms.Add(l3t);

        // 应用段：连续切换解谜——表台→(切里)里岛→(切表)表岛，坑内尖刺随世界换向
        var l3a = new Room(); l3a.name = "L3_Apply"; l3a.cfg = "Room_L3_Apply";
        l3a.rects.Add(new R('#', -19, -5, 18, -4));
        l3a.rects.Add(new R('#', -19, -3, -18, 9));
        l3a.rects.Add(new R('#', 17, -3, 18, 9));
        l3a.slopes.Add(new S('#', -16, -3, -12, 1));           // 上坡
        l3a.rects.Add(new R('#', -12, -3, -10, 0));            // 高台（底接地面，表）
        l3a.rects.Add(new R('X', -9, -5, -7, -4));             // 坑A
        l3a.rects.Add(new R('#', -9, -6, -7, -6));
        l3a.rects.Add(new R('U', -9, -5, -7, -5));             // 里尖刺（表坠落安全，可爬回）
        l3a.rects.Add(new R('N', -6, 0, -4, 0));               // 里岛（空中切里落上）
        l3a.rects.Add(new R('X', -3, -5, -1, -4));             // 坑B
        l3a.rects.Add(new R('#', -3, -6, -1, -6));
        l3a.rects.Add(new R('T', -3, -5, -1, -5));             // 表尖刺（里坠落安全）
        l3a.rects.Add(new R('O', 0, 0, 2, 0));                 // 表岛（空中切表落上）
        l3a.rects.Add(new R('X', 3, -5, 5, -4));               // 坑C
        l3a.rects.Add(new R('#', 3, -6, 5, -6));
        l3a.rects.Add(new R('T', 3, -5, 5, -5));
        rooms.Add(l3a);

        // 挑战段：进房即里世界（dimensionRequirement=2）——限时通道（2.5 倍理智 → 预算 2.0s）
        // 冲刺 0.3s + 通道 1.4s = 1.7s，贴线可过；耗尽强制弹回 = 落表尖刺死亡重来。弹幕 0.5s。
        var l3c = new Room(); l3c.name = "L3_Challenge"; l3c.cfg = "Room_L3_Challenge";
        l3c.endGame = true; l3c.hasBullets = true;
        l3c.spawn = new Vector2(-12.5f, -1.95f);
        l3c.bPoints = new Vector2[] { new Vector2(13, 2), new Vector2(13, 0), new Vector2(13, -2) };
        l3c.rects.Add(new R('#', -19, -5, 18, -4));
        l3c.rects.Add(new R('#', -19, -3, -18, 9));
        l3c.rects.Add(new R('#', 17, -3, 18, 9));
        l3c.rects.Add(new R('X', -11, -5, -5, -4));            // 里世界限时通道（7 宽）
        l3c.rects.Add(new R('#', -11, -6, -5, -6));            // 通道底
        l3c.rects.Add(new R('T', -11, -5, -5, -5));            // 表尖刺：弹回即死
        l3c.rects.Add(new R('N', -11, -4, -5, -4));            // 里世界桥面
        rooms.Add(l3c);

        return rooms;
    }

    // ---------------- 入口 ----------------
    [MenuItem("Tools/Level Rebuild/重建关卡 L1-L3")]
    public static void BuildAll()
    {
        EnsureArt();
        EnsureConfigs();
        CleanupOld();

        // 归零 Rooms 父物体的历史偏移（旧 Grid 手工挪动残留 (3.02,3.49)）：
        // 房间内部坐标与设计稿一致的前提，否则场景 Player 与触发器世界坐标错位
        var roomsRoot = GameObject.Find("Rooms");
        if (roomsRoot != null) roomsRoot.transform.position = Vector3.zero;

        var rooms = Spec();
        var triggers = new RoomTrigger[rooms.Count];
        for (int i = 0; i < rooms.Count; i++)
            triggers[i] = BuildRoom(rooms[i], i);

        // 出口链：Room_i.Exit → Room_{i+1}.RoomTrigger；末房 EndGame
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            var exit = rooms[i].endGame ? null : GameObject.Find("Rooms/" + rooms[i].name + "/RoomExit");
            if (exit != null) exit.GetComponent<RoomExit>().targetRoom = triggers[i + 1];
        }

        EnsureLights();
        // 玩家放到首房出生点的世界坐标（必须落在 RoomTrigger 内，开局即触发 EnterRoom）
        var firstSpawn = GameObject.Find("Rooms/" + rooms[0].name + "/PlayerSpawn");
        var player = GameObject.FindGameObjectWithTag("Player");
        if (firstSpawn != null && player != null) player.transform.position = firstSpawn.transform.position;
        Selection.activeGameObject = null;

        bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LevelRebuild] 完成 rooms=" + rooms.Count + " saved=" + saved);
    }

    // ---------------- 生成美术（斜坡楔形 tile / 平台贴图） ----------------
    private static void EnsureArt()
    {
        if (!AssetDatabase.IsValidFolder(GenDir)) AssetDatabase.CreateFolder("Assets/ArtRes/Tiles", "Gen");
        tSlopeUp = EnsureGenTile("SlopeUp", new Color32(165, 98, 75, 255), false);
        tSlopeDown = EnsureGenTile("SlopeDown", new Color32(165, 98, 75, 255), true);
        platSprite = EnsureGenSprite("PlatformBlue", 32, 8, new Color32(70, 130, 200, 255));

        // 砖块索引按"平均色表"实测选定（全部实心；_16 等装饰切片是透明/零散贴片，不可作地形）
        tGroundFill = LoadTile(TileDir + "Terrain Sliced (16x16)_23.asset");
        tGroundTop = LoadTile(TileDir + "Terrain Sliced (16x16)_5.asset");
        tOldFill = LoadTile(TileDir + "Terrain Sliced (16x16)_107.asset");
        tOldTop = LoadTile(TileDir + "Terrain Sliced (16x16)_106.asset");
        tNewFill = LoadTile(TileDir + "Terrain Sliced (16x16)_97.asset");
        tNewTop = LoadTile(TileDir + "Terrain Sliced (16x16)_96.asset");
        tOneWay = LoadTile(TileDir + "Terrain Sliced (16x16)_111.asset");
        tTrap = LoadTile(TrapTile);
    }

    private static TileBase LoadTile(string path)
    {
        var t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (t == null) Debug.LogError("[LevelRebuild] tile 缺失: " + path);
        return t;
    }

    /// <summary>程序生成 16×16 楔形 tile（colliderType=None：碰撞由斜坡 PolygonCollider2D 负责，贴图只管视觉）。
    /// 每次重建强制重生成（配色调整即时生效）。</summary>
    private static TileBase EnsureGenTile(string name, Color32 c, bool flipX)
    {
        string png = GenDir + name + ".png";
        string asset = GenDir + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<TileBase>(asset) != null) AssetDatabase.DeleteAsset(asset);
        if (System.IO.File.Exists(png)) System.IO.File.Delete(png);
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int xx = flipX ? 15 - x : x;
                    bool filled = y <= xx; // 上升坡：对角线以下填充；下降坡镜像
                    tex.SetPixel(x, y, filled ? c : new Color32(0, 0, 0, 0));
                }
            tex.Apply();
            System.IO.File.WriteAllBytes(png, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(png);
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16;
            imp.filterMode = FilterMode.Point;
            imp.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, GenDir + name + ".asset");
        }
        return AssetDatabase.LoadAssetAtPath<TileBase>(GenDir + name + ".asset");
    }

    private static Sprite EnsureGenSprite(string name, int w, int h, Color32 c)
    {
        string png = GenDir + name + ".png";
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(png);
        if (s != null) return s;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                tex.SetPixel(x, y, edge ? new Color32(30, 60, 110, 255) : c);
            }
        tex.Apply();
        System.IO.File.WriteAllBytes(png, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(png);
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(png);
    }

    // ---------------- RoomConfigSO 资产 ----------------
    private static void EnsureConfigs()
    {
        MakeConfig("Room_L1_Teach", "L1·表里初现·教学", "表世界安全区 + 第一座里世界断桥", 0f, 0, 1f, null);
        MakeConfig("Room_L1_Apply", "L1·应用", "断桥/表墙/尖刺带：不切换过不去", 0f, 0, 1f, null);
        MakeConfig("Room_L1_Challenge", "L1·挑战", "尖刺场上三岛连续切换（理智 1.5 倍）", 0f, 0, 1.5f, null);
        MakeConfig("Room_L2_Teach", "L2·斜坡法则·教学", "45° 坡 + 移动平台", 1.2f, 0, 1f, null);
        MakeConfig("Room_L2_Apply", "L2·应用", "坡末起跳跨沟 + 单向板顶穿", 1f, 0, 1f, null);
        MakeConfig("Room_L2_Challenge", "L2·挑战", "坡末切里保动量落里平台 + 弹幕", 0.7f, 0, 2f, BulletStats);
        MakeConfig("Room_L3_Teach", "L3·断章·教学", "综合热身：坡/单向板/里桥", 1f, 0, 1f, null);
        MakeConfig("Room_L3_Apply", "L3·应用", "连续切换解谜：表→里→表", 0.8f, 0, 2f, null);
        MakeConfig("Room_L3_Challenge", "L3·挑战·断章", "进房即里世界：限时通道（理智 2.5 倍）+ 弹幕", 0.5f, 2, 2.5f, BulletStats);
    }

    private static void MakeConfig(string name, string roomName, string desc, float interval, int dimReq, float sanityMul, string bulletStatsPath)
    {
        string path = RoomDir + name + ".asset";
        var cfg = AssetDatabase.LoadAssetAtPath<RoomConfigSO>(path);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<RoomConfigSO>();
            AssetDatabase.CreateAsset(cfg, path);
        }
        cfg.roomName = roomName;
        cfg.description = desc;
        cfg.fireInterval = interval;
        cfg.dimensionRequirement = dimReq;
        cfg.sanityDrainMultiplier = sanityMul;
        cfg.bulletStats = bulletStatsPath == null ? null : AssetDatabase.LoadAssetAtPath<BulletStatsSO>(bulletStatsPath);
        EditorUtility.SetDirty(cfg);
    }

    // ---------------- 清理旧三关 ----------------
    private static void CleanupOld()
    {
        foreach (var n in new[] { "Room_1_Tutorial", "Room_2_Bullet", "Room_3_Parkour" })
        {
            var go = GameObject.Find("Rooms/" + n);
            if (go != null) Object.DestroyImmediate(go);
        }
        // 幂等：工具重跑前清掉上一轮生成的 L* 房间（否则叠加重复房间）
        var roomsRoot = GameObject.Find("Rooms");
        if (roomsRoot != null)
        {
            for (int i = roomsRoot.transform.childCount - 1; i >= 0; i--)
            {
                var child = roomsRoot.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("L1_") || child.name.StartsWith("L2_") || child.name.StartsWith("L3_"))
                    Object.DestroyImmediate(child);
            }
        }
        // 旧房间 prefab 与旧 RoomConfigSO 已无引用（房间先删），一并清理
        AssetDatabase.DeleteAsset("Assets/Resources/Prefabs/Room_1_Tutorial.prefab");
        AssetDatabase.DeleteAsset("Assets/Resources/Prefabs/Room_2_Bullet.prefab");
        AssetDatabase.DeleteAsset("Assets/Resources/Prefabs/Room_3_Parkour.prefab");
        AssetDatabase.DeleteAsset(RoomDir + "Room_Tutorial.asset");
        AssetDatabase.DeleteAsset(RoomDir + "Room_Bullet.asset");
        AssetDatabase.DeleteAsset(RoomDir + "Room_Parkour.asset");
    }

    // ---------------- 房间构建 ----------------
    private static RoomTrigger BuildRoom(Room spec, int index)
    {
        var root = new GameObject(spec.name);
        root.transform.parent = GameObject.Find("Rooms").transform;
        root.transform.localPosition = new Vector3(index * 38f, 0f, 0f);

        // 房间入口触发器（须覆盖出生点：首房开局 / 传送进房都落在出生点，必须立即触发 EnterRoom）
        var trigGo = new GameObject("RoomTrigger");
        trigGo.transform.parent = root.transform;
        trigGo.transform.localPosition = new Vector3(-14f, 1f, 0f);
        var trigCol = trigGo.AddComponent<BoxCollider2D>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector2(6f, 14f);
        var trig = trigGo.AddComponent<RoomTrigger>();

        // 出生点
        var spawnGo = new GameObject("PlayerSpawn");
        spawnGo.transform.parent = root.transform;
        spawnGo.transform.localPosition = spec.spawn;

        // 相机（静态房间机位，RoomManager 启停）；y=-1.5 让取景重心落在低空玩法区
        var vcamGo = new GameObject("RoomVcam");
        vcamGo.transform.parent = root.transform;
        vcamGo.transform.localPosition = new Vector3(0f, -1.5f, 0.01f);
        var vcam = vcamGo.AddComponent<Cinemachine.CinemachineVirtualCamera>();
        vcam.m_Lens.OrthographicSize = 9.8f;
        vcamGo.SetActive(index == 0);

        // 子弹生成器（默认停用，进房激活）
        GameObject spawnerGo = null;
        if (spec.hasBullets)
        {
            spawnerGo = new GameObject("BulletSpawner");
            spawnerGo.transform.parent = root.transform;
            spawnerGo.transform.localPosition = Vector3.zero;
            var bs = spawnerGo.AddComponent<BulletSpawner>();
            bs.roomConfig = AssetDatabase.LoadAssetAtPath<RoomConfigSO>(RoomDir + spec.cfg + ".asset");
            bs.bulletStats = bs.roomConfig != null ? bs.roomConfig.bulletStats : null;
            bs.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefab).GetComponent<Bullet>();
            var pts = new Transform[spec.bPoints.Length];
            for (int i = 0; i < spec.bPoints.Length; i++)
            {
                var p = new GameObject("Pt" + (i + 1));
                p.transform.parent = spawnerGo.transform;
                p.transform.localPosition = spec.bPoints[i];
                pts[i] = p.transform;
            }
            bs.spawnPoints = pts;
            spawnerGo.SetActive(false);
        }

        // 出口 / 通关
        if (!spec.endGame)
        {
            var exitGo = new GameObject("RoomExit");
            exitGo.transform.parent = root.transform;
            exitGo.transform.localPosition = spec.exit;
            var sr = exitGo.AddComponent<SpriteRenderer>();
            sr.sprite = platSprite;
            sr.sortingOrder = 50;
            var col = exitGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 3.2f);
            exitGo.AddComponent<RoomExit>();
        }
        else
        {
            var endGo = new GameObject("EndGame");
            endGo.transform.parent = root.transform;
            endGo.transform.localPosition = spec.exit;
            var sr = endGo.AddComponent<SpriteRenderer>();
            sr.sprite = platSprite;
            sr.sortingOrder = 50;
            var col = endGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 3.2f);
            endGo.AddComponent<EndGameTrigger>();
        }

        // Grid + 六张维度 Tilemap
        var gridGo = new GameObject("Grid");
        gridGo.transform.parent = root.transform;
        gridGo.transform.localPosition = Vector3.zero;
        gridGo.AddComponent<Grid>();

        var ground = MakeTilemap(gridGo.transform, "Ground", LayerMask.NameToLayer("Ground"), "", false, 0);
        var oldG = MakeTilemap(gridGo.transform, "OldGround", LayerMask.NameToLayer("OldDimension"), "", true, 10, false);
        var newG = MakeTilemap(gridGo.transform, "NewGround", LayerMask.NameToLayer("NewDimension"), "", true, 20, true);
        var oneWay = MakeTilemap(gridGo.transform, "OneWay", LayerMask.NameToLayer("Ground"), "OneWayPlatform", false, 30);
        var trapO = MakeTilemap(gridGo.transform, "OldTrap", LayerMask.NameToLayer("OldDimension"), "Trap", true, 40, false);
        var trapN = MakeTilemap(gridGo.transform, "NewTrap", LayerMask.NameToLayer("NewDimension"), "Trap", true, 41, true);

        // 逐矩形绘制（先铺后挖）
        foreach (var r in spec.rects)
        {
            for (int x = r.x0; x <= r.x1; x++)
                for (int y = r.y0; y <= r.y1; y++)
                    SetCell(r.l, ground, oldG, newG, oneWay, trapO, trapN, x, y, false);
        }

        // 表层砖（先于斜坡：楔形/坡下填充后画，防止被顶砖覆盖成方形草块）
        ApplyTopTiles(spec, ground, oldG, newG, oneWay, trapO, trapN);

        // 斜坡：楔形贴图 + 地下填充 + PolygonCollider2D（物理真值，>60°按墙由 KinematicBody 处理）
        int slopeIdx = 0;
        foreach (var s in spec.slopes)
            BuildSlope(gridGo.transform, ground, oldG, newG, s, slopeIdx++);

        // 移动平台
        if (spec.plat != null)
        {
            var pGo = new GameObject("MovingPlatform");
            pGo.transform.parent = root.transform;
            pGo.transform.localPosition = new Vector3(spec.plat[0], spec.plat[1], 0f);
            pGo.layer = LayerMask.NameToLayer("Ground");
            var psr = pGo.AddComponent<SpriteRenderer>();
            psr.sprite = platSprite;
            psr.sortingOrder = 35;
            var pcol = pGo.AddComponent<BoxCollider2D>();
            pcol.size = new Vector2(2f, 0.5f);
            var mp = pGo.AddComponent<MovingPlatform>();
            var so = new SerializedObject(mp);
            so.FindProperty("moveAxis").vector2Value = new Vector2(spec.plat[2], spec.plat[3]);
            so.FindProperty("moveDistance").floatValue = spec.plat[4];
            so.FindProperty("speed").floatValue = spec.plat[5];
            so.ApplyModifiedProperties();
        }

        // RoomTrigger 引用接线
        trig.roomConfig = AssetDatabase.LoadAssetAtPath<RoomConfigSO>(RoomDir + spec.cfg + ".asset");
        trig.roomVcam = vcam;
        trig.playerSpawnPos = spawnGo.transform;
        trig.activateOnEnter = spec.hasBullets ? new[] { spawnerGo } : new GameObject[0];
        return trig;
    }

    private static void SetCell(char l, Tilemap ground, Tilemap oldG, Tilemap newG, Tilemap oneWay, Tilemap trapO, Tilemap trapN, int x, int y, bool topPass)
    {
        var p = new Vector3Int(x, y, 0);
        switch (l)
        {
            case '#': ground.SetTile(p, topPass ? tGroundTop : tGroundFill); break;
            case 'O': oldG.SetTile(p, topPass ? tOldTop : tOldFill); break;
            case 'N': newG.SetTile(p, topPass ? tNewTop : tNewFill); break;
            case '=': oneWay.SetTile(p, tOneWay); break;
            case 'T': trapO.SetTile(p, tTrap); break;
            case 'U': trapN.SetTile(p, tTrap); break;
            case 'X': ground.SetTile(p, null); break;
        }
    }

    private static void ApplyTopTiles(Room spec, Tilemap ground, Tilemap oldG, Tilemap newG, Tilemap oneWay, Tilemap trapO, Tilemap trapN)
    {
        // 最终占用状态（挖空后再判定）：某层 (x,y+1) 无砖才换"顶砖"。
        // 为什么不能按矩形顺序判：挖空矩形在前序矩形之后处理，按顺序判会把已挖空的位置当仍有砖。
        var occupied = new HashSet<string>();
        foreach (var r in spec.rects)
        {
            if (r.l == 'X') { // 挖空：移除所有层
                for (int x = r.x0; x <= r.x1; x++)
                    for (int y = r.y0; y <= r.y1; y++)
                        occupied.Remove(x + ":" + y);
                continue;
            }
            for (int x = r.x0; x <= r.x1; x++)
                for (int y = r.y0; y <= r.y1; y++)
                    occupied.Add(x + ":" + y);
        }
        foreach (var r in spec.rects)
        {
            if (r.l == 'X' || r.l == '=') continue;
            for (int x = r.x0; x <= r.x1; x++)
                for (int y = r.y0; y <= r.y1; y++)
                    if (occupied.Contains(x + ":" + y) && !occupied.Contains(x + ":" + (y + 1)))
                        SetCell(r.l, ground, oldG, newG, oneWay, trapO, trapN, x, y, true);
        }
    }

    private static void BuildSlope(Transform grid, Tilemap ground, Tilemap oldG, Tilemap newG, S s, int idx)
    {
        int n = Mathf.Abs(s.x1 - s.x0);
        bool rising = s.y1 > s.y0;
        Tilemap map = s.d == '#' ? ground : (s.d == 'O' ? oldG : newG);

        for (int i = 0; i < n; i++)
        {
            int cx = s.x0 + i;
            int cy = rising ? s.y0 + i : s.y0 - i;
            map.SetTile(new Vector3Int(cx, cy, 0), rising ? tSlopeUp : tSlopeDown); // 楔形视觉（无碰撞）
            for (int fy = cy - 1; fy >= -4; fy--)                                    // 坡下填充到地板顶层
                map.SetTile(new Vector3Int(cx, fy, 0), s.d == '#' ? tGroundFill : (s.d == 'O' ? tOldFill : tNewFill));
        }
        // 落地列（对角线终点所在列）也要补到地板：否则坡顶邻列留 1 格洞（如上坡接高台处）
        {
            int cx = s.x1;
            for (int fy = (rising ? s.y1 - 1 : s.y1 - 1); fy >= -4; fy--)
                map.SetTile(new Vector3Int(cx, fy, 0), s.d == '#' ? tGroundFill : (s.d == 'O' ? tOldFill : tNewFill));
        }

        // 物理斜面：三角形（直角边贴地/立墙），KinematicBody 射线读其法线完成贴坡
        var go = new GameObject("Slope_" + idx);
        go.transform.parent = grid;
        go.transform.localPosition = Vector3.zero;
        go.layer = s.d == '#' ? LayerMask.NameToLayer("Ground") : (s.d == 'O' ? LayerMask.NameToLayer("OldDimension") : LayerMask.NameToLayer("NewDimension"));
        if (s.d != '#') AddMaskObject(go, s.d == 'N');
        var poly = go.AddComponent<PolygonCollider2D>();
        poly.points = new[] { new Vector2(s.x0, s.y0), new Vector2(s.x1, s.y1), new Vector2(s.x1, s.y0) };
    }

    private static Tilemap MakeTilemap(Transform grid, string name, int layer, string tag, bool mask, int sortOrder, bool showWhenMask = false)
    {
        var go = new GameObject(name);
        go.transform.parent = grid;
        go.transform.localPosition = Vector3.zero;
        go.layer = layer;
        if (tag != "") go.tag = tag;

        var tm = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TilemapMat);
        tr.sortingOrder = sortOrder;
        var col = go.AddComponent<TilemapCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        col.usedByComposite = true;
        var comp = go.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        if (mask) AddMaskObject(go, showWhenMask);
        return tm;
    }

    private static void AddMaskObject(GameObject go, bool showWhenMaskActive)
    {
        var mo = go.AddComponent<MaskObject>();
        var so = new SerializedObject(mo);
        so.FindProperty("showWhenMaskActive").boolValue = showWhenMaskActive;
        so.ApplyModifiedProperties();
    }

    // ---------------- 灯光补线（任务 10 场景接线在磁盘上缺失，此处恢复） ----------------
    private static void EnsureLights()
    {
        var bright = EnsureGlobalLight("GlobalLight_Real", 1.0f);
        var dim = EnsureGlobalLight("GlobalLight_Mask", 0.12f);

        // 分层避让：URP 每个排序层每混合样式只认一盏 Global Light（同层第二盏被忽略并警告，
        // 且哪盏生效取决于注册顺序——非确定性）。玩法内容全部在 Default 层渲染 →
        // 亮光只管 Default，暗光管空的 Background/Foreground：运行时等效"单全局光强度交叉"
        //（表 1.0 / 里 0.12+玩家灯），行为与用户验收表现一致，警告消除。
        SetApplyLayers(bright, new[] { 0 });
        SetApplyLayers(dim, new[] { -677973757, 331793919 }); // Background / Foreground

        var go = GameObject.Find("DimensionLightController");
        if (go == null)
        {
            go = new GameObject("DimensionLightController");
            var ctrl = go.AddComponent<DimensionLightController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("brightGlobalLight").objectReferenceValue = bright;
            so.FindProperty("dimGlobalLight").objectReferenceValue = dim;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetApplyLayers(Light2D light, int[] sortingLayerIds)
    {
        var so = new SerializedObject(light);
        var prop = so.FindProperty("m_ApplyToSortingLayers");
        prop.arraySize = sortingLayerIds.Length;
        for (int i = 0; i < sortingLayerIds.Length; i++)
            prop.GetArrayElementAtIndex(i).intValue = sortingLayerIds[i];
        so.ApplyModifiedProperties();
    }

    private static Light2D EnsureGlobalLight(string name, float intensity)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = intensity;
        }
        return go.GetComponent<Light2D>();
    }
}
