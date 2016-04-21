using UnityEngine;

namespace AngryRain.Multiplayer
{
    public class DamageGiver
    {
        public DamageGiver(ClientPlayer mPlayer, int mWeapon)
        {
            this.mPlayer = mPlayer;
            this.mWeapon = mWeapon;
        }

        public ClientPlayer mPlayer;
        public int mWeapon;

        public DamageReceiver damageReceiver;

        public Vector3 LocalHitPosition;
        public float HitForce = 1000;
    }
}