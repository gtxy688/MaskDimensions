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

    [Header("维度要求（关卡重做新增）")]
    [Tooltip("0=不限制，1=必须表世界，2=必须里世界；进入时若不满足则自动切换")]
    public int dimensionRequirement = 0;

    [Tooltip("理智消耗倍率（L3 压力用，默认 1）")]
    public float sanityDrainMultiplier = 1f;
}
