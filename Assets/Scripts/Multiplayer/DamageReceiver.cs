using UnityEngine;
using System.Collections;

namespace AngryRain.Multiplayer
{
    public class DamageReceiver : MonoBehaviour
    {
        public TargetObject targetObject = TargetObject.PlayerManager;
        public PlayerManager pManager;
		public AIUnit aiunit;
        public MultiplayerVehicle mVehicle;

        public CharacterJoint joint;
        public int thisIndex;

        public SurfaceType surfaceType;

		public bool ai = false;

        public enum SurfaceType
        {
            None,
            Dirt,
            Stone,
            Metal,
            Wood,
            Water,
            Body
        }

        void Awake()
        {
            if (targetObject == TargetObject.PlayerManager)
            {
				if(!ai && pManager){
	                pManager.damageReceivers.Add(this);
	                thisIndex = pManager.damageReceivers.IndexOf(this);
				}
				else if(aiunit){
					aiunit.allDamageReceivers.Add (this);
					thisIndex = aiunit.allDamageReceivers.IndexOf (this);
				}
            }
			if (!ai) {
				if (!pManager) {
					enabled = false;
				}
			} else {
				if(!aiunit){
					enabled = false;
				}
			}
        }

        public void GetDamage(DamageGiver dGiver)
        {
            if (targetObject == TargetObject.PlayerManager) {
				if(!ai){
					pManager.Local_ReceiveDamage (dGiver, this);
				}
				else{
					aiunit.Local_ReceiveDamage (dGiver, this);
				}
			}
            if (targetObject == TargetObject.Vehicle)
                mVehicle.Local_ReceiveDamage(dGiver, this);
        }

        public static float GetDamageMultiplier(CharacterJoint joint)
        {
            switch (joint)
            {
                case CharacterJoint.Head:
                    return 2.5f;
                case CharacterJoint.Torso:
                    return 1;
                case CharacterJoint.Legs:
                    return 0.85f;
                case CharacterJoint.Arms:
                    return 0.9f;
            }
            return 1;
        }

        public static int EncryptionCharacterJoint(CharacterJoint joint)
        {
            switch (joint)
            {
                case CharacterJoint.Head:
                    return 0;
                case CharacterJoint.Torso:
                    return 1;
                case CharacterJoint.Legs:
                    return 2;
                case CharacterJoint.Arms:
                    return 3;
            }
            return 1;
        }

        public static CharacterJoint EncryptionCharacterJoint(int joint)
        {
            switch (joint)
            {
                case 0:
                    return CharacterJoint.Head;
                case 1:
                    return CharacterJoint.Torso;
                case 2:
                    return CharacterJoint.Legs;
                case 3:
                    return CharacterJoint.Arms;
            }
            return CharacterJoint.Torso;
        }
    }

    public enum CharacterJoint
    {
        Head,
        Torso,
        Legs,
        Arms
    }

    public enum TargetObject
    {
        PlayerManager,
        Vehicle,
        Fragment
    }
}