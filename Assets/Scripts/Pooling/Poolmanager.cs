using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AngryRain
{
    public static class PoolManager
    {
        static List<ParticleEffect> particlePool = new List<ParticleEffect>();
        static List<MultiplayerProjectile> projectilePool = new List<MultiplayerProjectile>();

        static int lastID=0;

        public static ParticleEffect CreateParticle(ParticleEffect particle, Vector3 position, Quaternion rotation)
        {
            if (particle == null)
                return null;

            //string gameObjectName = particle.particleName;
            ParticleEffect par = GetNextAvailiableInstance(particle.particleID);
            if (par == null)
            {
                par = (GameObject.Instantiate(particle.gameObject) as GameObject).GetComponent<ParticleEffect>();
                //par.gameObject.name = gameObjectName;
                particlePool.Add(par);
                par.Initialize();
            }
            else
            {
                par.gameObject.SetActive(true);
            }
            par.lastTimeUse = Time.time + par.usageTime;
            par.transform.position = position;
            par.transform.rotation = rotation;
            return par;
        }

        static ParticleEffect GetNextAvailiableInstance(int particleID)
        {
            int c = particlePool.Count;
            for (var i = 0; i < c; i++)
            {
                ParticleEffect a = particlePool[i];
                if (a.particleID.Equals(particleID) && a.lastTimeUse < Time.time) 
                    return a;
            }

            return null;
        }

        public static MultiplayerProjectile CreateProjectile(MultiplayerProjectile projectile, Vector3 position, Quaternion rotation)
        {
            MultiplayerProjectile par = GetNextAvailiableInstance(projectile);
            if (par == null)
            {
                par = (Object.Instantiate(projectile.gameObject) as GameObject).GetComponent<MultiplayerProjectile>();
                par.gameObject.name = projectile.gameObject.name;
                projectilePool.Add(par);
            }
            par.InitializeProjectile(position, rotation);
            return par;
        }

        static MultiplayerProjectile GetNextAvailiableInstance(MultiplayerProjectile projectile)
        {
            int c = projectilePool.Count;
            for (var i = 0; i < c; i++)
            {
                MultiplayerProjectile a = projectilePool[i];
                if (a.projectileID.Equals(projectile.projectileID) && a.isAvailable)
                    return a;
            }

            return null;
        }
    }
}