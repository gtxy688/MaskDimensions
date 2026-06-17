using UnityEngine;

[CreateAssetMenu(fileName = "NewRoom", menuName = "Config/Room")]
public class RoomConfigSO : ScriptableObject
{
    public string roomName;
    [TextArea] public string description;

    [Header("子弹配置（第 2 关专用）")]
    public BulletStatsSO bulletStats;
    public float fireInterval = 1f;

    [Header("BGM")]
    public AudioClip bgmOverride;
}
