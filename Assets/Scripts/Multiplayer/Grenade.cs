using UnityEngine;
using System.Collections;
using AngryRain;
using AngryRain.Multiplayer;
using System.Collections.Generic;
using UnityEngine.Audio;

public class Grenade : TNBehaviour
{
    public Vector3 startVelocity;
    public ParticleEffect explosion;

    public float minimumRange = 1;
    public float maximumRange = 5;

    public SoundItem[] explosionAudioClips;
    public AudioMixerGroup targetGroup;

    //Public Variables - Not visible
    public ClientPlayer player;
    public new Rigidbody rigidbody { get; private set; }

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(int cPlayer)
    {
        player = MultiplayerManager.GetPlayer(cPlayer);
    }

    public void Throw()
    {
        if (TNManager.isHosting)
            StartCoroutine(IEThrow());
    }

    IEnumerator IEThrow()
    {
        rigidbody.velocity = new Vector3(0, player.playerManager.multiplayerObject.cs_Buffer[0].velocity.y, 0);
        rigidbody.AddRelativeForce(startVelocity, ForceMode.Impulse);
        yield return new WaitForSeconds(2.5f);
        tno.Send("Explode", TNet.Target.All);

        MultiplayerManager.ExplosionDamage(player, transform.position, minimumRange, maximumRange, 500);

        TNManager.Destroy(gameObject);
    }

    [TNet.RFC]
    public void Explode()
    {
        ParticleEffect pe = PoolManager.CreateParticle(explosion, transform.position, Quaternion.identity);
        pe.PlayParticleEffect();
        for (int i = 0; i < explosionAudioClips.Length; i++)
            explosionAudioClips[i].Play(transform.position, null);
    }
}
