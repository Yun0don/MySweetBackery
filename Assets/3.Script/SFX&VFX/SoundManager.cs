using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource; 

    [Header("SFX Clips")]
    [SerializeField] private AudioClip cash;
    [SerializeField] private AudioClip costMoney;
    [SerializeField] private AudioClip getObject;
    [SerializeField] private AudioClip putObject;
    [SerializeField] private AudioClip success;
    [SerializeField] private AudioClip trash;

    void Awake()
    {
        // ✅ 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayCash()         => PlayOneShot(cash);
    public void PlayCostMoney()    => PlayOneShot(costMoney);
    public void PlayGetObject()    => PlayOneShot(getObject);
    public void PlayPutObject()    => PlayOneShot(putObject);
    public void PlaySuccess()      => PlayOneShot(success);
    public void PlayTrash()        => PlayOneShot(trash);

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] 재생할 오디오 클립이 없습니다.");
            return;
        }
        sfxSource.PlayOneShot(clip);
    }
}